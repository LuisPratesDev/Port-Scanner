using Scanner.Interfaces;

namespace Scanner.Response;
internal class Result<T> : IResult
{
    //Data é as informações que poderam ser passadas
    internal T? Data { get; init; }
    //Success retorna se a operação foi bem sucedida ou má sucedida
    internal bool Success { get; init; }
    //Errors é as mensagens de erros ao longo do percurso
    internal List<string> Errors {get; init; }

    internal Result(bool success, List<string> errors, T? data = default)
    {
        this.Data = data;
        this.Success = success;
        this.Errors = errors;
    }

    //Aqui temos um método estático, que retorna diretamente o objeto Result com as informações desejadas para um Successo
    static internal Result<T> Ok(T? data = default)
    {
        return new Result<T>(
            data: data,
            success: true,
            errors: []
        );
    }
    //Um outro método estático semelhante ao Ok, porém é para Errors que pode ter ou não um valor de data para passar adiante
    static internal Result<T> Error(List<string> errors, T? data = default)
    {
        return new Result<T>(
            data: data,
            success: false,
            errors: errors
        );
    }
    //Um método estático que permite a adição de apenas uma string a Lista de erros de um objeto Result<T>
    static internal Result<T> Error(string error, T? data = default)
    {
        return new Result<T>(
            data: data,
            success: false,
            errors: new List<string>() 
            {
                error
            }
        );
    }
    //Um método estáttico que permite a adição de erros em um objeto Result<T>
    static internal Result<T> Error(Result<T> result, string addError)
    {
        result.Errors.Add(addError);
        return result;
    }
}