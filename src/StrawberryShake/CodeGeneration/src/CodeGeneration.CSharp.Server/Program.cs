using System.Threading.Tasks;

namespace StrawberryShake.CodeGeneration.CSharp;

class Program
{
    static Task Main(string[] args)
        => Server.RunAsync();
}