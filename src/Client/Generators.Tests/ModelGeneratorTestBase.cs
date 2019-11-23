using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HotChocolate.Language;
using StrawberryShake.Generators.CSharp;
using StrawberryShake.Generators.Descriptors;
using StrawberryShake.Generators.Utilities;

namespace StrawberryShake.Generators
{
    public class ModelGeneratorTestBase
    {
        protected static async Task<string> WriteAllAsync(
            IReadOnlyCollection<ICodeDescriptor> descriptors,
            ITypeLookup typeLookup)
        {
            var generators = new ICodeGenerator[]
            {
                new InterfaceGenerator(),
                new ClassGenerator()
            };

            using (var stream = new MemoryStream())
            {
                using (var sw = new StreamWriter(stream, Encoding.UTF8))
                {
                    using (var cw = new CodeWriter(sw))
                    {
                        foreach (ICodeGenerator generator in generators)
                        {
                            foreach (ICodeDescriptor descriptor in descriptors)
                            {
                                if (generator.CanHandle(descriptor))
                                {
                                    await generator.WriteAsync(
                                        cw, descriptor, typeLookup);
                                    await cw.WriteLineAsync();
                                }
                            }
                        }
                    }
                }
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        protected class CollectFieldsVisitor
            : QuerySyntaxWalker<Dictionary<FieldNode, string>>
        {
            protected override void VisitField(
                FieldNode node,
                Dictionary<FieldNode, string> context)
            {
                if (!context.ContainsKey(node))
                {
                    context.Add(node, "UNKNOWN");
                }

                base.VisitField(node, context);
            }

            public static IReadOnlyDictionary<FieldNode, string> MockLookup(
                DocumentNode query,
                IReadOnlyDictionary<FieldNode, string> knownFieldTypes)
            {
                var fieldTypes = knownFieldTypes.ToDictionary(t => t.Key, t => t.Value);
                var visitor = new CollectFieldsVisitor();
                visitor.Visit(query, fieldTypes);
                return fieldTypes;
            }
        }

        protected class TestOutputHandler
            : IFileHandler
        {
            private readonly List<GeneratorTask> _tasks = new List<GeneratorTask>();

            public string Content { get; private set; }

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
                var usedNames = new HashSet<string>();

                using (var stream = new MemoryStream())
                {
                    using (var sw = new StreamWriter(stream, Encoding.UTF8))
                    {
                        using (var cw = new CodeWriter(sw))
                        {
                            foreach (GeneratorTask task in _tasks)
                            {
                                if (task.Descriptor.GetType() != typeof(QueryDescriptor))
                                {
                                    await task.Generator.WriteAsync(
                                        cw, task.Descriptor, typeLookup);
                                    await cw.WriteLineAsync();
                                    await cw.WriteLineAsync();
                                }
                            }
                        }
                    }

                    Content = Encoding.UTF8.GetString(stream.ToArray());
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
}
