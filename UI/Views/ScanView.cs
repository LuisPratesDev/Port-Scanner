using System.Net;
using Spectre.Console;

using Scanner.Services.PortScanner;
using Scanner.Models;
using Scanner.Response;
using Scanner.UI.Prompt.Scan;
using Scanner.Parsers;

namespace Scanner.UI.Scan.View;
internal class ScanView
{
    private int ResolvedHost;
    private int ScanCompleted;
    private int ScanSucess;
    private int ScanFailed;
    internal async void Main()
    {
        PortScannerService portScanner = new();

        HashSet<string> inputHosts = ScanPrompt.AskHosts();
        HashSet<string> inputPorts = ScanPrompt.AskPorts();

        HashSet<ushort> ports = ArgumentParser.ValidatePorts(inputPorts)
        .Where(port => port.Success)
        .Select(port => port.Data)
        .ToHashSet();

        Layout root = CreateLayout();

        HashSet<Task<Result<IPAddress[]>>> resolvedHosts = ArgumentParser.ResolveAddresses(inputHosts);

        IAsyncEnumerable<ScanResult> scanResults = portScanner.Processing(resolvedHosts, ports);

        while (resolvedHosts.Count > 0)
        {
            Task<Result<IPAddress[]>> task =  await Task.WhenAny(resolvedHosts);

            resolvedHosts.Remove(task);
            
            Result<IPAddress[]> completedTask = await task;

            AnsiConsole.Live(root)
            .AutoClear(true)
            .Start(ctx =>
            {
                root["main"]["info"].Update(
                    ChangeContentHost(inputHosts, completedTask, ports)
                );

                ctx.UpdateTarget(root);
            });
        }

    }
    private Panel ChangeContentHost(HashSet<string> inputHosts, Result<IPAddress[]> resolved, HashSet<ushort> ports)
    {
        foreach(IPAddress ips in resolved.Data!)
        {
            ResolvedHost++;
        }

        return new Panel(
            $"[green]Processing hosts...[/]\n\n" +

            $"Inputs: {inputHosts.Count}\n" +
            $"Resolved: {ResolvedHost}\n" +
            $"Ports: {ports.Count}"
        );
    }
    private Panel ChangeContentScan(ScanResult scanResult, HashSet<ushort> ports)
    {
        ScanCompleted++;

        if (scanResult.Status != System.Net.Sockets.SocketError.Success) ScanFailed++;

        else ScanSucess++;

        return new Panel(
            $"[green]Processing scans...[/]\n\n"+

            $"Hosts: {ResolvedHost}\n" +
            $"Ports: {ports.Count}\n" +
            $"Total: {ResolvedHost*ports.Count}\n\n" +

            $"Completed: {ScanCompleted}\n" +
            $"Success: {ScanSucess}\n" +
            $"Failed: {ScanFailed}"
        );
    }
    private static Layout CreateLayout()
    {
        Layout root = new Layout("root");
        
        root.SplitRows(
            new Layout("main")
            .Ratio(1)
        );

        root["main"].SplitRows(
            new Layout(
                "info", 
                new Panel(
                    new Markup(
                        "[green]Processing hosts...[/]\n\n" +

                        "Inputs: 0\n" +
                        "Resolved: 0\n" +
                        "Ports: 0"
                    )
                ).Collapse()
            )
        );

        return root;
    }
}