using System;
using System.Collections.Generic;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.NamingConventions;

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
            NamedTypeDescriptor descriptor =
                typeDescriptor as NamedTypeDescriptor ??
                throw new InvalidOperationException();

            var (classBuilder, constructorBuilder) = CreateClassBuilder();

            fileName = ResultFactoryNameFromTypeName(descriptor.Name);
            classBuilder
                .SetName(fileName)
                .AddImplements($"{TypeNames.IOperationResultDataFactory}<{descriptor.Name}>");

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
                .SetReturnType(descriptor.Name)
                .AddParameter("dataInfo", b => b.SetType(TypeNames.IOperationResultDataInfo));

            var returnStatement = MethodCallBuilder.New()
                .SetPrefix("return new ")
                .SetMethodName(descriptor.Name);

            var ifHasCorrectType = IfBuilder.New()
                .SetCondition($"dataInfo is {ResultInfoNameFromTypeName(descriptor.Name)} info");

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
                $"{ResultInfoNameFromTypeName(descriptor.Name)} expected.\");");

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
                .SetNamespace(descriptor.Namespace)
                .AddType(classBuilder)
                .Build(writer);
        }

        private MethodCallBuilder GetMappingCall(
            NamedTypeDescriptor namedTypeDescriptor,
            string idName)
        {
            return MethodCallBuilder.New()
                .SetMethodName(
                    EntityMapperNameFromGraphQLTypeName(
                            namedTypeDescriptor.Name,
                            namedTypeDescriptor.GraphQLTypeName
                            ?? throw new ArgumentNullException("GraphQLTypeName"))
                        .ToFieldName() + ".Map")
                .SetDetermineStatement(false)
                .AddArgument(
                    $"{StoreParamName}.GetEntity<" +
                    $"{EntityTypeNameFromGraphQLTypeName(namedTypeDescriptor.GraphQLTypeName)}>" +
                    $"({idName}) ?? throw new {TypeNames.ArgumentNullException}()");
        }
    }
}
