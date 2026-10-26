namespace Scanner.Models;
internal class ScanProgress
{
    internal int Hosts { get; set; }
    internal int FailedHosts { get; set; }
    internal ushort Ports { get; set; }
    internal uint Total { get; set; }
    internal uint Completed { get; set; }
    internal uint Success { get; set; }
    internal uint FailedConnections { get; set; }

    internal ScanProgress(
        int hosts,
        int failedHosts,
        ushort ports,
        uint total,
        uint completed,
        uint success,
        uint failedConnections
    )
    {
        Hosts = hosts;
        FailedHosts = failedHosts;
        Ports = ports;
        Total = total;
        Completed = completed;
        Success = success;
        FailedConnections = failedConnections;
    }
}