using System.Net;
using System.Threading.Channels;
using Scanner.Response;
using Scanner.Parsers;
using Scanner.Models;
using Scanner.Commands;

namespace Scanner.Services.PortScanner;

internal class PortScanner
{
    private static HashSet<ushort> ValidatePorts(HashSet<string> ports)
    {
        return ArgumentParser.ValidatePorts(ports)
            .Where(port => port.Success)
            .Select(port => port.Data)
            .ToHashSet()
        ;
    }
    internal static async Task ProcessingDns(
        ChannelWriter<Task<ScanEvent>> writer,
        HashSet<string> inputHosts,
        CancellationToken cancellationToken)
    {
        HashSet<Task<Result<IPAddress[]>>> pendingTasks = ArgumentParser.ResolveAddresses(inputHosts);

        try
        {
            while (pendingTasks.Count > 0)
            {
                Task<Result<IPAddress[]>> completedTask = await Task.WhenAny(pendingTasks);

                pendingTasks.Remove(completedTask);

                await writer.WriteAsync(Scan.CreateScanEvent(completedTask), cancellationToken);
            }   
        }
        catch(OperationCanceledException)
        {}
        finally
        {
            writer.Complete();
        }
    }
    internal static async IAsyncEnumerable<ScanProgress> ConsumeScanEvents(
        ChannelReader<Task<ScanEvent>> reader,
        HashSet<string> inputPorts,
        ScanProgress scanProgress
    )
    {
        List<Task<ScanEvent>> pendingTasks = new ();
        Queue<IEnumerator<Task<ScanEvent>>> scans = new();
        HashSet<ushort> validPorts = ValidatePorts(inputPorts);

        bool enumeratorHasMoreItems;

        while (!reader.Completion.IsCompleted || pendingTasks.Count != 0 || scans.Count != 0)
        {
            enumeratorHasMoreItems = true;

            if (pendingTasks.Count == 0 && !reader.Completion.IsCompleted)
            {
                pendingTasks.Add(await reader.ReadAsync());
            }

            Task<Task<ScanEvent>>  waitingScan = Task.WhenAny(pendingTasks);

            Task<bool>  waitingNewTask = reader.WaitToReadAsync().AsTask();

            Task winnerTask = await Task.WhenAny(waitingNewTask, waitingScan);

            ScanEvent completedTask;

            if (winnerTask == waitingNewTask && await waitingNewTask)
            {
                completedTask = await await reader.ReadAsync();
            }
            else 
            {
                Task<ScanEvent> task = await waitingScan;
                pendingTasks.Remove(task);
                completedTask = await task;
            }


            if (completedTask.Type == Event.Type.PortScanner)
            {
                ScanResult scanResult = (ScanResult)completedTask.Data;

                scanProgress.Completed++;

                if (scanResult.Status != System.Net.Sockets.SocketError.Success) scanProgress.Failed++;

                else scanProgress.Success++;

                yield return scanProgress;
            }

            else {
                Result<IPAddress[]> ips = (Result<IPAddress[]>)completedTask.Data;

                if (ips.Data != null)
                {
                    scans.Enqueue(
                        Scan.ScannerPorts(
                            ips,
                            validPorts
                        ).GetEnumerator()
                    );
                    
                    scanProgress.Hosts += ips.Data.Length;
                    scanProgress.Ports = (ushort)validPorts.Count;
                    scanProgress.Total = (uint)scanProgress.Hosts * scanProgress.Ports;
                }
            }
            

            if (scans.Count > 0)
            {
                IEnumerator<Task<ScanEvent>> enumerator = scans.Dequeue();

                while(pendingTasks.Count < 500)
                {
                    if (enumerator.MoveNext())
                    {
                        pendingTasks.Add(enumerator.Current);
                    }
                    else if (scans.Count > 0)
                    {
                        enumerator = scans.Dequeue();
                    }
                    else 
                    {
                        enumeratorHasMoreItems = false;
                        break;
                    }
                }

                if (enumeratorHasMoreItems) scans.Enqueue(enumerator);
            }
        }
    }
}