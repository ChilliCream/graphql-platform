using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using StrawberryShake.Generators.Utilities;

namespace StrawberryShake.Generators
{
    public class DirectoryFileHandler
        : IFileHandler
    {
        private readonly Dictionary<string, Func<CodeWriter, Task>> _tasks =
            new Dictionary<string, Func<CodeWriter, Task>>();
        private readonly string _directoryName;

        public DirectoryFileHandler(string directoryName)
        {
            _directoryName = directoryName
                ?? throw new ArgumentNullException(nameof(directoryName));
        }

        public void WriteTo(string fileName, Func<CodeWriter, Task> write)
        {
            if (fileName is null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            if (write is null)
            {
                throw new ArgumentNullException(nameof(write));
            }

            _tasks.Add(fileName, write);
        }

        public async Task WriteAllAsync()
        {
            if (!Directory.Exists(_directoryName))
            {
                Directory.CreateDirectory(_directoryName);
            }

            foreach (KeyValuePair<string, Func<CodeWriter, Task>> task in _tasks)
            {
                string fielName = Path.Combine(_directoryName, task.Key);

                using (FileStream stream = File.Create(fielName))
                {
                    using (var sw = new StreamWriter(stream, Encoding.UTF8))
                    {
                        using (var cw = new CodeWriter(sw))
                        {
                            await cw.WriteAsync("using System;");
                            await cw.WriteLineAsync();
                            await cw.WriteAsync("using System.Collections.Generic;");
                            await cw.WriteLineAsync();
                            await cw.WriteAsync("using StrawberryShake;");
                            await cw.WriteLineAsync();
                            await cw.WriteLineAsync();

                            await cw.WriteAsync("namespace Foo");
                            await cw.WriteLineAsync();
                            await cw.WriteAsync('{');
                            await cw.WriteLineAsync();

                            using (cw.IncreaseIndent())
                            {
                                await task.Value.Invoke(cw);
                                await cw.WriteLineAsync();
                            }

                            await cw.WriteAsync('}');
                            await cw.WriteLineAsync();
                        }
                    }
                }
            }
        }
    }
}
