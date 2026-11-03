import io
import json
import socket
import unittest

from balance_board_bridge import (
    Bridge,
    RECORDER_COLUMNS,
    SampleError,
    parse_sample,
)


RECORDER_ROW = "1000\t1.2\t-2.3\t10\t20\t30\t40\t10.2\t110\t120\t130\t140\n"


class RecorderParserTests(unittest.TestCase):
    def test_documented_recorder_columns_map_top_to_front(self):
        sample = parse_sample(RECORDER_ROW, RECORDER_COLUMNS)

        self.assertEqual(1.0, sample.source_time_seconds)
        self.assertEqual(30.0, sample.front_left)
        self.assertEqual(40.0, sample.front_right)
        self.assertEqual(10.0, sample.rear_left)
        self.assertEqual(20.0, sample.rear_right)
        self.assertEqual(100.0, sample.total_load)
        self.assertEqual(130.0, sample.raw_front_left)
        self.assertEqual(110.0, sample.raw_rear_left)

    def test_rejects_legacy_undocumented_ten_column_row(self):
        with self.assertRaisesRegex(SampleError, "legacy 10-column"):
            parse_sample("0 1 2 3 4 5 6 7 8 9\n", RECORDER_COLUMNS)

    def test_rejects_non_finite_values(self):
        with self.assertRaisesRegex(SampleError, "non-finite"):
            parse_sample(RECORDER_ROW.replace("\t30\t", "\tnan\t"), RECORDER_COLUMNS)


class FakeSocket:
    def __init__(self):
        self.sent = []
        self.closed = False

    def sendto(self, data, destination):
        self.sent.append((data, destination))

    def close(self):
        self.closed = True


class BridgeTests(unittest.TestCase):
    def test_audits_before_sending_versioned_packet(self):
        output = io.StringIO()
        udp = FakeSocket()
        bridge = Bridge(("127.0.0.1", 5005), output, RECORDER_COLUMNS, "milliseconds", udp)

        self.assertTrue(bridge.process_line(RECORDER_ROW))

        packet = json.loads(udp.sent[0][0])
        self.assertEqual(1, packet["version"])
        self.assertEqual(0, packet["sequence"])
        self.assertEqual(30.0, packet["frontLeft"])
        self.assertEqual(100.0, packet["totalLoad"])
        self.assertIn("sent", output.getvalue())

    def test_rejects_timestamp_regression_without_udp(self):
        output = io.StringIO()
        udp = FakeSocket()
        bridge = Bridge(("127.0.0.1", 5005), output, RECORDER_COLUMNS, "milliseconds", udp)
        bridge.process_line(RECORDER_ROW)

        self.assertFalse(bridge.process_line(RECORDER_ROW.replace("1000", "900", 1)))
        self.assertEqual(1, len(udp.sent))
        self.assertIn("source timestamp moved backwards", output.getvalue())


if __name__ == "__main__":
    unittest.main()
