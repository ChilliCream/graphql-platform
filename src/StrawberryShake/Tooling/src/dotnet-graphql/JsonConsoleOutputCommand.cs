using System;
using System.Text.Json;

namespace StrawberryShake.Tools
{
    public class JsonConsoleOutputCommand
        : IDisposable
    {
        private readonly JsonConsoleOutputData _data;

        public JsonConsoleOutputCommand(JsonConsoleOutputData data)
        {
            _data = data;
        }

        public void Dispose()
        {
            string json = JsonSerializer.Serialize(_data, new JsonSerializerOptions
            {
                IgnoreNullValues = true,
                IgnoreReadOnlyProperties = false
            });
            Console.WriteLine(json);
        }
    }
}
