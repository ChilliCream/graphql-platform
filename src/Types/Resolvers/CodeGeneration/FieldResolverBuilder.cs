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

        public IReadOnlyCollection<FieldResolver> Build(
            IEnumerable<IFieldResolverDescriptor> descriptors)
        {
            if (descriptors == null)
            {
                throw new ArgumentNullException(nameof(descriptors));
            }

            IFieldResolverDescriptor[] descriptorArr =
                descriptors.ToArray();
            if (descriptorArr.Length == 0)
            {
                Array.Empty<FieldResolver>();
            }

            return BuildInternal(descriptorArr).ToArray();
        }

        private IEnumerable<FieldResolver> BuildInternal(
            IFieldResolverDescriptor[] descriptors)
        {
            GeneratedClass generatedClass =
                _codeGenerator.Generate(descriptors);
            Assembly assembly = CSharpCompiler.Compile(sourceText);
            Type type = assembly.GetType(
                ClassSourceCodeGenerator.FullClassName);

            for (var i = 0; i < descriptors.Length; i++)
            {
                string delegateName = _codeGenerator.GetDelegateName(i);
                FieldInfo field = type.GetField(delegateName,
                    BindingFlags.Static | BindingFlags.Public);

                yield return new FieldResolver(
                    descriptors[i].Field.TypeName,
                    descriptors[i].Field.FieldName,
                    (FieldResolverDelegate)field.GetValue(field));
            }
        }
    }
}
