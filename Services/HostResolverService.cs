using System.Net;
using System.Net.Sockets;
using Scanner.Response;
using Scanner.Models;
using System.Diagnostics;

namespace Scanner.Services.HostResolver;
abstract class HostResolverService
{
    // Retorna os endereços IP associados ao DNS ou o próprio IP informado.
    internal static async Task<Result<IPAddress[]>> IsValidAddress(string address)
    {
        try
        {
            return Result<IPAddress[]>.Ok(
                await Dns.GetHostAddressesAsync(address) //retorna todos os ipv4 ou ipv6 endereçado a um dns
            );
        }
        catch(SocketException)
        {
            return Result<IPAddress[]>.Error(
                "DNS inválido."
            );
        }
    }
    // Verifica se uma porta está aberta em um endereço IPv4 ou IPv6.
    internal static async Task<ScanResult> PortIsOpen(IPAddress ip, ushort port)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        using TcpClient client = new();

        try
        {
            await client.ConnectAsync(ip, port);

            return ScanResult.Create(
                ip: ip,
                port: port,
                duration: stopwatch.Elapsed,
                status: SocketError.Success
            );
        }
        catch (SocketException ex)
        {
            return ScanResult.Create(
                ip: ip,
                port: port,
                duration: stopwatch.Elapsed,
                status: ex.SocketErrorCode
            );
        }
    }
}