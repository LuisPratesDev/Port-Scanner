using Spectre.Console;

namespace Scanner.UI.Prompt.Scan;
internal class ScanPrompt
{
    //Pergunta ao usuário os host que ele quer usar para escanear
    internal static HashSet<string> AskHosts()
    {
        return Ask("Digite apenas um IP ou DNS ou cole vários separados por espaço.", "IPs/Dns");
    }

    //Pergunta ao usuário as portas que ele quer usar para escanear os hosts
    internal static HashSet<string> AskPorts()
    {
        return Ask("Digite apenas uma porta ou cole várias separadas por espaço.", "Ports");
    }

    //Método genérico que pergunta ao usuário a informação esclarecida pelos argumentos
    private static HashSet<string> Ask(string phrase, string type)
    {
        AnsiConsole.Clear();

        HashSet<string> result = new();

        string info = string.Empty;

        do
        {
            AnsiConsole.MarkupLine($"[yellow]{phrase}[/]");
            AnsiConsole.MarkupLine("[blue]! Aperte 'Enter' para continuar[/]");

            TextPrompt<string> infoAsk = new TextPrompt<string>(": ").
            DefaultValue("")
            .AllowEmpty();

            info = AnsiConsole.Prompt(infoAsk);

            string[] splitInfoSpace = info.Split(" ", StringSplitOptions.RemoveEmptyEntries);

            foreach (string address in splitInfoSpace)
            {
                result.Add(address);
            }

            if (splitInfoSpace.Length == 0)
            {
                result.Add(info);
            }

            AnsiConsole.MarkupLine($"[green]{type} adicionados: {result.Count}[/]");

        } while(info != string.Empty);

        return result;
    }
}