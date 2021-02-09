using System;
using System.Collections.Generic;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using static StrawberryShake.CodeGeneration.NamingConventions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class EntityTypeGenerator : CodeGenerator<EntityTypeDescriptor>
    {
        protected override void Generate(
            CodeWriter writer,
            EntityTypeDescriptor descriptor,
            out string fileName)
        {
            // Setup class
            fileName = EntityTypeNameFromGraphQLTypeName(descriptor.GraphQLTypeName);

            ClassBuilder classBuilder = ClassBuilder.New()
                .SetName(fileName);

            // Add Properties to class
            foreach (KeyValuePair<string, PropertyDescriptor> item in descriptor.Properties)
            {
                switch (item.Value.Type.Kind)
                {
                    case TypeKind.LeafType:
                        PropertyBuilder propBuilder =
                            PropertyBuilder
                                .New()
                                .SetName(item.Value.Name)
                                .SetType(item.Value.Type.ToBuilder())
                                .MakeSettable()
                                .SetAccessModifier(AccessModifier.Public);
                        classBuilder.AddProperty(propBuilder);
                        break;

                    case TypeKind.DataType:
                        PropertyBuilder dataBuilder =
                            PropertyBuilder
                                .New()
                                .SetName(item.Value.Name)
                                .SetType(
                                    item.Value.Type.ToBuilder(
                                        DataTypeNameFromTypeName(item.Value.Type.Name)))
                                .MakeSettable()
                                .SetAccessModifier(AccessModifier.Public);
                        classBuilder.AddProperty(dataBuilder);
                        break;

                    case TypeKind.EntityType:
                        PropertyBuilder referencePropertyBuilder = PropertyBuilder
                            .New()
                            .SetName(item.Value.Name)
                            .SetType(item.Value.Type.ToBuilder().SetName(TypeNames.EntityId))
                            .MakeSettable()
                            .SetAccessModifier(AccessModifier.Public);
                        classBuilder.AddProperty(referencePropertyBuilder);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            CodeFileBuilder
                .New()
                .SetNamespace(descriptor.Namespace)
                .AddType(classBuilder)
                .Build(writer);
        }
    }
}
