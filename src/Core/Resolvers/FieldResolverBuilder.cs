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
        private FieldResolverSourceCodeGenerator _codeGenerator =
            new FieldResolverSourceCodeGenerator();

        public IEnumerable<FieldResolver> Build(
            IEnumerable<FieldResolverDescriptor> fieldResolverDescriptors)
        {
            FieldResolverDescriptor[] descriptors = fieldResolverDescriptors.ToArray();
            string sourceText = _codeGenerator.Generate(descriptors);

            Assembly assembly = Compiler.Compile(sourceText);
            Type type = assembly.GetType(FieldResolverSourceCodeGenerator.FullClassName);

            for (int i = 0; i < descriptors.Length; i++)
            {
                string resolverName = _codeGenerator.GetResolverName(i);
                FieldInfo field = type.GetField(resolverName,
                    BindingFlags.Static | BindingFlags.Public);
                yield return new FieldResolver(
                    descriptors[i].Field.TypeName,
                    descriptors[i].Field.FieldName,
                    (AsyncFieldResolverDelegate)field.GetValue(field));
            }
        }
    }
}