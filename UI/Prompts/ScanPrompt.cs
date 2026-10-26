using Spectre.Console;
using System.Net;
using Scanner.Response;
using Scanner.Parsers;

namespace Scanner.UI.Prompt.Scan;
internal class ScanPrompt
{
    //Pergunta ao usuário os host que ele quer usar para escanear
    internal static HashSet<Task<Result<IPAddress[]>>> AskHosts()
    {
        return AskGeneric(
            () => ArgumentParser.ResolveAddresses(
                Ask()
            ), 
            "[yellow]Digite apenas um IP ou DNS ou cole vários separados por espaço.[/]",
            "DNS"
        );
    }
    //Pergunta ao usuário as portas que ele quer usar para escanear os hosts
    internal static HashSet<ushort> AskPorts()
    {
        return AskGeneric(
            () =>  ArgumentParser.ValidatePorts(Ask()),
            "[yellow]Digite apenas uma PORTA ou cole várias separados por espaço.[/]",
            "Porta"
        );
    }
    //Este método faz com que as perguntas sejam genéricas
    private static HashSet<T> AskGeneric<T>(
        Func<IEnumerable<T>> collection,
        string message, 
        string type
    )
    {
        Console.Clear();
        
        HashSet<T> result = new();

        AnsiConsole.MarkupLine(message);
        AnsiConsole.MarkupLine("[blue]! Aperte 'Enter' para continuar[/]");

        int confirmStart = 0;
        int confirmEnd = 0;
        do
        {
            confirmEnd = Console.GetCursorPosition().Top;

            ClearArea(confirmStart, confirmEnd);

            foreach(T item in collection())
            {
                result.Add(item);
            }

            confirmStart = Console.GetCursorPosition().Top;
        } 
        while(
            AnsiConsole.Confirm(
                $"Você tem {result.Count} {type} adicionadas, deseja adicionar mais: ",
                defaultValue: true
            )
        );

        AnsiConsole.Clear();

        return result;
    }
    //Método genérico que pergunta ao usuário a informação esclarecida pelos argumentos
    private static HashSet<string> Ask()
    {   
        HashSet<string> result = new();

        TextPrompt<string> infoAsk = new TextPrompt<string>("").
        DefaultValue("")
        .AllowEmpty();

        string infoResponse = AnsiConsole.Prompt(infoAsk);

        string[] splitInfoSpace = infoResponse.Split(" ", StringSplitOptions.RemoveEmptyEntries);

        if (splitInfoSpace.Length == 0)
        {
            return new HashSet<string>();
        }

        foreach (string info in splitInfoSpace)
        {
            result.Add(info);
        }

        return result;
    }
    //Limpa toda a área em que o usuário escreveu
    private static void ClearArea(int confirmStart, int confirmEnd)
    {
        if (confirmStart <= 0) return;
        
        int height = confirmEnd - confirmStart;

        for(int y = confirmStart; y <= confirmStart + height; y++)
        {
            for (int startX = 0; startX < Console.BufferWidth; startX++)
            {
                Console.SetCursorPosition(startX, y);
                Console.Write(" ");
            }
        }
        Console.SetCursorPosition(0, confirmStart);
    }
}