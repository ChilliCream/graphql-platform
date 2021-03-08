using System.Collections.Generic;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Descriptors;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using StrawberryShake.CodeGeneration.Extensions;

namespace StrawberryShake.CodeGeneration.CSharp.Generators
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

            ClassBuilder classBuilder = ClassBuilder
                .New()
                .SetComment(descriptor.Documentation)
                .SetName(fileName);

            // Add Properties to class
            foreach (KeyValuePair<string, PropertyDescriptor> item in descriptor.Properties)
            {
                classBuilder
                    .AddProperty(item.Value.Name)
                    .SetComment(item.Value.Description)
                    .SetType(item.Value.Type.ToStateTypeReference())
                    .MakeSettable()
                    .SetPublic()
                    .SetValue(item.Value.Type.IsNullableType() ? null : "default!");
            }

            CodeFileBuilder
                .New()
                .SetNamespace(descriptor.RuntimeType.NamespaceWithoutGlobal)
                .AddType(classBuilder)
                .Build(writer);
        }
    }
}
