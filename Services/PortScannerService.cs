using Scanner.Commands;
using Scanner.Models;
using Scanner.Parsers;
using Scanner.Response;
using System.Net;

namespace Scanner.Services.PortScanner;
internal class PortScannerService
{
    //Processa as informaçãos da UI
    internal IAsyncEnumerable<ScanResult> Processing(HashSet<Task<Result<IPAddress[]>>> address, HashSet<ushort> ports)
    {
        HashSet<Task<ScanEvent>> scanEvents = new();

        foreach (Task<Result<IPAddress[]>> ip in address)
        {
           scanEvents.Add(CreateScanEvent(ip));
        }

        return Scan.ScannerPorts(scanEvents, ports);
    }
    //transforma um Task<Result<IPAddress[]>> para Task<ScanEvent>
    private async Task<ScanEvent> CreateScanEvent(Task<Result<IPAddress[]>> ip)
    {
        ScanEvent scanEvent = new ScanEvent(
            Event.Type.Dns,
            await ip
        );

        return scanEvent;
    }
}