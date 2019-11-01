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
            (
                descriptor,
                new NamespaceGenerator(
                    generator,
                    ((IHasNamespace)descriptor).Namespace)
            ));
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
                tasks.Add(Task.Run(() =>
                    ExecuteGeneratorAsync(task, typeLookup, usedNames)));
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
            public GeneratorTask(ICodeDescriptor descriptor, ICodeGenerator generator)
            {
                Descriptor = descriptor;
                Generator = generator;
            }

            public ICodeDescriptor Descriptor { get; }
            public ICodeGenerator Generator { get; }
        }
    }
}
