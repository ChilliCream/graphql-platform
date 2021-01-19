using System.Threading.Tasks;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.NamingConventions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class ResultInfoGenerator : ClassBaseGenerator<NamedTypeDescriptor>
    {
        protected override Task WriteAsync(CodeWriter writer, NamedTypeDescriptor namedTypeDescriptor)
        {
            AssertNonNull(
                writer,
                namedTypeDescriptor);

            var className = ResultInfoNameFromTypeName(namedTypeDescriptor.Name);

            ClassBuilder
                .AddImplements(ResultInfoNameFromTypeName(WellKnownNames.IOperationResultDataInfo))
                .SetName(className);

            ConstructorBuilder
                .SetTypeName(namedTypeDescriptor.Name)
                .SetAccessModifier(AccessModifier.Public);

            var constructorCaller = MethodCallBuilder.New()
                .SetPrefix("return new ")
                .SetMethodName(className);

            var withVersion = MethodBuilder.New()
                .SetAccessModifier(AccessModifier.Public)
                .SetReturnType(className)
                .SetName($"{WellKnownNames.IOperationResultDataInfo}.WithVersion")
                .AddParameter(ParameterBuilder.New()
                    .SetType("ulong")
                    .SetName("version"));

            foreach (var prop in namedTypeDescriptor.Properties)
            {
                var propTypeBuilder = prop.Type.ToEntityIdBuilder();
                
                // Add Property to class
                var propBuilder = PropertyBuilder
                    .New()
                    .SetName(prop.Name)
                    .SetType(propTypeBuilder)
                    .SetAccessModifier(AccessModifier.Public);
                
                ClassBuilder.AddProperty(propBuilder);
                constructorCaller.AddArgument(prop.Name);

                // Add initialization of property to the constructor
                var paramName = prop.Name.WithLowerFirstChar();
                ParameterBuilder parameterBuilder = ParameterBuilder.New()
                    .SetName(paramName)
                    .SetType(propTypeBuilder);
                
                ConstructorBuilder.AddParameter(parameterBuilder);
                ConstructorBuilder.AddCode(prop.Name + " = " + paramName + ";");
            }

            ClassBuilder.AddProperty(PropertyBuilder.New()
                .SetName("IOperationResultDataInfo.EntityIds")
                .SetType("IReadOnlyCollection<EntityId>")
                .AsLambda("_entityIds"));

            ClassBuilder.AddProperty(PropertyBuilder.New()
                .SetName("IOperationResultDataInfo.Version")
                .SetType("ulong")
                .AsLambda("_version"));

            AddConstructorAssignedField("IReadOnlyCollection<EntityId>", "_entityIds");
            constructorCaller.AddArgument("_entityIds");
            AddConstructorAssignedField("ulong", "_version");
            constructorCaller.AddArgument("_version");

            withVersion.AddCode(constructorCaller);
            ClassBuilder.AddMethod(withVersion);

            return CodeFileBuilder.New()
                .SetNamespace(namedTypeDescriptor.Namespace)
                .AddType(ClassBuilder)
                .BuildAsync(writer);
        }
    }
}
