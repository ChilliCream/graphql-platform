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
            new InputValueFormatterGenerator(),
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
            
            // firs we start with the type descriptor mapper which creates 
            // the type structure for the generators.
            // The following mappers can depend on this foundational data.
            TypeDescriptorMapper.Map(clientModel, context);

            // The EntityId mapper has no dependencies on other mappers.
            EntityIdFactoryDescriptorMapper.Map(clientModel, context);

            
            EntityTypeDescriptorMapper.Map(clientModel, context);
            OperationDescriptorMapper.Map(clientModel, context);
            ClientDescriptorMapper.Map(clientModel, context);
            ResultBuilderDescriptorMapper.Map(clientModel, context);
            DependencyInjectionMapper.Map(clientModel, context);
            DataTypeDescriptorMapper.Map(clientModel, context);

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
}
