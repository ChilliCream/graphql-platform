using System;
using System.Linq;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.NamingConventions;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class ResultInfoGenerator : ClassBaseGenerator<ITypeDescriptor>
    {
        private const string _entityIds = nameof(_entityIds);
        private const string _version = nameof(_version);

        protected override bool CanHandle(ITypeDescriptor descriptor)
        {
            return descriptor.Kind == TypeKind.ResultType && !descriptor.IsInterface();
        }

        protected override void Generate(
            CodeWriter writer,
            ITypeDescriptor typeDescriptor,
            out string fileName)
        {
            ComplexTypeDescriptor complexTypeDescriptor =
                typeDescriptor as ComplexTypeDescriptor ??
                throw new InvalidOperationException(
                    "A result entity mapper can only be generated for complex types");

            var className = CreateResultInfoName(complexTypeDescriptor.RuntimeType.Name);
            fileName = className;

            ClassBuilder classBuilder = ClassBuilder
                .New()
                .AddImplements(TypeNames.IOperationResultDataInfo)
                .SetName(fileName);

            ConstructorBuilder constructorBuilder = classBuilder
                .AddConstructor()
                .SetTypeName(complexTypeDescriptor.RuntimeType.Name);

            foreach (var prop in complexTypeDescriptor.Properties)
            {
                TypeReferenceBuilder propTypeBuilder = prop.Type.ToEntityIdBuilder();

                // Add Property to class
                classBuilder
                    .AddProperty(prop.Name)
                    .SetComment(prop.Description)
                    .SetType(propTypeBuilder)
                    .SetPublic();

                // Add initialization of property to the constructor
                var paramName = GetParameterName(prop.Name);
                constructorBuilder.AddParameter(paramName).SetType(propTypeBuilder);
                constructorBuilder.AddCode(
                    AssignmentBuilder
                        .New()
                        .SetLefthandSide(prop.Name)
                        .SetRighthandSide(paramName));
            }

            classBuilder
                .AddProperty("EntityIds")
                .SetType(TypeNames.IReadOnlyCollection.WithGeneric(TypeNames.EntityId))
                .AsLambda(_entityIds);

            classBuilder
                .AddProperty("Version")
                .SetType(TypeNames.UInt64)
                .AsLambda(_version);

            AddConstructorAssignedField(
                TypeNames.IReadOnlyCollection.WithGeneric(TypeNames.EntityId),
                _entityIds,
                classBuilder,
                constructorBuilder);

            AddConstructorAssignedField(
                TypeNames.UInt64,
                _version,
                classBuilder,
                constructorBuilder,
                true);


            // WithVersion
            const string version = nameof(version);

            classBuilder
                .AddMethod("WithVersion")
                .SetAccessModifier(AccessModifier.Public)
                .SetReturnType(TypeNames.IOperationResultDataInfo)
                .AddParameter(version, x => x.SetType(TypeNames.UInt64))
                .AddCode(MethodCallBuilder
                    .New()
                    .SetReturn()
                    .SetNew()
                    .SetMethodName(className)
                    .AddArgumentRange(
                        complexTypeDescriptor.Properties.Select(x => x.Name.Value))
                    .AddArgument(_entityIds)
                    .AddArgument(version));

            CodeFileBuilder
                .New()
                .SetNamespace(complexTypeDescriptor.RuntimeType.NamespaceWithoutGlobal)
                .AddType(classBuilder)
                .Build(writer);
        }
    }
}
