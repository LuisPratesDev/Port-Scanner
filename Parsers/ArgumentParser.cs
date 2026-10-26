using System.Globalization;
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
    //Retorna todas as portas válidas
    internal static HashSet<ushort> ValidatePorts(HashSet<string> ports)
    {
        HashSet<ushort> results = new();
        foreach(string port in ports)
        {
            if 
            (
                ushort.TryParse(
                    port.Trim(),
                    out ushort correctPort
                ) && 
                correctPort >= 1
            ) 
                results.Add(
                correctPort
            );
        }
        return results;
    }
}