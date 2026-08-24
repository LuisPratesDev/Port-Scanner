using System.Net;
using Scanner.Response;
using Scanner.Services.HostResolver;

namespace Scanner.Parsers;
internal static class ArgumentParser
{
    //Retorna todos os endereços em processamento
    internal static HashSet<Task<Result<IPAddress[]>>> ResolveAddresses(HashSet<string> address)
    {
        HashSet<Task<Result<IPAddress[]>>> tasks = new();

        foreach(string ip in address)
        {
            tasks.Add(HostResolverService.IsValidAddress(ip));
        }

        return tasks;
    }
    //Retorna todas as portas válidas e inválidas
    internal static IEnumerable<Result<ushort>> ValidatePorts(HashSet<string> ports)
    {
        foreach(string port in ports)
        {
            if (ushort.TryParse(port, out ushort correctPort)) yield return Result<ushort>.Ok(correctPort);
            
            else yield return Result<ushort>.Error(
                "Porta inválida"
            );
        }
    }
}