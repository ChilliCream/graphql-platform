using System;
using System.Linq;
using HotChocolate;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using static StrawberryShake.CodeGeneration.NamingConventions;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class DataTypeGenerator : ClassBaseGenerator<DataTypeDescriptor>
    {
        private const string __typename = "__typename";
        private const string _typename = "typename";

        protected override void Generate(
            CodeWriter writer,
            DataTypeDescriptor descriptor,
            out string fileName)
        {
            fileName = descriptor.RuntimeType.Name;
            AbstractTypeBuilder typeBuilder;
            ConstructorBuilder? constructorBuilder = null;

            if (descriptor.IsInterface)
            {
                typeBuilder = InterfaceBuilder
                    .New()
                    .SetName(fileName);

                typeBuilder
                    .AddProperty(__typename)
                    .SetType(TypeNames.String);
            }
            else
            {
                ClassBuilder classBuilder = ClassBuilder
                    .New()
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
                TypeReferenceBuilder propertyType = property.Type.Kind switch
                {
                    TypeKind.LeafType => property.Type.ToBuilder(),
                    TypeKind.DataType => property.Type.ToBuilder(property.Type.Name),
                    TypeKind.EntityType => property.Type.ToEntityIdBuilder(),
                    _ => throw new ArgumentOutOfRangeException()
                };

                typeBuilder
                    .AddProperty(property.Name)
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
