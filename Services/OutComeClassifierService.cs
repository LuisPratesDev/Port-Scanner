using System.Net.Sockets;
using Scanner.Models;

namespace Scanner.Services.OutComeClassifier;

static class Classifier
{
    internal static ScanOutcome Classifying(
        SocketError error,
        bool appCancelled
    )
    {
        if (!appCancelled)
        {
            ScanOutcome outCome = error switch
            {
                SocketError.Success => ScanOutcome.Open,
                SocketError.ConnectionRefused => ScanOutcome.Closed,
                SocketError.ConnectionReset => ScanOutcome.Closed,
                SocketError.HostUnreachable => ScanOutcome.Unreachable,
                SocketError.NetworkUnreachable => ScanOutcome.Unreachable,
                _ => ScanOutcome.Filtered
            };
            return outCome;
        }
        return ScanOutcome.Aborted;
    }
}