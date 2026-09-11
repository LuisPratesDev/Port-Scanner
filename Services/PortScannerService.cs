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
        ChannelWriter<Result<IPAddress[]>> writer,
        HashSet<string> inputHosts,
        CancellationToken cancellationToken)
    {
        HashSet<Task<Result<IPAddress[]>>> pendingTasks = ArgumentParser.ResolveAddresses(inputHosts);

        try
        {
            while (pendingTasks.Count > 0)
            {
                // Aguarda a conclusão de qualquer resolução DNS pendente.
                Task<Result<IPAddress[]>> completedTask = await Task.WhenAny(pendingTasks);

                pendingTasks.Remove(completedTask);

                // Envia a tarefa concluída como um evento para o consumidor processar.
                await writer.WriteAsync(await completedTask, cancellationToken);
            }   
        }
        catch(OperationCanceledException)
        {}
        finally
        {
            // Sinaliza que nenhum novo evento será produzido.
            writer.Complete();
        }
    }

    internal static async IAsyncEnumerable<ScanProgress> ConsumeScanResults(
        ChannelReader<Result<IPAddress[]>> reader,
        HashSet<string> inputPorts,
        ScanProgress scanProgress
    )
    {
        // Mantém as tarefas de escaneamento iniciadas que ainda não foram concluídas.
        List<Task<ScanResult>> pendingTasks = new ();

        // Mantém os enumeradores responsáveis por gerar novas tarefas de escaneamento.
        Queue<IEnumerator<Task<ScanResult>>> scans = new();

        //Mostra a quantidade de portas válidas
        HashSet<ushort> validPorts = ValidatePorts(inputPorts);

        // Continua enquanto ainda existirem eventos no channel, escaneamentos pendentes
        // ou enumeradores capazes de gerar novos escaneamentos.
        while (!reader.Completion.IsCompleted || pendingTasks.Count != 0 || scans.Count != 0)
        {
            await InitialCreateScanEnumerator(
                scans,
                pendingTasks,
                reader,
                scanProgress,
                validPorts
            );

            ConsumeItensInChannel(
                reader,
                scans,
                scanProgress,
                validPorts
            );

            int capacity = CalcCapacity(
                validPorts,
                scanProgress
            );

            AddScansAtPendingTask(
                scans,
                pendingTasks, 
                capacity
            );

            if (pendingTasks.Count > 0)
            {
                yield return await ResultCompleted(
                    pendingTasks, 
                    scanProgress
                );
            }
        }
    }
    private static async Task<ScanProgress> ResultCompleted(
        List<Task<ScanResult>> pendingTasks,
        ScanProgress scanProgress
    )
    {
        // Aguarda a conclusão de qualquer escaneamento pendente.
        Task<ScanResult>  scanTask = await Task.WhenAny(pendingTasks);
        
        pendingTasks.Remove(scanTask);
            
        ScanResult scanCompleted = await scanTask;

        UpdateScanProgress(
            scanProgress, 
            scanCompleted
        );

        // Disponibiliza o progresso atualizado para a interface.
        return scanProgress;
    }
    private static void UpdateScanProgress(
        ScanProgress scanProgress,
        ScanResult scanCompleted
    )
    {
        scanProgress.Completed++;

        // Classifica o resultado do escaneamento como falha.
        if (scanCompleted.Status != System.Net.Sockets.SocketError.Success) scanProgress.Failed++;

        else scanProgress.Success++;
    }
    private static void ConsumeItensInChannel(
        ChannelReader<Result<IPAddress[]>> reader,
        Queue<IEnumerator<Task<ScanResult>>> scans,
        ScanProgress scanProgress,
        HashSet<ushort> validPorts
    )
    {
        //consome todos os itens disponíveis no channel
        while(reader.TryRead(out Result<IPAddress[]>? item) && item.Data != null)
        {
            AddScansEnumerator(
                scans: scans,
                ips: item,
                validPorts: validPorts,
                scanProgress: scanProgress
            );
        }
    }
    private static int CalcCapacity(
        HashSet<ushort> validPorts,
        ScanProgress scanProgress
    )
    {
        int totalScans = scanProgress.Hosts * validPorts.Count;

        double baseCapacity = 500 * Math.Sqrt((double)totalScans / 1000);

        double successRate = scanProgress.Completed == 0 ? 0 : (double)scanProgress.Success / scanProgress.Completed;

        double maxPercentageOfWork = 0.6;

        double relativeLimit = totalScans * maxPercentageOfWork;

        double adaptiveCapacity = baseCapacity + (relativeLimit - baseCapacity) * successRate;

        return (int)adaptiveCapacity;
    }
    private static async Task InitialCreateScanEnumerator(
        Queue<IEnumerator<Task<ScanResult>>> scans,
        List<Task<ScanResult>> pendingTasks,
        ChannelReader<Result<IPAddress[]>> reader,
        ScanProgress scanProgress,
        HashSet<ushort> validPorts
    )
    {
        if (pendingTasks.Count == 0 && scans.Count == 0 && !reader.Completion.IsCompleted)
        {
            Result<IPAddress[]> result = await reader.ReadAsync();

            AddScansEnumerator(
                scans: scans,
                ips: result,
                validPorts: validPorts,
                scanProgress: scanProgress
            );
        }
    }
    private static void AddScansAtPendingTask(
        Queue<IEnumerator<Task<ScanResult>>> scans,
        List<Task<ScanResult>> pendingTasks,
        int capacity
    )
    {
        //Preenche o pendingTasks com as Tasks De ScanResult
            if (scans.Count > 0 && pendingTasks.Count < capacity)
            {
                IEnumerator<Task<ScanResult>> enumerator = scans.Dequeue();

                // Inicia novos escaneamentos até atingir o limite de tarefas pendentes.
                int availablePositions = Math.Max(
                    0,
                    capacity - pendingTasks.Count
                );

                bool enumeratorHasMoreItems = true;

                for (int i = 0; i < availablePositions; i ++)
                {
                    if (enumerator.MoveNext())
                    {
                        pendingTasks.Add(enumerator.Current);
                    }
                    // Alterna para o próximo conjunto de escaneamentos quando o atual termina.
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

                // Recoloca o enumerador na fila caso ainda existam tarefas a serem geradas.
                if (enumeratorHasMoreItems) scans.Enqueue(enumerator);
            }
    }
    private static void AddScansEnumerator(
        Queue<IEnumerator<Task<ScanResult>>> scans,
        Result<IPAddress[]> ips, 
        HashSet<ushort> validPorts, 
        ScanProgress scanProgress)
    {
        if (ips.Data != null)
        {
            // Adiciona um novo gerador de escaneamentos para os IPs resolvidos.
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
}