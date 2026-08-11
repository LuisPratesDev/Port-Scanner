internal class Result<T>
{
    //Data é as informações que poderam ser passadas
    internal T? Data { get; init; }
    //Sucess retorna se a operação foi bem sucedida ou má sucedida
    internal bool Sucess { get; init; }
    //Errors é as mensagens de erros ao longo do percurso
    internal string[] Erros {get; init; }

    internal Result(bool sucess, string[] errors, T? data = default)
    {
        this.Data = data;
        this.Sucess = sucess;
        this.Erros = errors;
    }

    //Aqui temos um método estático, que retorna diretamente o objeto Result com as informações desejadas para um sucesso
    static internal Result<T> Ok(T? data = default)
    {
        return new Result<T>(
            data: data,
            sucess: true,
            errors: []
        );
    }
    //Um outro método estático semelhante ao Ok, porém é para erros que pode ter ou não um valor de data para passar adiante
    static internal Result<T> Error(string[] errors, T? data = default)
    {
        return new Result<T>(
            data: data,
            sucess: false,
            errors: errors
        );
    }
}