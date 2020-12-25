using System;
using System.Threading.Tasks;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class ResultTypeGenerator: CodeGenerator<TypeDescriptor>
    {
        protected override Task WriteAsync(CodeWriter writer, TypeDescriptor typeDescriptor)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (typeDescriptor is null)
            {
                throw new ArgumentNullException(nameof(typeDescriptor));
            }

            ClassBuilder classBuilder = ClassBuilder.New()
                .SetName(typeDescriptor.Name);

            ConstructorBuilder constructorBuilder = ConstructorBuilder.New()
                .SetTypeName(typeDescriptor.Name)
                .SetAccessModifier(AccessModifier.Public);


            foreach (var prop in typeDescriptor.Properties)
            {
                var propTypeBuilder = prop.TypeReference.ToBuilder();

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

            foreach (var implement in typeDescriptor.Implements)
            {
                classBuilder.AddImplements(implement);
            }

            classBuilder.AddConstructor(constructorBuilder);

            return CodeFileBuilder.New()
                .SetNamespace(typeDescriptor.Namespace)
                .AddType(classBuilder)
                .BuildAsync(writer);
        }
    }
}
