namespace Scanner.Models;
internal class ScanProgress
{
    internal int Hosts { get; set; }
    internal ushort Ports { get; set; }
    internal uint Total { get; set; }
    internal uint Completed { get; set; }
    internal uint Success { get; set; }
    internal uint Failed { get; set; }

    internal ScanProgress(
        int hosts,
        ushort ports,
        uint total,
        uint completed,
        uint success,
        uint failed
    )
    {
        Hosts = hosts;
        Ports = ports;
        Total = total;
        Completed = completed;
        Success = success;
        Failed = failed;
    }
    internal static ScanProgress Create(
        int hosts,
        ushort ports,
        uint total,
        uint completed,
        uint success,
        uint failed)
    {
        return new ScanProgress(
            hosts,
            ports,
            total,
            completed,
            success,
            failed
        );
    }
}