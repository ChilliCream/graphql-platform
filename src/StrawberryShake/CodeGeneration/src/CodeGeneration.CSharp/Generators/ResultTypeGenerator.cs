using System.Linq;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.CSharp.Generators
{
    public class ResultTypeGenerator : CodeGenerator<ObjectTypeDescriptor>
    {
        private const string __typename = "__typename";

        protected override bool CanHandle(ObjectTypeDescriptor descriptor)
        {
            return true;
        }

        protected override void Generate(
            CodeWriter writer,
            ObjectTypeDescriptor descriptor,
            out string fileName,
            out string? path)
        {
            fileName = descriptor.RuntimeType.Name;
            path = null;

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
                TypeReferenceBuilder propTypeBuilder = prop.Type.ToBuilder();

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
                        .SetLefthandSide((prop.Name.Value is __typename ? "this." : "") + prop.Name)
                        .SetRighthandSide(paramName));
            }

            classBuilder.AddImplementsRange(descriptor.Implements.Select(x => x.Value));

            CodeFileBuilder
                .New()
                .SetNamespace(descriptor.RuntimeType.NamespaceWithoutGlobal)
                .AddType(classBuilder)
                .Build(writer);
        }
    }
}
