using System.Collections.Generic;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Descriptors;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.CSharp.Generators
{
    public class EntityTypeGenerator : CodeGenerator<EntityTypeDescriptor>
    {
        protected override void Generate(
            CodeWriter writer,
            EntityTypeDescriptor descriptor,
            out string fileName,
            out string? path)
        {
            // Setup class
            fileName = descriptor.RuntimeType.Name;
            path = State;

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

            CodeFileBuilder
                .New()
                .SetNamespace(descriptor.RuntimeType.NamespaceWithoutGlobal)
                .AddType(classBuilder)
                .Build(writer);
        }
    }
}
