using HotChocolate;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Extensions;

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
            ObjectTypeDescriptor namedTypeDescriptor,
            out string fileName)
        {
            fileName = namedTypeDescriptor.RuntimeType.Name;
            ClassBuilder classBuilder = ClassBuilder.New()
                .SetName(fileName);

            ConstructorBuilder constructorBuilder = ConstructorBuilder.New()
                .SetTypeName(fileName)
                .SetAccessModifier(AccessModifier.Public);

            foreach (var prop in namedTypeDescriptor.Properties)
            {
                var propTypeBuilder = prop.Type.ToBuilder();

                // Add Property to class
                classBuilder.AddProperty(
                    prop.Name,
                    x => x
                        .SetName(prop.Name)
                        .SetType(propTypeBuilder)
                        .SetAccessModifier(AccessModifier.Public)
                        .SetValue(prop.Type.IsNullableType() ? "default!" : null));

                // Add initialization of property to the constructor
                var paramName = prop.Name.WithLowerFirstChar();
                constructorBuilder.AddParameter(paramName, x => x.SetType(propTypeBuilder));
                constructorBuilder.AddCode(prop.Name + " = " + paramName + ";");
            }

            foreach (NameString implement in namedTypeDescriptor.Implements)
            {
                classBuilder.AddImplements(implement);
            }

            classBuilder.AddConstructor(constructorBuilder);

            CodeFileBuilder
                .New()
                .SetNamespace(namedTypeDescriptor.RuntimeType.NamespaceWithoutGlobal)
                .AddType(classBuilder)
                .Build(writer);
        }
    }
}
