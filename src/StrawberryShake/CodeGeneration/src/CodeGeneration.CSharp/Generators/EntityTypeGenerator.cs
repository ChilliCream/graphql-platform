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
                PropertyBuilder referencePropertyBuilder = PropertyBuilder
                    .New()
                    .SetName(item.Value.Name)
                    .SetType(item.Value.Type.ToEntityIdBuilder())
                    .MakeSettable()
                    .SetAccessModifier(AccessModifier.Public);
                classBuilder.AddProperty(referencePropertyBuilder);
            }

            CodeFileBuilder
                .New()
                .SetNamespace(descriptor.Namespace)
                .AddType(classBuilder)
                .Build(writer);
        }
    }
}
