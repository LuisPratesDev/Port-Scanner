using System.Threading.Channels;
using Scanner.Response;
using Scanner.UI.View;
using System.Net;

// O Scanner ainda está em desenvolvimento, mas essa versão já pode ser usada

Channel<Result<IPAddress[]>> channel = Channel.CreateUnbounded<Result<IPAddress[]>>();

using CancellationTokenSource cts = new();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

ScanView scanView = new();

await scanView.RunScanAsync(
    channel,
    cts.Token
);