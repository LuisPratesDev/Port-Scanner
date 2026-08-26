using Scanner.Models;
using Scanner.Services.PortScanner;

namespace Scanner.Services.ScanResultFile;
internal class ScanResultFileService
{
    internal async IAsyncEnumerable<ScanResult> SaveInfoInFileTemp(HashSet<string> address, HashSet<string> ports)
    {
        string filePath = PathFileTemp();

        PortScannerService portScanner = new();

        IAsyncEnumerable<ScanResult> scanResults = portScanner.Processing(address, ports);
        
        using StreamWriter streamWriter = new StreamWriter(filePath);

        await foreach(ScanResult scanResult in scanResults)
        {
            streamWriter.WriteLine(scanResult.ToString());
            yield return scanResult;
        }
    }
    internal (bool Success, string message) MoveFileCompleted(string directory)
    {
        try
        {
            DateTime dateTime = DateTime.UtcNow;

            File.Move(
                Path.GetFullPath(
                    "ScanTemp.txt"
                ),
                Path.Combine(
                    directory,
                    $"ScanResult-{
                        dateTime.ToShortDateString()
                        .Replace("/", "-")
                    }_{
                        dateTime.ToLongTimeString()
                        .Replace(":", "-")
                    }-.txt"
                )
            );

            return (
                true,
                "Arquivo Salvo com sucesso."
            );
        }
        catch (Exception ex)
        {
            return (
                false,
                ex.Message
            );
        }
    }
    private string PathFileTemp()
    {
        string pathFileTemp = Path.GetFullPath("ScanTemp.txt");

        if (File.Exists(pathFileTemp))
        {
            File.Delete(pathFileTemp);
        }

        return pathFileTemp;
    }
}