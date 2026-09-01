using System.Net;
using Scanner.Response;
using Scanner.Services.HostResolver;
using Scanner.Models;
namespace Scanner.Commands;
internal class Scan
{
    //Pode escanear multiplos ips com multiplas portas ou apenas um ip com uma porta
    internal static IEnumerable<Task<ScanEvent>> ScannerPorts(Result<IPAddress[]> result, HashSet<ushort> ports)
    {
        if (result.Data != null)
        {
            foreach (IPAddress ip in result.Data)
            {
                foreach(ushort port in ports)
                {
                    yield return CreatePortScanEvent(
                        HostResolverService.PortIsOpen(ip, port)
                    );
                }
            }
        }
    }
    //transforma um Task<Result<IPAddress[]>> para Task<ScanEvent>
    internal static async Task<ScanEvent> CreateScanEvent(Task<Result<IPAddress[]>> ip)
    {
        ScanEvent scanEvent = new ScanEvent(
            Event.Type.Dns,
            await ip
        );

        return scanEvent;
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