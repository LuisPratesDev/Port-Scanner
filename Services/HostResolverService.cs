using System.Net;
using System.Net.Sockets;
using Scanner.Response;
using Scanner.Models;
using System.Diagnostics;
using Scanner.Services.OutComeClassifier;

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
    internal static async Task<ScanResult> ConnectionInPort(IPAddress ip, ushort port, CancellationToken appCancel)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        using TcpClient client = new();

        try
        {
            using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(
                appCancel
            );

            cts.CancelAfter(
                TimeSpan.FromSeconds(2)
            );

            await client.ConnectAsync(ip, port, cts.Token);

            return ScanResult.Create(
                ip: ip,
                port: port,
                outCome: ScanOutcome.Open,
                duration: stopwatch.Elapsed,
                rawError: SocketError.Success
            );
        }
        catch(OperationCanceledException)
        {
            return ScanResult.Create(
                ip: ip,
                port: port,
                outCome: Classifier.Classifying(
                    error: SocketError.TimedOut, 
                    appCancelled: false
                ),
                duration: stopwatch.Elapsed,
                rawError: SocketError.TimedOut
            );
        }
        catch (SocketException ex)
        {
            return ScanResult.Create(
                ip: ip,
                port: port,
                outCome: Classifier.Classifying(
                    error: ex.SocketErrorCode, 
                    appCancelled: false
                ),
                duration: stopwatch.Elapsed,
                rawError: ex.SocketErrorCode
            );
        }
    }
}