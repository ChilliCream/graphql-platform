using System;
using System.Threading.Tasks;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class EntityTypeGenerator: CodeGenerator<TypeClassDescriptor>
    {
        protected override Task WriteAsync(CodeWriter writer, TypeClassDescriptor typeClassDescriptor)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (typeClassDescriptor is null)
            {
                throw new ArgumentNullException(nameof(typeClassDescriptor));
            }

            // Setup class
            ClassBuilder classBuilder = ClassBuilder.New()
                .SetName(typeClassDescriptor.Name);

            // Add Properties to class
            foreach (var prop in typeClassDescriptor.Properties)
            {
                if (prop.Type.IsReferenceType)
                {
                    PropertyBuilder referencePropertyBuilder = PropertyBuilder
                        .New()
                        .SetName(prop.Name)
                        .SetType(prop.Type.ToBuilder().SetName(WellKnownNames.EntityId))
                        .MakeSettable()
                        .SetAccessModifier(AccessModifier.Public);
                    classBuilder.AddProperty(referencePropertyBuilder);
                }
                else
                {
                    PropertyBuilder propBuilder = PropertyBuilder
                        .New()
                        .SetName(prop.Name)
                        .SetType(prop.Type.ToBuilder())
                        .MakeSettable()
                        .SetAccessModifier(AccessModifier.Public);
                    classBuilder.AddProperty(propBuilder);
                }
            }

            return CodeFileBuilder.New()
                .SetNamespace(typeClassDescriptor.Namespace)
                .AddType(classBuilder)
                .BuildAsync(writer);
        }
    }
}
