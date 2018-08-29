using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Resolvers.CodeGeneration;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal class FieldResolverBuilder
        : IFieldResolverBuilder
    {
        private readonly ClassSourceCodeGenerator _codeGenerator =
            new ClassSourceCodeGenerator();

        public IEnumerable<FieldResolver> Build(
            IEnumerable<IFieldResolverDescriptor> fieldResolverDescriptors)
        {
            IFieldResolverDescriptor[] descriptors =
                fieldResolverDescriptors.ToArray();
            if (descriptors.Length == 0)
            {
                yield break;
            }

            string sourceText = _codeGenerator.Generate(descriptors);
            Assembly assembly = CSharpCompiler.Compile(sourceText);
            Type type = assembly.GetType(
                ClassSourceCodeGenerator.FullClassName);

            for (var i = 0; i < descriptors.Length; i++)
            {
                string resolverName = _codeGenerator.GetResolverName(i);
                FieldInfo field = type.GetField(resolverName,
                    BindingFlags.Static | BindingFlags.Public);
                yield return new FieldResolver(
                    descriptors[i].Field.TypeName,
                    descriptors[i].Field.FieldName,
                    (FieldResolverDelegate)field.GetValue(field));
            }
        }
    }
}
