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
        private readonly List<GeneratorTask> _tasks = new List<GeneratorTask>();


        private readonly string _directoryName;

        public DirectoryFileHandler(string directoryName)
        {
            _directoryName = directoryName
                ?? throw new ArgumentNullException(nameof(directoryName));
        }

        public void Register(ICodeDescriptor descriptor, ICodeGenerator generator)
        {
            _tasks.Add(new GeneratorTask
            {
                Descriptor = descriptor,
                Generator = generator
            });
        }

        public async Task WriteAllAsync(ITypeLookup typeLookup)
        {
            if (!Directory.Exists(_directoryName))
            {
                Directory.CreateDirectory(_directoryName);
            }

            var usedNames = new HashSet<string>();

            foreach (GeneratorTask task in _tasks)
            {
                string fileName = task.Generator.CreateFileName(task.Descriptor);
                fileName = Path.Combine(_directoryName, fileName);

                if (!usedNames.Add(fileName))
                {
                    throw new InvalidOperationException(
                        $"The file name `{fileName}` was already used.");
                }

                using (FileStream stream = File.Create(fileName))
                {
                    using (var sw = new StreamWriter(stream, Encoding.UTF8))
                    {
                        using (var cw = new CodeWriter(sw))
                        {
                            await WriteUsings(cw, task.Generator);
                            await cw.WriteLineAsync();

                            await cw.WriteAsync("namespace Foo");
                            await cw.WriteLineAsync();
                            await cw.WriteAsync('{');
                            await cw.WriteLineAsync();

                            using (cw.IncreaseIndent())
                            {
                                await task.Generator.WriteAsync(
                                    cw, task.Descriptor, typeLookup);
                            }

                            await cw.WriteAsync('}');
                            await cw.WriteLineAsync();
                        }
                    }
                }
            }
        }

        private async Task WriteUsings(CodeWriter writer, ICodeGenerator generator)
        {
            var components = generator is IUsesComponents c
                ? new HashSet<string>(c.Components)
                : new HashSet<string>();

            await WriteUsing(writer, "System");
            await WriteUsing(writer, "System.Collections.Generic");

            if (components.Contains(WellKnownComponents.Json))
            {
                await WriteUsing(writer, "System.Text.Json");
            }

            await WriteUsing(writer, "StrawberryShake");

            if (components.Contains(WellKnownComponents.Http))
            {
                await WriteUsing(writer, "StrawberryShake.Http");
            }
        }

        private async Task WriteUsing(CodeWriter writer, string ns)
        {
            await writer.WriteAsync($"using {ns};");
            await writer.WriteLineAsync();
        }

        private class GeneratorTask
        {
            public ICodeDescriptor Descriptor { get; set; }
            public ICodeGenerator Generator { get; set; }
        }

    }
}
