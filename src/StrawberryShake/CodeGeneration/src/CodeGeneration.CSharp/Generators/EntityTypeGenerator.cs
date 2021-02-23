using System.Collections.Generic;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Extensions;
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
            fileName = descriptor.RuntimeType.Name;

            ClassBuilder classBuilder = 
                ClassBuilder.New()
                    .SetName(fileName);

            // Add Properties to class
            foreach (KeyValuePair<string, PropertyDescriptor> item in descriptor.Properties)
            {
                PropertyBuilder builder = classBuilder
                    .AddProperty(item.Value.Name)
                    .SetName(item.Value.Name)
                    .SetType(item.Value.Type.ToEntityIdBuilder())
                    .MakeSettable()
                    .SetAccessModifier(AccessModifier.Public);

                if (!item.Value.Type.IsNullableType())
                {
                    builder.SetValue("default!");
                }
            }

            CodeFileBuilder
                .New()
                .SetNamespace(descriptor.RuntimeType.NamespaceWithoutGlobal)
                .AddType(classBuilder)
                .Build(writer);
        }
    }
}
