using System;
using System.IO;
using System.Threading.Tasks;

namespace StrawberryShake.CodeGeneration.CSharp;

class Program
{
    static async Task<int> Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("You must pass in the request file name.");
            return 1;
        }

        if (!File.Exists(args[0]))
        {
            Console.WriteLine("The specified request file name is invalid.");
            return 1;
        }

        return await CSharpGeneratorServer.RunAsync(args[0]);
    }
}
