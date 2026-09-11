using System.Net;
using Scanner.Response;
using Scanner.Services.HostResolver;
using Scanner.Models;
namespace Scanner.Commands;
internal class Scan
{
    //Pode escanear multiplos ips com multiplas portas ou apenas um ip com uma porta
    internal static IEnumerable<Task<ScanResult>> ScannerPorts(Result<IPAddress[]> result, HashSet<ushort> ports)
    {
        if (result.Data != null)
        {
            foreach (ushort port in ports)
            {
                foreach(IPAddress ip in result.Data)
                {
                    yield return HostResolverService.PortIsOpen(ip, port);
                
                }
            }
        }
    }
}