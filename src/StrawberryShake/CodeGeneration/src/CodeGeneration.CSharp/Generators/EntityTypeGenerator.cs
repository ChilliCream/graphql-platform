using System;
using System.Threading.Tasks;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using static StrawberryShake.CodeGeneration.NamingConventions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class EntityTypeGenerator : CSharpBaseGenerator<EntityTypeDescriptor>
    {
        protected override Task WriteAsync(CodeWriter writer, EntityTypeDescriptor typeDescriptor)
        {
            AssertNonNull(
                writer,
                typeDescriptor
            );

            // Setup class
            ClassBuilder classBuilder = ClassBuilder.New()
                .SetName(EntityTypeNameFromGraphQLTypeName(typeDescriptor.GraphQLTypename))
                .AddProperty(PropertyBuilder.New().SetName("Id").SetType(WellKnownNames.EntityId));

            // Add Properties to class
            foreach (var (_, prop) in typeDescriptor.Properties)
            {
                switch (prop.Type.Kind)
                {
                    case TypeKind.Scalar:
                        PropertyBuilder propBuilder = PropertyBuilder
                            .New()
                            .SetName(prop.Name)
                            .SetType(prop.Type.ToBuilder())
                            .MakeSettable()
                            .SetAccessModifier(AccessModifier.Public);
                        classBuilder.AddProperty(propBuilder);
                        break;

                    case TypeKind.DataType:
                        PropertyBuilder dataBuilder = PropertyBuilder
                            .New()
                            .SetName(prop.Name)
                            .SetType(prop.Type.ToBuilder(DataTypeNameFromTypeName(prop.Type.Name)))
                            .MakeSettable()
                            .SetAccessModifier(AccessModifier.Public);
                        classBuilder.AddProperty(dataBuilder);
                        break;

                    case TypeKind.EntityType:
                        PropertyBuilder referencePropertyBuilder = PropertyBuilder
                            .New()
                            .SetName(prop.Name)
                            .SetType(prop.Type.ToBuilder().SetName(WellKnownNames.EntityId))
                            .MakeSettable()
                            .SetAccessModifier(AccessModifier.Public);
                        classBuilder.AddProperty(referencePropertyBuilder);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return CodeFileBuilder.New()
                .SetNamespace(typeDescriptor.Namespace)
                .AddType(classBuilder)
                .BuildAsync(writer);
        }
    }
}
