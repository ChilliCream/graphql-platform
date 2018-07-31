using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Resolvers.CodeGeneration;

namespace HotChocolate.Resolvers
{
    public class FieldResolverBuilder
        : IFieldResolverBuilder
    {
        private readonly FieldResolverSourceCodeGenerator _codeGenerator =
            new FieldResolverSourceCodeGenerator();

        public IEnumerable<FieldResolver> Build(
            IEnumerable<FieldResolverDescriptor> fieldResolverDescriptors)
        {
            FieldResolverDescriptor[] descriptors = fieldResolverDescriptors.ToArray();
            if(descriptors.Length == 0)
            {
                yield break;
            }

            string sourceText = _codeGenerator.Generate(descriptors);
            Assembly assembly = CSharpCompiler.Compile(sourceText);
            Type type = assembly.GetType(FieldResolverSourceCodeGenerator.FullClassName);

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
