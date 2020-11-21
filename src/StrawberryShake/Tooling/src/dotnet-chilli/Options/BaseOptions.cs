using CommandLine;

namespace StrawberryShake.Tools.Options
{
    public abstract class BaseOptions
    {
        [Option('j', "json", HelpText = "Console output as JSON.")]
        public bool Json { get; }

        protected BaseOptions(bool json)
        {
            Json = json;
        }
    }
}
