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
        protected override void Generate(
            CodeWriter writer,
            DataTypeDescriptor descriptor,
            CodeGeneratorSettings settings,
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
                    .AddProperty(WellKnownNames.TypeName)
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
                    .AddProperty(WellKnownNames.TypeName)
                    .SetPublic()
                    .SetType(TypeNames.String);

                constructorBuilder = classBuilder
                    .AddConstructor()
                    .SetTypeName(fileName);

                constructorBuilder
                    .AddParameter(WellKnownNames.TypeName)
                    .SetType(TypeNames.String)
                    .SetName(WellKnownNames.TypeName);

                constructorBuilder
                    .AddCode(
                        AssignmentBuilder
                            .New()
                            .SetLefthandSide("this." + WellKnownNames.TypeName)
                            .SetRighthandSide(WellKnownNames.TypeName)
                            .AssertNonNull());
            }

            // Add Properties to class
            foreach (PropertyDescriptor property in descriptor.Properties)
            {
                if (property.Name.Value.EqualsOrdinal(WellKnownNames.TypeName))
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
