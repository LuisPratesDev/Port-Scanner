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
                // Aguarda a conclusão de qualquer resolução DNS pendente.
                Task<Result<IPAddress[]>> completedTask = await Task.WhenAny(pendingTasks);

                pendingTasks.Remove(completedTask);

                // Envia a tarefa concluída como um evento para o consumidor processar.
                await writer.WriteAsync(Scan.CreateScanEvent(completedTask), cancellationToken);
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

    internal static async IAsyncEnumerable<ScanProgress> ConsumeScanEvents(
        ChannelReader<Task<ScanEvent>> reader,
        HashSet<string> inputPorts,
        ScanProgress scanProgress
    )
    {
        // Mantém as tarefas de escaneamento iniciadas que ainda não foram concluídas.
        List<Task<ScanEvent>> pendingTasks = new ();

        // Mantém os enumeradores responsáveis por gerar novas tarefas de escaneamento.
        Queue<IEnumerator<Task<ScanEvent>>> scans = new();

        HashSet<ushort> validPorts = ValidatePorts(inputPorts);

        bool enumeratorHasMoreItems;

        // Continua enquanto ainda existirem eventos no channel, escaneamentos pendentes
        // ou enumeradores capazes de gerar novos escaneamentos.
        while (!reader.Completion.IsCompleted || pendingTasks.Count != 0 || scans.Count != 0)
        {
            enumeratorHasMoreItems = true;

            // Garante uma tarefa pendente enquanto o channel ainda pode produzir eventos.
            if (pendingTasks.Count == 0 && !reader.Completion.IsCompleted)
            {
                pendingTasks.Add(await reader.ReadAsync());
            }

            // Aguarda a conclusão de qualquer escaneamento pendente.
            Task<Task<ScanEvent>>  waitingScan = Task.WhenAny(pendingTasks);

            // Aguarda a disponibilidade de um novo evento produzido pelo DNS.
            Task<bool>  waitingNewTask = reader.WaitToReadAsync().AsTask();

            // Processa primeiro aquilo que ocorrer antes: um novo evento ou
            // a conclusão de um escaneamento já iniciado.
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

                // Classifica o resultado do escaneamento como sucesso ou falha.
                if (scanResult.Status != System.Net.Sockets.SocketError.Success) scanProgress.Failed++;

                else scanProgress.Success++;

                // Disponibiliza o progresso atualizado para a interface.
                yield return scanProgress;
            }

            else {
                Result<IPAddress[]> ips = (Result<IPAddress[]>)completedTask.Data;

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
            

            if (scans.Count > 0)
            {
                IEnumerator<Task<ScanEvent>> enumerator = scans.Dequeue();

                // Inicia novos escaneamentos até atingir o limite de tarefas pendentes.
                while(pendingTasks.Count < 500)
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
    }
}