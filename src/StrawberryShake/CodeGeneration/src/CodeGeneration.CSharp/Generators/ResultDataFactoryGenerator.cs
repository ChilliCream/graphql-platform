using System;
using System.Collections.Generic;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.NamingConventions;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class ResultDataFactoryGenerator : TypeMapperGenerator
    {
        const string StoreParamName = "_entityStore";

        protected override bool CanHandle(ITypeDescriptor descriptor)
        {
            return descriptor.Kind == TypeKind.ResultType && !descriptor.IsInterface();
        }

        protected override void Generate(
            CodeWriter writer,
            ITypeDescriptor typeDescriptor,
            out string fileName)
        {
            ComplexTypeDescriptor descriptor =
                typeDescriptor as ComplexTypeDescriptor ??
                throw new InvalidOperationException(
                    "A result data factory can only be generated for complex types");

            var (classBuilder, constructorBuilder) = CreateClassBuilder();

            fileName = CreateResultFactoryName(descriptor.RuntimeType.Name);
            classBuilder
                .SetName(fileName)
                .AddImplements( // TODO: This should be descriptor.RuntimeType!
                    $"{TypeNames.IOperationResultDataFactory}<{descriptor.RuntimeType.Name}>");

            constructorBuilder
                .SetTypeName(descriptor.Name)
                .SetAccessModifier(AccessModifier.Public);

            AddConstructorAssignedField(
                TypeNames.IEntityStore,
                StoreParamName,
                classBuilder,
                constructorBuilder);

            var createMethod = MethodBuilder.New()
                .SetAccessModifier(AccessModifier.Public)
                .SetName("Create")
                .SetReturnType(descriptor.RuntimeType.Name)
                .AddParameter("dataInfo", b => b.SetType(TypeNames.IOperationResultDataInfo));

            var returnStatement = MethodCallBuilder.New()
                .SetPrefix("return new ")
                .SetMethodName(descriptor.RuntimeType.Name);

            var ifHasCorrectType = IfBuilder.New()
                .SetCondition(
                    $"dataInfo is {CreateResultInfoName(descriptor.RuntimeType.Name)} info");

            foreach (PropertyDescriptor property in descriptor.Properties)
            {
                returnStatement.AddArgument(
                    BuildMapMethodCall(
                        "info",
                        property));
            }

            ifHasCorrectType.AddCode(returnStatement);
            createMethod.AddCode(ifHasCorrectType);
            createMethod.AddEmptyLine();
            createMethod.AddCode(
                $"throw new {TypeNames.ArgumentException}(\"" +
                $"{CreateResultInfoName(descriptor.RuntimeType.Name)} expected.\");");

            classBuilder.AddMethod(createMethod);

            var processed = new HashSet<string>();
            AddRequiredMapMethods(
                "info",
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

        private MethodCallBuilder GetMappingCall(
            ComplexTypeDescriptor complexTypeDescriptor,
            string idName)
        {
            return MethodCallBuilder.New()
                .SetMethodName(
                    GetFieldName(
                        CreateEntityMapperName(
                            complexTypeDescriptor.RuntimeType.Name,
                            complexTypeDescriptor.Name)) + 
                        ".Map")
                .SetDetermineStatement(false)
                .AddArgument(
                    $"{StoreParamName}.GetEntity<" +
                    $"{CreateEntityTypeName(complexTypeDescriptor.Name)}>" +
                    $"({idName}) ?? throw new {TypeNames.ArgumentNullException}()");
        }
    }
}
