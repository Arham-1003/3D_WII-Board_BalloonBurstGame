#!/usr/bin/env python3
"""Stream Wii Balance Board Recorder samples to Balloon Balance Unity.

This bridge deliberately performs acquisition transport only.  It tails the
Recorder's tab-delimited output, preserves every observed line in an audit
CSV, and sends valid four-load samples to Unity using UDP protocol version 1.
Gameplay filtering, participant calibration, and pointer mapping remain in
Unity.
"""

from __future__ import annotations

import argparse
import csv
import json
import math
import socket
import sys
import time
from dataclasses import dataclass
from datetime import datetime
from pathlib import Path
from typing import Optional, Sequence, TextIO


PROTOCOL_VERSION = 1
DEFAULT_HOST = "127.0.0.1"
DEFAULT_PORT = 5005


class SampleError(ValueError):
    """Raised when a source row cannot safely become a Unity packet."""


@dataclass(frozen=True)
class ColumnMap:
    time: int
    rear_left: int
    rear_right: int
    front_left: int
    front_right: int
    raw_rear_left: Optional[int] = None
    raw_rear_right: Optional[int] = None
    raw_front_left: Optional[int] = None
    raw_front_right: Optional[int] = None

    @property
    def required_columns(self) -> int:
        indexes = [
            self.time,
            self.rear_left,
            self.rear_right,
            self.front_left,
            self.front_right,
        ]
        indexes.extend(
            index
            for index in (
                self.raw_rear_left,
                self.raw_rear_right,
                self.raw_front_left,
                self.raw_front_right,
            )
            if index is not None
        )
        return max(indexes) + 1


# Documented Recorder order:
# Time, CpX, CpY, BL, BR, TL, TR, Weight, RAWBL, RAWBR, RAWTL, RAWTR
RECORDER_COLUMNS = ColumnMap(
    time=0,
    rear_left=3,
    rear_right=4,
    front_left=5,
    front_right=6,
    raw_rear_left=8,
    raw_rear_right=9,
    raw_front_left=10,
    raw_front_right=11,
)


@dataclass(frozen=True)
class BoardSample:
    source_time_seconds: float
    front_left: float
    front_right: float
    rear_left: float
    rear_right: float
    raw_front_left: Optional[float]
    raw_front_right: Optional[float]
    raw_rear_left: Optional[float]
    raw_rear_right: Optional[float]

    @property
    def total_load(self) -> float:
        return self.front_left + self.front_right + self.rear_left + self.rear_right

    def packet(self, sequence: int) -> dict[str, object]:
        return {
            "version": PROTOCOL_VERSION,
            "sequence": sequence,
            "sourceTime": self.source_time_seconds,
            "frontLeft": self.front_left,
            "frontRight": self.front_right,
            "rearLeft": self.rear_left,
            "rearRight": self.rear_right,
            "totalLoad": self.total_load,
            "connected": True,
        }


def finite_number(value: str, label: str) -> float:
    cleaned = value.strip().replace(",", ".")
    if not cleaned or "#IND" in cleaned.upper():
        raise SampleError(f"invalid {label}: {value!r}")
    try:
        number = float(cleaned)
    except ValueError as exc:
        raise SampleError(f"invalid {label}: {value!r}") from exc
    if not math.isfinite(number):
        raise SampleError(f"non-finite {label}: {value!r}")
    return number


def split_row(line: str) -> list[str]:
    # Both the Recorder and the supplied legacy files are whitespace-delimited.
    # This also accepts tabs mixed with repeated spaces.
    return line.strip().lstrip("\ufeff").split()


