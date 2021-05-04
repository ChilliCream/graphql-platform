using System.Linq;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.CSharp.Generators
{
    public class ResultTypeGenerator : CodeGenerator<ObjectTypeDescriptor>
    {
        protected override void Generate(
            ObjectTypeDescriptor descriptor,
            CSharpSyntaxGeneratorSettings settings,
            CodeWriter writer,
            out string fileName,
            out string? path,
            out string ns)
        {
            fileName = descriptor.RuntimeType.Name;
            path = null;
            ns = descriptor.RuntimeType.NamespaceWithoutGlobal;

            ClassBuilder classBuilder = ClassBuilder
                .New()
                .SetComment(descriptor.Description)
                .SetName(fileName)
                .AddEquality(fileName, descriptor.Properties);

            ConstructorBuilder constructorBuilder = classBuilder
                .AddConstructor()
                .SetTypeName(fileName);

            foreach (var prop in descriptor.Properties)
            {
                TypeReferenceBuilder propTypeBuilder = prop.Type.ToTypeReference();

                // Add Property to class
                classBuilder
                    .AddProperty(prop.Name)
                    .SetComment(prop.Description)
                    .SetName(prop.Name)
                    .SetType(propTypeBuilder)
                    .SetPublic();

                // Add initialization of property to the constructor
                var paramName = GetParameterName(prop.Name);
                constructorBuilder
                    .AddParameter(paramName, x => x.SetType(propTypeBuilder))
                    .AddCode(AssignmentBuilder
                        .New()
                        .SetLefthandSide(GetLeftPropertyAssignment(prop.Name))
                        .SetRighthandSide(paramName));
            }

            classBuilder.AddImplementsRange(descriptor.Implements.Select(x => x.Value));
            classBuilder.Build(writer);
        }
    }
}
