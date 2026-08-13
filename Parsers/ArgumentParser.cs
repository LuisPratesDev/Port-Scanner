using Scanner.Response;
namespace Scanner.Parsers;
internal static class ArgumentParser
{
    internal static Result<string[]> ValidateArgument(string[] args)
    {
        //Verifico se há algum argumento
        if (args.Length == 0) return Result<string[]>.Error(
            "Não há nenhum argumento."
        );

        //verifico se o primeiro argumento é equivalente ao comando "scan"
        if (!args[0].Equals("scan", StringComparison.InvariantCultureIgnoreCase)) return Result<string[]>.Error(
            "O primeiro comando não é scan."
        );

        //verifica se tem menos que 3 elementos ou mais do que quatro elementos
        if (args.Length < 3 || args.Length > 4) return Result<string[]>.Error(
            "Argumentos insuficientes para continuar."
        );

        //retorna uma tupla com success caso o elemento correspondente ao indice é um número e retorna o valor do número
        var elementSecondIsValid = IsValidIndex(args, 2) ? TryGetNumberArgument(args, 2) : (success: false, value: 0);
        var elementThirdIsValid = IsValidIndex(args, 3) ? TryGetNumberArgument(args, 3) : (success: false, value: 0);

        //verifico se existe um elemento correspondente ao indice 2 e verifico se esse elemento é um número e se é maior que 0
        if (
            IsValidIndex(args, 2) &&
            !(
                elementSecondIsValid.success &&
                elementSecondIsValid.value > 0
            )
        ) return Result<string[]>.Error(
            "O terceiro argumento é inválido"
        );

        /*
            verifico se existe um elemento correspondente ao indice 3 e
            verifico se esse elemento é um número e se é maior que elemento no indice 2
        */
        if (
            IsValidIndex(args, 3) &&
            !(
                elementThirdIsValid.success &&
                elementThirdIsValid.value > elementSecondIsValid.value
            )
        ) return Result<string[]>.Error(
            "O quarto argumento é inválido"
        );
        
        
        //retorna o valor do array formatado corretamente
        return Result<string[]>.Ok(
            FormatArguments(args)
        );
    }
    private static string[] FormatArguments(string[] args)
    {
        if (IsValidIndex(args, 3)) return new string[]
        {
          args[0].ToLowerInvariant(),
          args[1],
          args[2],
          args[3]  
        };

        return new string[]
        {
            args[0].ToLowerInvariant(),
            args[1],
            args[2],
        };
    }
    private static (bool success, int value) TryGetNumberArgument(string[] args, byte index)
    {
        return (int.TryParse(args[index], out int value), value);
    }
    private static bool IsValidIndex(string[] args, byte index)
    {
        return args.Length - 1 >= index;
    }
}