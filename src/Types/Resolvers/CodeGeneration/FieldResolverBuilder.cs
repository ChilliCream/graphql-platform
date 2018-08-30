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
            IEnumerable<IFieldResolverDescriptor> descriptors)
        {
            IFieldResolverDescriptor[] descriptorArr =
                descriptors.ToArray();
            if (descriptorArr.Length == 0)
            {
                yield break;
            }

            string sourceText = _codeGenerator.Generate(descriptorArr);
            Assembly assembly = CSharpCompiler.Compile(sourceText);
            Type type = assembly.GetType(
                ClassSourceCodeGenerator.FullClassName);

            for (var i = 0; i < descriptorArr.Length; i++)
            {
                string resolverName = _codeGenerator.GetResolverName(i);
                FieldInfo field = type.GetField(resolverName,
                    BindingFlags.Static | BindingFlags.Public);
                yield return new FieldResolver(
                    descriptorArr[i].Field.TypeName,
                    descriptorArr[i].Field.FieldName,
                    (FieldResolverDelegate)field.GetValue(field));
            }
        }
    }
}
