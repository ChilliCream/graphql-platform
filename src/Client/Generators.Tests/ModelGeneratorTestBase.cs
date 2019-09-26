using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HotChocolate.Language;
using StrawberryShake.Generators.CSharp;
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
    }
}
