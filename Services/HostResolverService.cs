using System.Net;
using System.Net.Sockets;
using Scanner.Response;

namespace Scanner.Services.HostResolver;
abstract class HostResolverService
{
    internal static Result<IPAddress[]> IsValidAddress(string address)
    {
        try
        {
            return Result<IPAddress[]>.Ok(
                Dns.GetHostAddresses(address)
            );
        }
        catch(SocketException)
        {
            return Result<IPAddress[]>.Error(
                "DNS inválido."
            );
        }
    }
    internal static async Task<Result<string>> PortIsOpen(IPAddress ip, ushort port)
    {
        using TcpClient client = new(); 
        try
        {
            await client.ConnectAsync(ip, port);

            return Result<string>.Ok(
                $"ip: {ip} - porta: {port} aberta."
            );
        }
        catch (SocketException ex)
        {
            return Result<string>.Error(
                $"ip: {ip} - porta: {port} {ex.Message}"
            );
        }
    }
}