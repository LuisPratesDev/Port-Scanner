using Scanner.Models;
using Scanner.Response;

namespace Scanner.Respository.Scan;
internal class ScanRepository
{
    //Retorna os scans completos em ordem de chegada para o UI e salva em um arquivo temporário
    internal async IAsyncEnumerable<ScanResult> SaveInfoInFileTemp(ScanResult scanResult)
    {
        string filePath = PathFileTemp();
        
        using StreamWriter streamWriter = new StreamWriter(filePath);

        streamWriter.WriteLine(
            scanResult.ToString()
        );

        yield return scanResult;
    }
    //Move o arquivo temporário para o diretório desejado
    internal Result<string> MoveFileCompleted(string directory)
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

            return Result<string>.Ok(
                "Arquivo Salvo com sucesso."
            );
        }
        catch (Exception error)
        {
            return Result<string>.Error(
                error.Message
            );
        }
    }
    //Retorna o caminho do arquivo temporário
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