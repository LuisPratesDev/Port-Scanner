using System.Net;
using Scanner.Response;
using Scanner.Services.HostResolver;
using Scanner.Models;
namespace Scanner.Commands;
internal class Scan
{
    //Pode escanear multiplos ips com multiplas portas ou apenas um ip com uma porta
    internal static async IAsyncEnumerable<ScanResult> ScannerPorts(HashSet<Task<ScanEvent>> scanEvents, HashSet<ushort> ports)
    {
        while (scanEvents.Count > 0)
        {
            Task<ScanEvent> task = await Task.WhenAny(scanEvents);

            scanEvents.Remove(task);

            ScanEvent eventTask = await task;

            if (eventTask.Type == Event.Type.PortScanner)
            {
                ScanResult result = (ScanResult)eventTask.Data;
                yield return result;
            }

            if (eventTask.Type == Event.Type.Dns)
            {
                Result<IPAddress[]> ips = (Result<IPAddress[]>)eventTask.Data;

                if (!ips.Success || ips.Data == null) continue;

                foreach (IPAddress ip in ips.Data)
                {
                    foreach(ushort port in ports)
                    {
                        scanEvents.Add(CreatePortScanEvent(HostResolverService.PortIsOpen(ip, port)));
                    }
                }
            }
        }
    }

    //Converte um Task<ScanResult> para Task<ScanEvent> para o fluxo do código continuar
    private static async Task<ScanEvent> CreatePortScanEvent(Task<ScanResult> task)
    {
        ScanResult result = await task;

        return ScanEvent.Create(
            Event.Type.PortScanner,
            result
        );
    }
}