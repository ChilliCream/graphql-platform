using System.Threading.Tasks;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Extensions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class ResultTypeGenerator: CSharpBaseGenerator<NamedTypeDescriptor>
    {
        protected override bool CanHandle(NamedTypeDescriptor descriptor)
        {
            return descriptor.Kind != TypeKind.LeafType;
        }

        protected override Task WriteAsync(CodeWriter writer, NamedTypeDescriptor namedTypeDescriptor)
        {
            AssertNonNull(writer, namedTypeDescriptor);

            ClassBuilder classBuilder = ClassBuilder.New()
                .SetName(namedTypeDescriptor.Name);

            ConstructorBuilder constructorBuilder = ConstructorBuilder.New()
                .SetTypeName(namedTypeDescriptor.Name)
                .SetAccessModifier(AccessModifier.Public);

            foreach (var prop in namedTypeDescriptor.Properties)
            {
                var propTypeBuilder = prop.Type.ToBuilder();

                // Add Property to class
                var propBuilder = PropertyBuilder
                    .New()
                    .SetName(prop.Name)
                    .SetType(propTypeBuilder)
                    .SetAccessModifier(AccessModifier.Public);
                classBuilder.AddProperty(propBuilder);

                // Add initialization of property to the constructor
                var paramName = prop.Name.WithLowerFirstChar();
                ParameterBuilder parameterBuilder = ParameterBuilder.New()
                    .SetName(paramName)
                    .SetType(propTypeBuilder);
                constructorBuilder.AddParameter(parameterBuilder);
                constructorBuilder.AddCode(prop.Name + " = " + paramName + ";");
            }

            foreach (var implement in namedTypeDescriptor.Implements)
            {
                classBuilder.AddImplements(implement);
            }

            classBuilder.AddConstructor(constructorBuilder);

            return CodeFileBuilder.New()
                .SetNamespace(namedTypeDescriptor.Namespace)
                .AddType(classBuilder)
                .BuildAsync(writer);
        }
    }
}
