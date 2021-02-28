using System.Linq;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class ResultTypeGenerator : CodeGenerator<ObjectTypeDescriptor>
    {
        protected override bool CanHandle(ObjectTypeDescriptor descriptor)
        {
            return true;
        }

        protected override void Generate(
            CodeWriter writer,
            ObjectTypeDescriptor descriptor,
            out string fileName)
        {
            fileName = descriptor.RuntimeType.Name;
            ClassBuilder classBuilder = ClassBuilder
                .New()
                .SetName(fileName);

            ConstructorBuilder constructorBuilder = classBuilder
                .AddConstructor()
                .SetTypeName(fileName);

            foreach (var prop in descriptor.Properties)
            {
                TypeReferenceBuilder propTypeBuilder = prop.Type.ToBuilder();

                // Add Property to class
                classBuilder
                    .AddProperty(prop.Name)
                    .SetName(prop.Name)
                    .SetType(propTypeBuilder)
                    .SetPublic()
                    .SetValue(prop.Type.IsNullableType() ? "default!" : null);

                // Add initialization of property to the constructor
                var paramName = GetParameterName(prop.Name);
                constructorBuilder
                    .AddParameter(paramName, x => x.SetType(propTypeBuilder))
                    .AddCode(AssignmentBuilder
                        .New()
                        .SetLefthandSide(prop.Name)
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
