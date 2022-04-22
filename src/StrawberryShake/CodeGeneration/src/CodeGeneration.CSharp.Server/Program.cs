using System;
using System.IO;
using System.Threading.Tasks;

namespace StrawberryShake.CodeGeneration.CSharp;

static class Program
{
    static async Task<int> Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine(ServerResources.Main_No_Arguments);
            return 1;
        }

        if (!File.Exists(args[0]))
        {
            Console.WriteLine(ServerResources.Main_Request_File_Does_Not_Exist);
            return 1;
        }

        return await CSharpGeneratorServer.RunAsync(args[0]);
    }
}
