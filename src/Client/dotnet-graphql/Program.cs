using System.Threading.Tasks;
using static McMaster.Extensions.CommandLineUtils.CommandLineApplication;

namespace StrawberryShake.Tools
{
    internal class Program
    {
        internal static Task<int> Main(string[] args) =>
            ExecuteAsync<CommandRouter>(args);
    }
}
