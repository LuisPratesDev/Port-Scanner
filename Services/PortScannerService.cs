using Scanner.Commands;
using Scanner.Models;
using Scanner.Parsers;
using Scanner.Response;
using System.Net;

internal class PortScannerService
{
    internal IAsyncEnumerable<ScanResult> Processing(HashSet<string> address, HashSet<string> ports)
    {
        HashSet<Task<ScanEvent>> scanEvents = new();

        foreach (Task<Result<IPAddress[]>> ip in ArgumentParser.ResolveAddresses(address))
        {
           scanEvents.Add(CreateScanEvent(ip));
        }

        HashSet<ushort> resultPorts = ArgumentParser.ValidatePorts(ports)
        .Where(port => port.Success)
        .Select(port => port.Data)
        .ToHashSet();

        return Scan.ScannerPorts(scanEvents, resultPorts);
    }
    private async Task<ScanEvent> CreateScanEvent(Task<Result<IPAddress[]>> ip)
    {
        ScanEvent scanEvent = new ScanEvent(
            Event.Type.Dns,
            await ip
        );

        return scanEvent;
    }
}