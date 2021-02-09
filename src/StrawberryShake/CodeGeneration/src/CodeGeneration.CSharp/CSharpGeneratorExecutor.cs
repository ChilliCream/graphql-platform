using System;
using System.Collections.Generic;
using System.Text;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Mappers;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class CSharpGeneratorExecutor
    {
        private readonly ICodeGenerator[] _generators =
        {
            new ClientGenerator(),
            new EntityTypeGenerator(),
            new EntityIdFactoryGenerator(),
            new DependencyInjectionGenerator(),
            new EnumGenerator(),
            new EnumParserGenerator(),
            new JsonResultBuilderGenerator(),
            new OperationDocumentGenerator(),
            new OperationServiceGenerator(),
            new ResultDataFactoryGenerator(),
            new ResultFromEntityTypeMapperGenerator(),
            new ResultInfoGenerator(),
            new ResultTypeGenerator(),
            new InputTypeGenerator(),
            new ResultInterfaceGenerator(),
            new DataTypeGenerator()
        };

        public IEnumerable<CSharpDocument> Generate(
            ClientModel clientModel,
            string ns,
            string clientName)
        {
            if (clientModel is null)
            {
                throw new ArgumentNullException(nameof(clientModel));
            }

            var context = new MapperContext(ns, clientName);
            EnumDescriptorMapper.Map(clientModel, context);
            EntityIdFactoryDescriptorMapper.Map(clientModel, context);
            TypeDescriptorMapper.Map(clientModel, context);
            EntityTypeDescriptorMapper.Map(clientModel, context);
            OperationDescriptorMapper.Map(clientModel, context);
            ClientDescriptorMapper.Map(clientModel, context);
            ResultBuilderDescriptorMapper.Map(clientModel, context);
            DependencyInjectionMapper.Map(clientModel, context);

            var code = new StringBuilder();

            foreach (var descriptor in context.GetAllDescriptors())
            {
                foreach (var generator in _generators)
                {
                    if (generator.CanHandle(descriptor))
                    {
                        yield return WriteDocument(generator, descriptor, code);
                    }
                }
            }
        }

        private CSharpDocument WriteDocument(
            ICodeGenerator generator,
            ICodeDescriptor descriptor,
            StringBuilder code)
        {
            code.Clear();

            using var writer = new CodeWriter(code);

            generator.Generate(writer, descriptor, out string fileName);

            writer.Flush();
            return new(fileName, code.ToString());
        }
    }

    public class CSharpDocument
    {
        public CSharpDocument(string name, string source)
        {
            Name = name;
            SourceText = source;
        }

        public string Name { get; }

        public string SourceText { get; }
    }
}
