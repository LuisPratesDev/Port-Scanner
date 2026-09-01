using System.Threading.Channels;
using Spectre.Console;

using Scanner.Models;
using Scanner.UI.Prompt.Scan;
using Scanner.Services.PortScanner;

namespace Scanner.UI.View;

internal class ScanView
{
    //Executa a visualização dos resultados dos scans de acordo com os inputs
    internal async Task RunScanAsync(
        Channel<Task<ScanEvent>> channel,
        CancellationToken cancellationToken
    )
    {
        HashSet<string> inputHosts = ScanPrompt.AskHosts();
        HashSet<string> inputPorts = ScanPrompt.AskPorts();

        Layout root = CreateLayout();
        
        await AnsiConsole.Live(root)
        .StartAsync(async display =>
        {
            // Inicia a produção dos eventos de resolução DNS no channel.
            Task producer = PortScanner.ProcessingDns(
                channel.Writer,
                inputHosts,
                cancellationToken
            );

            ScanProgress progress = ScanProgress.Create(
                hosts: 0,
                ports: 0,
                total: 0,
                completed: 0,
                success: 0,
                failed: 0
            );

            // Consome os eventos produzidos pelo scanner conforme são concluídos.
            await foreach (
                ScanProgress scanResult
                in PortScanner.ConsumeScanEvents(
                    channel.Reader,
                    inputPorts,
                    progress
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

            // Garante que o produtor conclua antes de encerrar a execução da View.
            await producer;
        });
    }

    private Panel CreateScanProgressPanel(
        ScanProgress progress,
        HashSet<string> inputHosts,
        HashSet<string> inputPorts
    )
    {
        return new Panel(
            $"[green]Processing scans...[/]\n\n" +

            $"Input Hosts: {inputHosts.Count}\n" +
            $"Input Ports: {inputPorts.Count}\n" +
            $"Hosts Valid: {progress.Hosts}\n" +
            $"Ports Valid: {progress.Ports}\n" +
            $"Total Valid: {progress.Total}\n\n" +

            $"Completed: {progress.Completed}\n" +
            $"Success: {progress.Success}\n" +
            $"Failed: {progress.Failed}"
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
                new Panel(
                    new Markup(
                        $"[green]Processing scans...[/]\n\n" +

                        $"Input Hosts: 0\n" +
                        $"Input Ports: 0\n" +
                        $"Hosts Valid: 0\n" +
                        $"Ports Valid: 0\n" +
                        $"Total Valid: 0\n\n" +

                        $"Completed: 0\n" +
                        $"Success: 0\n" +
                        $"Failed: 0"
                    )
                ).Collapse()
            )
        );

        return root;
    }
}