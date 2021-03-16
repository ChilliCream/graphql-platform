using System;
using System.Linq;
using HotChocolate.Utilities;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Descriptors;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using static StrawberryShake.CodeGeneration.Descriptors.NamingConventions;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.CSharp.Generators
{
    public class DataTypeGenerator : ClassBaseGenerator<DataTypeDescriptor>
    {
        private const string __typename = "__typename";
        private const string _typename = "typename";

        protected override void Generate(
            CodeWriter writer,
            DataTypeDescriptor descriptor,
            out string fileName,
            out string? path)
        {
            fileName = descriptor.RuntimeType.Name;
            path = State;

            AbstractTypeBuilder typeBuilder;
            ConstructorBuilder? constructorBuilder = null;

            if (descriptor.IsInterface)
            {
                typeBuilder = InterfaceBuilder
                    .New()
                    .SetComment(descriptor.Documentation)
                    .SetName(fileName);

                typeBuilder
                    .AddProperty(__typename)
                    .SetType(TypeNames.String);
            }
            else
            {
                ClassBuilder classBuilder = ClassBuilder
                    .New()
                    .SetComment(descriptor.Documentation)
                    .SetName(fileName);

                typeBuilder = classBuilder;

                classBuilder
                    .AddProperty(__typename)
                    .SetPublic()
                    .SetType(TypeNames.String);

                constructorBuilder = classBuilder
                    .AddConstructor()
                    .SetTypeName(fileName);

                constructorBuilder
                    .AddParameter(_typename)
                    .SetType(TypeNames.String)
                    .SetName(_typename);

                constructorBuilder
                    .AddCode(
                        AssignmentBuilder
                            .New()
                            .SetLefthandSide(__typename)
                            .SetRighthandSide(_typename)
                            .AssertNonNull());
            }

            // Add Properties to class
            foreach (PropertyDescriptor property in descriptor.Properties)
            {
                if (property.Name.Value.EqualsOrdinal(__typename))
                {
                    continue;
                }

                TypeReferenceBuilder propertyType = property.Type.ToStateTypeReference();

                typeBuilder
                    .AddProperty(property.Name)
                    .SetComment(property.Description)
                    .SetType(propertyType)
                    .SetPublic();

                var parameterName = GetParameterName(property.Name);

                constructorBuilder?
                    .AddParameter(parameterName)
                    .SetType(propertyType)
                    .SetDefault("null");

                constructorBuilder?
                    .AddCode(AssignmentBuilder
                        .New()
                        .SetLefthandSide(property.Name)
                        .SetRighthandSide(parameterName));
            }

            // implement interfaces
            typeBuilder.AddImplementsRange(descriptor.Implements.Select(CreateDataTypeName));

            CodeFileBuilder
                .New()
                .SetNamespace(descriptor.RuntimeType.NamespaceWithoutGlobal)
                .AddType(typeBuilder)
                .Build(writer);
        }
    }
}
