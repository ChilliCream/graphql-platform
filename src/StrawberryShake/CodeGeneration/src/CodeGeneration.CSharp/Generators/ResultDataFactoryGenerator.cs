using System;
using System.Collections.Generic;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.Descriptors.NamingConventions;

namespace StrawberryShake.CodeGeneration.CSharp.Generators
{
    public class ResultDataFactoryGenerator : TypeMapperGenerator
    {
        private const string _entityStore = "_entityStore";
        private const string _dataInfo = "dataInfo";
        private const string _info = "info";

        protected override bool CanHandle(ITypeDescriptor descriptor)
        {
            return descriptor.Kind == TypeKind.ResultType && !descriptor.IsInterface();
        }

        protected override void Generate(
            CodeWriter writer,
            ITypeDescriptor typeDescriptor,
            out string fileName,
            out string? path)
        {
            ComplexTypeDescriptor descriptor =
                typeDescriptor as ComplexTypeDescriptor ??
                throw new InvalidOperationException(
                    "A result data factory can only be generated for complex types");

            fileName = CreateResultFactoryName(descriptor.RuntimeType.Name);
            path = State;

            ClassBuilder classBuilder =
                ClassBuilder
                    .New()
                    .SetName(fileName)
                    .AddImplements(
                        TypeNames.IOperationResultDataFactory
                            .WithGeneric(descriptor.RuntimeType.Name));

            ConstructorBuilder constructorBuilder = classBuilder
                .AddConstructor()
                .SetTypeName(descriptor.Name);

            AddConstructorAssignedField(
                TypeNames.IEntityStore,
                _entityStore,
                classBuilder,
                constructorBuilder);

            MethodCallBuilder returnStatement = MethodCallBuilder
                .New()
                .SetReturn()
                .SetNew()
                .SetMethodName(descriptor.RuntimeType.Name);

            foreach (PropertyDescriptor property in descriptor.Properties)
            {
                returnStatement
                    .AddArgument(BuildMapMethodCall(_info, property));
            }

            IfBuilder ifHasCorrectType = IfBuilder
                .New()
                .SetCondition(
                    $"{_dataInfo} is {CreateResultInfoName(descriptor.RuntimeType.Name)} {_info}")
                .AddCode(returnStatement);

            classBuilder
                .AddMethod("Create")
                .SetAccessModifier(AccessModifier.Public)
                .SetReturnType(descriptor.RuntimeType.Name)
                .AddParameter(_dataInfo, b => b.SetType(TypeNames.IOperationResultDataInfo))
                .AddCode(ifHasCorrectType)
                .AddEmptyLine()
                .AddCode(
                    ExceptionBuilder
                        .New(TypeNames.ArgumentException)
                        .AddArgument(
                            $"\"{CreateResultInfoName(descriptor.RuntimeType.Name)} expected.\""));

            var processed = new HashSet<string>();

            AddRequiredMapMethods(
                _info,
                descriptor,
                classBuilder,
                constructorBuilder,
                processed,
                true);

            CodeFileBuilder
                .New()
                .SetNamespace(descriptor.RuntimeType.NamespaceWithoutGlobal)
                .AddType(classBuilder)
                .Build(writer);
        }
    }
}
