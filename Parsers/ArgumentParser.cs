using System.Data;

internal static class ArgumentParser
{
    //valida os argumentos
    //transforma na formatação correta
    //retorna erros

    //scan google.com 22
    static void ValidateArgument(string[] args)
    {
        //para um argumento ser válido ele precisa ter entre 3 a 4 argumentos
        //converter o 1° para minúscula e o restante deixa igual
        int count = args.Length;

        byte quantityArg = count > byte.MaxValue
            ? throw new ArgumentException("Você excedeu o limite de argumentos permitido")
            : (byte)count
        ;
        
        //argumentos inválidos
        //tudo que é menor ou igual a dois || maior que quatro é inválido, sobrando somente 3 ou o 4 como argumentos válidos
        if (quantityArg <= 2 || quantityArg > 4) return;

        //Nesta lista o primeiro comando estará em minúsculo
        List<string> listArg = args.ToList();
        listArg[0] = listArg.First().ToLower();

       //verifico se o terceiro argumento é numérico
       if (int.TryParse(listArg[2], out int arg3)) return;

        //significa que existe um quarto argumento
        if (listArg[2] != listArg.Last())
        {
            //verifico se ele é numérico
            if (int.TryParse(listArg[3], out int arg4)) return;
        }
    }
}