def parse_sample(
    line: str,
    columns: ColumnMap,
    time_unit: str = "milliseconds",
) -> BoardSample:
    fields = split_row(line)
    if not fields:
        raise SampleError("empty line")
    if any(character.isalpha() for character in fields[0]):
        raise SampleError("header or non-data line")
    if len(fields) < columns.required_columns:
        if len(fields) == 10 and columns == RECORDER_COLUMNS:
            raise SampleError(
                "legacy 10-column row: physical corner order is undocumented; "
                "use the Recorder 12-column file or explicit custom columns"
            )
        raise SampleError(
            f"expected at least {columns.required_columns} columns, found {len(fields)}"
        )

    source_time = finite_number(fields[columns.time], "source time")
    if time_unit == "milliseconds":
        source_time /= 1000.0
    elif time_unit != "seconds":
        raise ValueError(f"unsupported time unit: {time_unit}")
    if source_time < 0:
        raise SampleError("source time cannot be negative")

    def required(index: int, label: str) -> float:
        return finite_number(fields[index], label)

    def optional(index: Optional[int], label: str) -> Optional[float]:
        return None if index is None else finite_number(fields[index], label)

    sample = BoardSample(
        source_time_seconds=source_time,
        front_left=required(columns.front_left, "front-left load"),
        front_right=required(columns.front_right, "front-right load"),
        rear_left=required(columns.rear_left, "rear-left load"),
        rear_right=required(columns.rear_right, "rear-right load"),
        raw_front_left=optional(columns.raw_front_left, "raw front-left"),
        raw_front_right=optional(columns.raw_front_right, "raw front-right"),
        raw_rear_left=optional(columns.raw_rear_left, "raw rear-left"),
        raw_rear_right=optional(columns.raw_rear_right, "raw rear-right"),
    )
    if not math.isfinite(sample.total_load):
        raise SampleError("non-finite total load")
    return sample


AUDIT_FIELDS = [
    "record_number",
    "sequence",
    "bridge_monotonic_seconds",
    "source_time_seconds",
    "front_left",
    "front_right",
    "rear_left",
    "rear_right",
    "total_load",
    "raw_front_left",
    "raw_front_right",
    "raw_rear_left",
    "raw_rear_right",
    "status",
    "source_line",
]


class Bridge:
    def __init__(
        self,
        destination: tuple[str, int],
        audit_file: TextIO,
        columns: ColumnMap,
        time_unit: str,
        udp_socket: Optional[socket.socket] = None,
    ) -> None:
        self.destination = destination
        self.columns = columns
        self.time_unit = time_unit
        self.socket = udp_socket or socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.audit_file = audit_file
        self.audit = csv.DictWriter(audit_file, fieldnames=AUDIT_FIELDS)
        if audit_file.tell() == 0:
            self.audit.writeheader()
            audit_file.flush()
        self.record_number = 0
        self.sequence = 0
        self.last_source_time: Optional[float] = None
        self.sent_count = 0
        self.rejected_count = 0

    def close(self) -> None:
        self.socket.close()
        self.audit_file.flush()

    def process_line(self, line: str) -> bool:
        self.record_number += 1
        sample: Optional[BoardSample] = None
        status = "sent"
        packet_sequence: Optional[int] = None
        try:
            sample = parse_sample(line, self.columns, self.time_unit)
            if (
                self.last_source_time is not None
                and sample.source_time_seconds < self.last_source_time
            ):
                raise SampleError("source timestamp moved backwards")
            packet_sequence = self.sequence
        except SampleError as exc:
            status = f"rejected: {exc}"
            self.rejected_count += 1

        row = {
            "record_number": self.record_number,
            "sequence": "" if packet_sequence is None else packet_sequence,
            "bridge_monotonic_seconds": f"{time.perf_counter():.9f}",
            "source_time_seconds": "" if sample is None else f"{sample.source_time_seconds:.9f}",
            "front_left": "" if sample is None else sample.front_left,
            "front_right": "" if sample is None else sample.front_right,
            "rear_left": "" if sample is None else sample.rear_left,
            "rear_right": "" if sample is None else sample.rear_right,
            "total_load": "" if sample is None else sample.total_load,
            "raw_front_left": "" if sample is None else sample.raw_front_left,
            "raw_front_right": "" if sample is None else sample.raw_front_right,
            "raw_rear_left": "" if sample is None else sample.raw_rear_left,
            "raw_rear_right": "" if sample is None else sample.raw_rear_right,
            "status": status,
            "source_line": line.rstrip("\r\n"),
        }

        # The audit record is made durable to Python before UDP transmission.
        self.audit.writerow(row)
        self.audit_file.flush()
        if sample is None or packet_sequence is None:
            return False

        encoded = json.dumps(
            sample.packet(packet_sequence), separators=(",", ":"), allow_nan=False
        ).encode("utf-8")
        self.socket.sendto(encoded, self.destination)
        self.sequence += 1
        self.last_source_time = sample.source_time_seconds
        self.sent_count += 1
        return True


