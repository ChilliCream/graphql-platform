using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Mappers;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class CSharpGeneratorExecutor
    {
        private readonly ICodeGenerator[] _generators =
        {
            new EntityTypeGenerator()
        };

        public IEnumerable<CSharpDocument> Generate(ClientModel clientModel, string ns)
        {
            if (clientModel is null)
            {
                throw new ArgumentNullException(nameof(clientModel));
            }

            var context = new MapperContext(ns);
            EnumDescriptorMapper.Map(clientModel, context);
            TypeDescriptorMapper.Map(clientModel, context);
            EntityTypeDescriptorMapper.Map(clientModel, context);

            var code = new StringBuilder();

            foreach (var descriptor in context.GetAllDescriptors())
            {
                foreach (var generator in _generators)
                {
                    // TODO : we need a name
                    // TODO : asnyc context
                    yield return WriteDocument(generator, descriptor, code).Result;
                }
            }
        }

        private async Task<CSharpDocument> WriteDocument(
            ICodeGenerator generator,
            ICodeDescriptor descriptor,
            StringBuilder code)
        {
            code.Clear();

            await using var writer = new CodeWriter(code);

            await generator.WriteAsync(writer, descriptor);

            writer.Flush();
            return new(descriptor.Name, code.ToString());
        }
    }

    public class CSharpDocument
    {
        public CSharpDocument(string name, string source)
        {
            Name = name;
            Source = source;
        }

        public string Name { get; }

        public string Source { get; }
    }
}
