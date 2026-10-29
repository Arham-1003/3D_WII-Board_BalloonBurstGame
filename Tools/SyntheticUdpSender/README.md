# Synthetic UDP sender

Run `dotnet run --project Tools/SyntheticUdpSender/SyntheticUdpSender.csproj` from the repository root. It writes every generated four-load packet to an in-progress raw CSV before sending the matching version 1 JSON packet to `127.0.0.1:5005` at approximately 50 Hz. This is a development source, not a physical-board bridge.