def make_column_map(args: argparse.Namespace) -> ColumnMap:
    if args.format == "recorder":
        return RECORDER_COLUMNS
    required = {
        "time": args.time_column,
        "rear_left": args.rear_left_column,
        "rear_right": args.rear_right_column,
        "front_left": args.front_left_column,
        "front_right": args.front_right_column,
    }
    missing = [name.replace("_", "-") for name, value in required.items() if value is None]
    if missing:
        raise SampleError("custom format requires columns: " + ", ".join(missing))
    return ColumnMap(
        **required,
        raw_rear_left=args.raw_rear_left_column,
        raw_rear_right=args.raw_rear_right_column,
        raw_front_left=args.raw_front_left_column,
        raw_front_right=args.raw_front_right_column,
    )


def wait_for_file(path: Path, poll_seconds: float) -> None:
    announced = False
    while not path.is_file():
        if not announced:
            print(f"Waiting for Recorder output: {path}", flush=True)
            announced = True
        time.sleep(poll_seconds)


def follow_file(path: Path, from_start: bool, poll_seconds: float):
    """Yield complete appended lines and recover if the Recorder truncates the file."""
    first_open = True
    while True:
        wait_for_file(path, poll_seconds)
        with path.open("r", encoding="utf-8-sig", errors="replace", newline="") as source:
            if first_open and not from_start:
                source.seek(0, 2)
            first_open = False
            while True:
                start = source.tell()
                line = source.readline()
                if line:
                    if line.endswith("\n") or line.endswith("\r"):
                        yield line
                    else:
                        source.seek(start)
                        time.sleep(poll_seconds)
                    continue
                try:
                    if path.stat().st_size < source.tell():
                        break
                except FileNotFoundError:
                    break
                time.sleep(poll_seconds)


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Tail Wii Balance Board Recorder data and stream four loads to Unity."
    )
    parser.add_argument("--input", required=True, type=Path, help="Recorder TXT data file")
    parser.add_argument("--host", default=DEFAULT_HOST)
    parser.add_argument("--port", type=int, default=DEFAULT_PORT)
    parser.add_argument("--log-dir", type=Path, default=Path("BridgeLogs"))
    parser.add_argument("--from-start", action="store_true", help="Replay existing rows")
    parser.add_argument("--poll-seconds", type=float, default=0.01)
    parser.add_argument("--format", choices=("recorder", "custom"), default="recorder")
    parser.add_argument("--time-unit", choices=("milliseconds", "seconds"), default="milliseconds")
    for name in (
        "time",
        "front-left",
        "front-right",
        "rear-left",
        "rear-right",
        "raw-front-left",
        "raw-front-right",
        "raw-rear-left",
        "raw-rear-right",
    ):
        parser.add_argument(f"--{name}-column", type=int)
    return parser


def run(arguments: Optional[Sequence[str]] = None) -> int:
    args = build_parser().parse_args(arguments)
    if not 1 <= args.port <= 65535:
        raise SampleError("port must be between 1 and 65535")
    if args.poll_seconds <= 0:
        raise SampleError("poll-seconds must be positive")
    columns = make_column_map(args)
    args.log_dir.mkdir(parents=True, exist_ok=True)
    stamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    audit_path = args.log_dir / f"bridge_raw_{stamp}.csv"

    print(f"Input: {args.input.resolve()}")
    print(f"Unity UDP: {args.host}:{args.port}")
    print(f"Raw audit log: {audit_path.resolve()}")
    print("Press Ctrl+C to stop.", flush=True)

    with audit_path.open("x", encoding="utf-8", newline="") as audit_file:
        bridge = Bridge((args.host, args.port), audit_file, columns, args.time_unit)
        try:
            for line in follow_file(args.input, args.from_start, args.poll_seconds):
                bridge.process_line(line)
                if (bridge.sent_count + bridge.rejected_count) % 100 == 0:
                    print(
                        f"Sent {bridge.sent_count}; rejected {bridge.rejected_count}",
                        flush=True,
                    )
        except KeyboardInterrupt:
            print("\nStopping bridge.")
        finally:
            bridge.close()
            print(
                f"Final counts - sent: {bridge.sent_count}, "
                f"rejected: {bridge.rejected_count}"
            )
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(run())
    except (OSError, SampleError) as exc:
        print(f"Bridge error: {exc}", file=sys.stderr)
        raise SystemExit(2)
