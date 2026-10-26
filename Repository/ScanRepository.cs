using Scanner.Models;
using Scanner.Response;

namespace Scanner.Respository;
internal class ScanRepository : IDisposable
{
    StreamWriter streamWriter = File.AppendText(
        Path.Combine(
            Directory.GetCurrentDirectory(),
            "ScanTemp.txt"
        )
    );
    public void Dispose()
    {
        streamWriter.Dispose();
    }
    //Retorna os scans completos em ordem de chegada para o UI e salva em um arquivo temporário
    internal Result<bool> SaveInfoInFileTemp(ScanResult scanResult)
    {
        try
        {
            streamWriter.WriteLine(scanResult);
            return Result<bool>.Ok(); 
        }
        catch (Exception ex)
        {
            return Result<bool>.Error(
                ex.Message
            );
        }
    }
    //Move o arquivo temporário para o diretório desejado
    internal Result<string> MoveFileCompleted(
        string scanTempPath, 
        string scanResultPath
    )
    {
        try
        {
            File.Move(
                scanTempPath,
                scanResultPath
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
}