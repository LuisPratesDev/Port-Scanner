using System.Net;
using System.Net.Sockets;

namespace Scanner.Models;
class ScanResult
{
    internal IPAddress IP { get; init;}
    internal ushort Port { get; init; }
    internal SocketError Status { get; init; }
    internal TimeSpan Duration { get; init;}

    internal ScanResult(
        IPAddress ip,
        ushort port,
        SocketError status,
        TimeSpan duration
    )
    {
        this.IP = ip;
        this.Port = port;
        this.Status = status;
        this.Duration = duration;
    }
    internal static ScanResult Create(IPAddress ip, ushort port, TimeSpan duration, SocketError status = SocketError.Success)
    {
        return new ScanResult(
            ip: ip,
            port: port,
            status: status,
            duration: duration
        );
    }
}