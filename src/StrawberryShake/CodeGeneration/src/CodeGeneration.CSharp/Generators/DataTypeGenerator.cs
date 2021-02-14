using System;
using HotChocolate;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.NamingConventions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class DataTypeGenerator : ClassBaseGenerator<DataTypeDescriptor>
    {
        protected override void Generate(
            CodeWriter writer,
            DataTypeDescriptor descriptor,
            out string fileName)
        {
            // Setup class
            fileName = descriptor.Name;
            AbstractTypeBuilder typeBuilder;
            ConstructorBuilder? constructorBuilder = null;

            var typenamePropName = "__typename";
            if (descriptor.IsInterface)
            {
                typeBuilder = new InterfaceBuilder()
                    .SetName(fileName);

                typeBuilder.AddProperty(PropertyBuilder.New()
                    .SetName(typenamePropName)
                    .SetType(TypeNames.String));
            }
            else
            {
                var (classBuilder, constructorBuilder2) = CreateClassBuilder();
                constructorBuilder2
                    .SetTypeName(fileName)
                    .SetAccessModifier(AccessModifier.Public);

                constructorBuilder = constructorBuilder2;

                classBuilder.AddProperty(
                    PropertyBuilder
                        .New()
                        .SetAccessModifier(AccessModifier.Public)
                        .SetName(typenamePropName)
                        .SetType(TypeNames.String));

                var paramName = "typename";
                var assignment = AssignmentBuilder
                    .New()
                    .SetLefthandSide(typenamePropName)
                    .SetRighthandSide(paramName)
                    .AssertNonNull();

                constructorBuilder.AddParameter(
                        ParameterBuilder
                            .New()
                            .SetType(TypeNames.String)
                            .SetName(paramName))
                    .AddCode(assignment);

                classBuilder.SetName(fileName);
                typeBuilder = classBuilder;
            }

            // Add Properties to class
            foreach (PropertyDescriptor item in descriptor.Properties)
            {
                var itemParamName = item.Name.WithLowerFirstChar();
                var assignment = AssignmentBuilder
                    .New()
                    .SetLefthandSide(item.Name)
                    .SetRighthandSide(itemParamName);

                var paramType = item.Type.IsEntityType()
                    ? item.Type.ToEntityIdBuilder()
                    : item.Type.ToBuilder();
                constructorBuilder?.AddParameter(
                        ParameterBuilder
                            .New()
                            .SetType(paramType)
                            .SetName(itemParamName)
                            .SetDefault("null"))
                    .AddCode(assignment);

                switch (item.Type.Kind)
                {
                    case TypeKind.LeafType:
                        typeBuilder.AddProperty(item.Name)
                            .SetType(item.Type.ToBuilder())
                            .SetAccessModifier(AccessModifier.Public);
                        break;

                    case TypeKind.DataType:
                        typeBuilder.AddProperty(item.Name)
                            .SetType(item.Type.ToBuilder(item.Type.Name))
                            .SetAccessModifier(AccessModifier.Public);
                        break;

                    case TypeKind.EntityType:
                        typeBuilder.AddProperty(item.Name)
                            .SetType(item.Type.ToBuilder().SetName(TypeNames.EntityId))
                            .SetAccessModifier(AccessModifier.Public);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            foreach (NameString superType in descriptor.Implements)
            {
                typeBuilder.AddImplements(DataTypeNameFromTypeName(superType));
            }

            CodeFileBuilder
                .New()
                .SetNamespace(descriptor.Namespace)
                .AddType(typeBuilder)
                .Build(writer);
        }
    }
}
