using System.Collections.Generic;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Descriptors;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.CSharp.Generators
{
    public class EntityTypeGenerator : CodeGenerator<EntityTypeDescriptor>
    {
        protected override bool CanHandle(EntityTypeDescriptor descriptor,
            CSharpSyntaxGeneratorSettings settings) => !settings.NoStore;

        protected override void Generate(EntityTypeDescriptor descriptor,
            CSharpSyntaxGeneratorSettings settings,
            CodeWriter writer,
            out string fileName,
            out string? path,
            out string ns)
        {
            // Setup class
            fileName = descriptor.RuntimeType.Name;
            path = State;
            ns = descriptor.RuntimeType.NamespaceWithoutGlobal;

            ClassBuilder classBuilder = ClassBuilder
                .New()
                .SetComment(descriptor.Documentation)
                .SetName(fileName);

            ConstructorBuilder constructorBuilder = classBuilder
                .AddConstructor()
                .SetPublic()
                .SetTypeName(fileName);

            // Add Properties to class
            foreach (KeyValuePair<string, PropertyDescriptor> item in descriptor.Properties)
            {
                classBuilder
                    .AddProperty(item.Value.Name)
                    .SetComment(item.Value.Description)
                    .SetType(item.Value.Type.ToStateTypeReference())
                    .SetPublic();

                var paramName = item.Value.Name == WellKnownNames.TypeName
                    ? WellKnownNames.TypeName
                    : GetParameterName(item.Value.Name);

                constructorBuilder
                    .AddParameter(
                        paramName,
                        x => x.SetType(item.Value.Type.ToStateTypeReference()))
                    .AddCode(AssignmentBuilder
                        .New()
                        .SetLefthandSide(
                            (item.Value.Name == WellKnownNames.TypeName
                                ? "this."
                                : string.Empty) +
                            item.Value.Name)
                        .SetRighthandSide(paramName));
            }

            classBuilder.Build(writer);
        }
    }
}
