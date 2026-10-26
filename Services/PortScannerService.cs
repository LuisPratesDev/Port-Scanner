using System.Net;
using System.Threading.Channels;
using Scanner.Response;
using Scanner.Models;
using Scanner.Commands;
using System.Runtime.CompilerServices;

namespace Scanner.Services.PortScanner;

internal class PortScanner
{
    internal static async Task ProcessingDns(
        ChannelWriter<Result<IPAddress[]>> writer,
        IEnumerable<Task<Result<IPAddress[]>>> hosts,
        CancellationToken cancellationToken)
    {
        HashSet<Task<Result<IPAddress[]>>> pendingTasks = hosts.ToHashSet();
        
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
        catch(Exception ex)
        {
            await writer.WriteAsync(
                Result<IPAddress[]>.Error(
                    ex.Message
                ), 
                cancellationToken
            );
        }
        finally
        {
            // Sinaliza que nenhum novo evento será produzido.
            writer.Complete();
        }
    }

    internal static async IAsyncEnumerable<ScanResult> ConsumeScanResults(
        ChannelReader<Result<IPAddress[]>> reader,
        IEnumerable<ushort> ports,
        ScanProgress scanProgress,
        [EnumeratorCancellation] CancellationToken cts
    )
    {
        // Mantém as tarefas de escaneamento iniciadas que ainda não foram concluídas.
        List<Task<ScanResult>> pendingTasks = new ();

        // Mantém os enumeradores responsáveis por gerar novas tarefas de escaneamento.
        Queue<IEnumerator<Task<ScanResult>>> scans = new();

        //Mostra a quantidade de portas válidas
        HashSet<ushort> validPorts = ports.ToHashSet();

        // Continua enquanto ainda existirem eventos no channel, escaneamentos pendentes
        // ou enumeradores capazes de gerar novos escaneamentos.
        while (!reader.Completion.IsCompleted || pendingTasks.Count != 0 || scans.Count != 0)
        {
            await InitialCreateScanEnumerator(
                scans,
                reader,
                scanProgress,
                validPorts,
                cts
            );

            ConsumeItensInChannel(
                reader,
                scans,
                scanProgress,
                validPorts,
                cts
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
                await foreach(ScanResult result in ResultCompleted(
                    pendingTasks, 
                    scanProgress,
                    cts
                ).WithCancellation(cts))
                {
                    yield return result;
                }
            }
        }
    }
    private static async IAsyncEnumerable<ScanResult> ResultCompleted(
        List<Task<ScanResult>> pendingTasks,
        ScanProgress scanProgress,
        [EnumeratorCancellation] CancellationToken cts
    )
    {
        // Aguarda a conclusão de qualquer escaneamento pendente.
        await foreach(Task<ScanResult> scanTask in Task.WhenEach(pendingTasks).WithCancellation(cts))
        {
            cts.ThrowIfCancellationRequested();

            pendingTasks.Remove(scanTask);
            
            ScanResult scanCompleted = await scanTask;

            UpdateScanProgress(
                scanProgress, 
                scanCompleted
            );
            
            // Disponibiliza o progresso atualizado para a interface.
            yield return scanCompleted;    
        }
    }
    private static void UpdateScanProgress(
        ScanProgress scanProgress,
        ScanResult scanCompleted
    )
    {
        scanProgress.Completed++;

        // Classifica o resultado do escaneamento como sucesso
        if (scanCompleted.OutCome == ScanOutcome.Open) scanProgress.Success++;

        else scanProgress.FailedConnections++;
    }
    private static void ConsumeItensInChannel(
        ChannelReader<Result<IPAddress[]>> reader,
        Queue<IEnumerator<Task<ScanResult>>> scans,
        ScanProgress scanProgress,
        HashSet<ushort> validPorts,
        CancellationToken cts
    )
    {
        //consome todos os itens disponíveis no channel
        while(reader.TryRead(out Result<IPAddress[]>? item))
        {
            AddScansEnumerator(
                scans: scans,
                ips: item,
                validPorts: validPorts,
                scanProgress: scanProgress,
                cts: cts
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
        ChannelReader<Result<IPAddress[]>> reader,
        ScanProgress scanProgress,
        HashSet<ushort> validPorts,
        CancellationToken cts
    )
    {
        while (reader.TryRead(out Result<IPAddress[]>? item))
        {
            AddScansEnumerator(
                scans: scans,
                ips: item,
                validPorts: validPorts,
                scanProgress: scanProgress,
                cts: cts
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
        ScanProgress scanProgress,
        CancellationToken cts
    )
    {
        if (ips.Success)
        {
            // Adiciona um novo gerador de escaneamentos para os IPs resolvidos.
            scans.Enqueue(
                ScanCommand.ScannerPorts(
                    ips,
                    validPorts,
                    cts
                ).GetEnumerator()
            );
            
            scanProgress.Hosts += ips.Data!.Length;
            scanProgress.Ports = (ushort)validPorts.Count;
            scanProgress.Total = (uint)scanProgress.Hosts * scanProgress.Ports;
        }  
        else scanProgress.FailedHosts++;
    }
}