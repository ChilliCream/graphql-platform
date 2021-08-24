using System.Collections.Generic;

namespace StrawberryShake.Tools
{
    public class JsonConsoleOutputData
    {
        public List<JsonConsoleOutputActivityData> Activities { get; } =
            new List<JsonConsoleOutputActivityData>();

        public List<JsonConsoleOutputErrorData> Errors { get; } =
            new List<JsonConsoleOutputErrorData>();

        public  List<string> CreatedFiles { get; } =
            new List<string>();
    }
}
