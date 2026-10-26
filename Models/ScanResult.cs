using System.Net;
using System.Net.Sockets;

namespace Scanner.Models;
class ScanResult
{
    internal IPAddress IP { get; init;}
    internal ushort Port { get; init; }
    internal ScanOutcome OutCome { get; init;}
    internal SocketError RawError { get; init; }
    internal TimeSpan Duration { get; init;}

    internal ScanResult(
        IPAddress ip,
        ushort port,
        ScanOutcome outCome,
        SocketError rawError,
        TimeSpan duration
    )
    {
        this.IP = ip;
        this.Port = port;
        this.OutCome = outCome;
        this.RawError = rawError;
        this.Duration = duration;
    }
    public override string ToString()
    {
        return $"IP: {IP, -39} | Port: {Port, -5} | OutCome: {OutCome, 11} |RawError:{RawError, -24} | Duration: {Duration.TotalMilliseconds:F2} ms";
    }
    internal static ScanResult Create(IPAddress ip, ushort port, TimeSpan duration, ScanOutcome outCome, SocketError rawError = SocketError.Success)
    {
        return new ScanResult(
            ip: ip,
            port: port,
            outCome: outCome,
            rawError: rawError,
            duration: duration
        );
    }
}