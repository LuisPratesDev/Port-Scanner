using System.Threading.Channels;
using Spectre.Console;

using Scanner.Models;
using Scanner.UI.Prompt.Scan;
using Scanner.Services.PortScanner;
using Scanner.Response;
using System.Net;

namespace Scanner.UI.View;

internal class ScanView
{
    //Executa a visualização dos resultados dos scans de acordo com os inputs
    internal async Task RunScanAsync(
        Channel<Result<IPAddress[]>> channel,
        CancellationTokenSource cts
    )
    {
        HashSet<Task<Result<IPAddress[]>>> inputHosts = ScanPrompt.AskHosts();
        HashSet<ushort> inputPorts = ScanPrompt.AskPorts();

        // Inicia a produção dos eventos de resolução DNS no channel.
        Task producer = PortScanner.ProcessingDns(
            channel.Writer,
            inputHosts,
            cts.Token
        );
        
        Layout root = CreateLayout();
        
        await AnsiConsole.Live(root)
        .StartAsync(async display =>
        {

            ScanProgress progress = new ScanProgress(
                hosts: 0,
                failedHosts: 0,
                ports: 0,
                total: 0,
                completed: 0,
                success: 0,
                failedConnections: 0
            );

            // Consome os eventos produzidos pelo scanner conforme são concluídos.
            try
            {
                await foreach (
                    ScanResult scanResult
                    in PortScanner.ConsumeScanResults(
                        channel.Reader,
                        inputPorts,
                        progress,
                        cts.Token
                    )
                )
                {
                    root["main"]["info"].Update(
                        CreateScanProgressPanel(
                            progress,
                            inputHosts,
                            inputPorts
                        )
                    );

                    display.Refresh();
                }
            }
            catch
            {
                
            }

            // Garante que o produtor conclua antes de encerrar a execução da View.
            await producer;
        });
    }

    private static Panel CreateScanProgressPanel(
        ScanProgress progress,
        HashSet<Task<Result<IPAddress[]>>> inputHosts,
        HashSet<ushort> inputPorts
    )
    {
        return new Panel(
            $"[green]Processing scans...[/]\n\n" +

            $"Input Hosts: {inputHosts.Count}\n" +
            $"Input Ports: {inputPorts.Count}\n" +
            $"Hosts Valid: {progress.Hosts}\n" + 
            $"Hosts Failed: {progress.FailedHosts}\n" +
            $"Ports Valid: {progress.Ports}\n" +
            $"Total Valid: {progress.Total}\n\n" +

            $"Completed: {progress.Completed}\n" +
            $"Success: {progress.Success}\n" +
            $"Failed: {progress.FailedConnections}"
        );
    }

    // Cria a estrutura inicial utilizada pela interface durante o escaneamento.
    private static Layout CreateLayout()
    {
        Layout root = new Layout("root");

        root.SplitRows(
            new Layout("main").Ratio(1)
        );

        root["main"].SplitRows(
            new Layout(
                "info",
                CreateScanProgressPanel(
                    new ScanProgress(
                        hosts: 0,
                        failedHosts: 0,
                        ports: 0,
                        total: 0,
                        completed: 0,
                        success: 0,
                        failedConnections: 0
                    ),
                    new HashSet<Task<Result<IPAddress[]>>>(),
                    new HashSet<ushort>()
                )
            )
        );

        return root;
    }
}