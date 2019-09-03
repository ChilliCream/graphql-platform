using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using StrawberryShake.Generators.CSharp;
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

        public void Register(
            ICodeDescriptor descriptor,
            ICodeGenerator generator)
        {
            _tasks.Add(new GeneratorTask
            {
                Descriptor = descriptor,
                Generator = new NamespaceGenerator(generator)
            });
        }

        public async Task WriteAllAsync(ITypeLookup typeLookup)
        {
            if (!Directory.Exists(_directoryName))
            {
                Directory.CreateDirectory(_directoryName);
            }

            foreach (string fielName in Directory.GetFiles(_directoryName, "*.cs"))
            {
                File.Delete(fielName);
            }

            var usedNames = new HashSet<string>();
            var tasks = new List<Task>();

            foreach (GeneratorTask task in _tasks)
            {
                tasks.Add(Task.Factory.StartNew(
                    () => ExecuteGeneratorAsync(task, typeLookup, usedNames),
                    TaskCreationOptions.AttachedToParent));

            }

            await Task.WhenAll(tasks);
        }

        private async Task ExecuteGeneratorAsync(
            GeneratorTask task,
            ITypeLookup typeLookup,
            ISet<string> usedNames)
        {
            string fileName = task.Generator.CreateFileName(task.Descriptor);
            fileName = Path.Combine(_directoryName, fileName);

            lock (usedNames)
            {
                if (!usedNames.Add(fileName))
                {
                    throw new InvalidOperationException(
                        $"The file name `{fileName}` was already used.");
                }
            }

            using (FileStream stream = File.Create(fileName))
            {
                using (var sw = new StreamWriter(stream, Encoding.UTF8))
                {
                    using (var cw = new CodeWriter(sw))
                    {
                        await task.Generator.WriteAsync(
                            cw, task.Descriptor, typeLookup);
                    }
                }
            }
        }



        private class GeneratorTask
        {
            public ICodeDescriptor Descriptor { get; set; }
            public ICodeGenerator Generator { get; set; }
        }
    }
}
