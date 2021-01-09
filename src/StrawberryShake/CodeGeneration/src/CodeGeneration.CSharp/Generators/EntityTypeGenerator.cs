using System;
using System.Threading.Tasks;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class EntityTypeGenerator: CSharpBaseGenerator<TypeDescriptor>
    {
        protected override Task WriteAsync(CodeWriter writer, TypeDescriptor typeDescriptor)
        {
            AssertNonNull(writer, typeDescriptor);

            // Setup class
            ClassBuilder classBuilder = ClassBuilder.New()
                .SetName(typeDescriptor.Name);

            // Add Properties to class
            foreach (var prop in typeDescriptor.Properties)
            {
                if (prop.Type.IsEntityType)
                {
                    PropertyBuilder referencePropertyBuilder = PropertyBuilder
                        .New()
                        .SetName(prop.Name)
                        .SetType(prop.ToBuilder().SetName(WellKnownNames.EntityId))
                        .MakeSettable()
                        .SetAccessModifier(AccessModifier.Public);
                    classBuilder.AddProperty(referencePropertyBuilder);
                }
                else
                {
                    PropertyBuilder propBuilder = PropertyBuilder
                        .New()
                        .SetName(prop.Name)
                        .SetType(prop.ToBuilder())
                        .MakeSettable()
                        .SetAccessModifier(AccessModifier.Public);
                    classBuilder.AddProperty(propBuilder);
                }
            }

            return CodeFileBuilder.New()
                .SetNamespace(typeDescriptor.Namespace)
                .AddType(classBuilder)
                .BuildAsync(writer);
        }
    }
}
