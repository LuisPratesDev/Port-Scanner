using System.Net;
using Scanner.Response;
using Scanner.Services.HostResolver;

namespace Scanner.Parsers;
internal static class ArgumentParser
{
    //Retorna todos os endereços que ficarem prontos
    internal static async IAsyncEnumerable<Result<IPAddress[]>> ResolveAddresses(HashSet<string> address)
    {
        HashSet<Task<Result<IPAddress[]>>> results = new();

        foreach(string result in address)
        {
            results.Add(HostResolverService.IsValidAddress(result));
        }

        while(results.Count > 0)
        {
            Task<Result<IPAddress[]>> task = await Task.WhenAny(results);

            results.Remove(task);

            Result<IPAddress[]> completedTask = await task;

            yield return completedTask;
        }
    }
    //Retorna todas as portas válidas
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