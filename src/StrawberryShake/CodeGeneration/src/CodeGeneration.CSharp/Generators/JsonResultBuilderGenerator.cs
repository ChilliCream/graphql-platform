using System;
using System.Collections.Generic;
using System.Linq;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.NamingConventions;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public partial class JsonResultBuilderGenerator : ClassBaseGenerator<ResultBuilderDescriptor>
    {
        private const string _entityStoreFieldName = "_entityStore";
        private const string _extractIdFieldName = "_extractId";
        private const string _resultDataFactoryFieldName = "_resultDataFactory";
        private const string _serializerResolverParamName = "serializerResolver";
        private const string _entityIdsParam = "entityIds";
        private const string _jsonElementParamName = TypeNames.JsonElement + "?";
        private const string _objParamName = "obj";

        protected override void Generate(
            CodeWriter writer,
            ResultBuilderDescriptor resultBuilderDescriptor,
            out string fileName)
        {
            var processed = new HashSet<string>();
            var resultTypeDescriptor =
                resultBuilderDescriptor.ResultNamedType as InterfaceTypeDescriptor
                ?? throw new InvalidOperationException(
                    "A result type can only be generated for complex types");

            var (classBuilder, constructorBuilder) = CreateClassBuilder();

            fileName = resultBuilderDescriptor.RuntimeType.Name;
            classBuilder.SetName(fileName);

            constructorBuilder.SetTypeName(fileName);

            classBuilder.AddImplements(
                $"{TypeNames.IOperationResultBuilder}<{TypeNames.JsonDocument}," +
                $" {resultTypeDescriptor.RuntimeType.Name}>");

            AddConstructorAssignedField(
                TypeNames.IEntityStore,
                _entityStoreFieldName,
                classBuilder,
                constructorBuilder);

            AddConstructorAssignedField(
                TypeReferenceBuilder.New()
                    .SetName(TypeNames.Func)
                    .AddGeneric(TypeNames.JsonElement)
                    .AddGeneric(TypeNames.EntityId),
                _extractIdFieldName,
                classBuilder,
                constructorBuilder);

            AddConstructorAssignedField(
                TypeReferenceBuilder.New()
                    .SetName(TypeNames.IOperationResultDataFactory)
                    .AddGeneric(resultTypeDescriptor.RuntimeType.Name),
                _resultDataFactoryFieldName,
                classBuilder,
                constructorBuilder);

            constructorBuilder.AddParameter(
                ParameterBuilder.New()
                    .SetName(_serializerResolverParamName)
                    .SetType(TypeNames.ISerializerResolver));

            IEnumerable<ValueParserDescriptor> valueParsers = resultBuilderDescriptor
                .ValueParsers
                .ToLookup(x => x.RuntimeType)
                .Select(x => x.First());

            foreach (ValueParserDescriptor valueParser in valueParsers)
            {
                var parserFieldName = $"{GetFieldName(valueParser.Name)}Parser";
                
                classBuilder.AddField(
                    FieldBuilder.New()
                        .SetName(parserFieldName)
                        .SetType(
                            TypeReferenceBuilder.New()
                                .SetName(TypeNames.ILeafValueParser)
                                .AddGeneric(valueParser.SerializedType.ToString())
                                .AddGeneric(valueParser.RuntimeType.ToString())));

                constructorBuilder.AddCode(
                    AssignmentBuilder.New()
                        .SetAssertNonNull()
                        .SetAssertException(
                            TypeNames.ArgumentException +
                            $"(\"No serializer for type `{valueParser.Name}` found.\")")
                        .SetLefthandSide(parserFieldName)
                        .SetRighthandSide(
                            MethodCallBuilder.New()
                                .SetPrefix(_serializerResolverParamName + ".")
                                .SetDetermineStatement(false)
                                .SetMethodName(
                                    $"GetLeafValueParser<{valueParser.SerializedType}, " +
                                    $"{valueParser.RuntimeType}>")
                                .AddArgument($"\"{valueParser.Name}\"")));
            }

            AddBuildMethod(
                resultTypeDescriptor,
                classBuilder);

            AddBuildDataMethod(
                resultTypeDescriptor,
                classBuilder);

            AddRequiredDeserializeMethods(
                resultBuilderDescriptor.ResultNamedType,
                classBuilder,
                processed);

            CodeFileBuilder.New()
                .SetNamespace(
                    resultBuilderDescriptor.ResultNamedType.RuntimeType.NamespaceWithoutGlobal)
                .AddType(classBuilder)
                .Build(writer);
        }

        /// <summary>
        /// Adds all required deserializers of the given type descriptors properties
        /// </summary>
        private void AddRequiredDeserializeMethods(
            INamedTypeDescriptor namedTypeDescriptor,
            ClassBuilder classBuilder,
            HashSet<string> processed)
        {
            if (namedTypeDescriptor is InterfaceTypeDescriptor interfaceTypeDescriptor)
            {
                foreach (var @class in interfaceTypeDescriptor.ImplementedBy)
                {
                    AddRequiredDeserializeMethods(@class, classBuilder, processed);
                }
            }
            else if (namedTypeDescriptor is ComplexTypeDescriptor complexTypeDescriptor)
            {
                foreach (var property in complexTypeDescriptor.Properties)
                {
                    AddDeserializeMethod(
                        property.Type,
                        classBuilder,
                        processed);

                    if (property.Type.NamedType() is INamedTypeDescriptor nt &&
                        !nt.IsLeafType())
                    {
                        AddRequiredDeserializeMethods(nt, classBuilder, processed);
                    }
                }
            }
        }

        private void AddDeserializeMethod(
            ITypeDescriptor typeReference,
            ClassBuilder classBuilder,
            HashSet<string> processed)
        {
            string methodName = DeserializerMethodNameFromTypeName(typeReference);

            if (processed.Add(methodName))
            {
                var returnType = typeReference.ToEntityIdBuilder();

                var methodBuilder = MethodBuilder.New()
                    .SetAccessModifier(AccessModifier.Private)
                    .SetName(methodName)
                    .SetReturnType(returnType)
                    .AddParameter(
                        ParameterBuilder.New()
                            .SetType(_jsonElementParamName)
                            .SetName(_objParamName));
                if (typeReference.IsEntityType() || typeReference.ContainsEntity())
                {
                    methodBuilder.AddParameter(
                        ParameterBuilder.New()
                            .SetType($"{TypeNames.ISet}<{TypeNames.EntityId}>")
                            .SetName(_entityIdsParam));
                }

                methodBuilder.AddCode(
                    EnsureProperNullability(isNonNullType: typeReference.IsNonNullableType()));

                classBuilder.AddMethod(methodBuilder);

                AddDeserializeMethodBody(
                    classBuilder,
                    methodBuilder,
                    typeReference,
                    processed);
            }
        }

        private void AddDeserializeMethodBody(
            ClassBuilder classBuilder,
            MethodBuilder methodBuilder,
            ITypeDescriptor typeDescriptor,
            HashSet<string> processed)
        {
            switch (typeDescriptor)
            {
                case ListTypeDescriptor listTypeDescriptor:
                    AddArrayHandler(
                        classBuilder,
                        methodBuilder,
                        listTypeDescriptor,
                        processed);
                    break;

                case INamedTypeDescriptor namedTypeDescriptor:
                    switch (typeDescriptor.Kind)
                    {
                        case TypeKind.LeafType when namedTypeDescriptor is ILeafTypeDescriptor l:
                            AddScalarTypeDeserializerMethod(methodBuilder, l);
                            break;

                        case TypeKind.ComplexDataType:
                        case TypeKind.DataType:
                            if (namedTypeDescriptor is ComplexTypeDescriptor complexTypeDescriptor)
                            {
                                AddDataTypeDeserializerMethod(
                                    classBuilder,
                                    methodBuilder,
                                    complexTypeDescriptor,
                                    processed);
                            }

                            break;

                        case TypeKind.EntityType:
                            AddUpdateEntityMethod(
                                classBuilder,
                                methodBuilder,
                                namedTypeDescriptor,
                                processed);
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    break;

                case NonNullTypeDescriptor nonNullTypeDescriptor:
                    AddDeserializeMethodBody(
                        classBuilder,
                        methodBuilder,
                        nonNullTypeDescriptor.InnerType,
                        processed);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(typeDescriptor));
            }
        }

        private void AddBuildMethod(
            InterfaceTypeDescriptor resultNamedType,
            ClassBuilder classBuilder)
        {
            var responseParameterName = "response";

            var buildMethod = MethodBuilder
                .New()
                .SetAccessModifier(AccessModifier.Public)
                .SetName("Build")
                .SetReturnType(
                    TypeReferenceBuilder.New()
                        .SetName(TypeNames.IOperationResult)
                        .AddGeneric(resultNamedType.RuntimeType.Name))
                .AddParameter(
                    ParameterBuilder.New()
                        .SetType(
                            TypeReferenceBuilder.New()
                                .SetName(TypeNames.Response)
                                .AddGeneric(TypeNames.JsonDocument)
                                .SetName(TypeNames.Response))
                        .SetName(responseParameterName));

            var concreteResultType =
                CreateResultInfoName(resultNamedType.ImplementedBy.First().RuntimeType.Name);
            buildMethod.AddCode(
                AssignmentBuilder.New()
                    .SetLefthandSide(
                        $"({resultNamedType.RuntimeType.Name} Result, {concreteResultType} " +
                        "Info)? data")
                    .SetRighthandSide("null"));

            buildMethod.AddEmptyLine();
            buildMethod.AddCode(
                IfBuilder.New()
                    .SetCondition(
                        ConditionBuilder.New()
                            .Set("response.Body is not null")
                            .And("response.Body.RootElement.TryGetProperty(\"data\"," +
                                $" out {TypeNames.JsonElement} obj)"))
                    .AddCode("data = BuildData(obj);"));

            buildMethod.AddEmptyLine();
            buildMethod.AddCode(
                MethodCallBuilder.New()
                    .SetPrefix("return new ")
                    .SetMethodName(
                        TypeNames.OperationResult.WithGeneric(resultNamedType.RuntimeType.Name))
                    .AddArgument("data?.Result")
                    .AddArgument("data?.Info")
                    .AddArgument(_resultDataFactoryFieldName)
                    .AddArgument("null"));

            classBuilder.AddMethod(buildMethod);
        }

        private CodeBlockBuilder EnsureProperNullability(
            string propertyName = _objParamName,
            bool isNonNullType = false)
        {
            var ifBuilder = IfBuilder
                .New()
                .SetCondition(
                    ConditionBuilder.New()
                        .Set($"!{propertyName}.HasValue"));
            ifBuilder.AddCode(
                isNonNullType
                    ? $"throw new {TypeNames.ArgumentNullException}();"
                    : "return null;");

            var codeBuilder = CodeBlockBuilder.New()
                .AddCode(ifBuilder)
                .AddEmptyLine();

            return codeBuilder;
        }

        private MethodCallBuilder BuildUpdateMethodCall(
            PropertyDescriptor property,
            string propertyAccess = ".Value")
        {
            var deserializeMethodCaller =
                MethodCallBuilder
                    .New()
                    .SetDetermineStatement(false)
                    .SetMethodName(DeserializerMethodNameFromTypeName(property.Type));

            deserializeMethodCaller.AddArgument(
                $"{TypeNames.GetPropertyOrNull}({_objParamName}{propertyAccess}, " +
                $"\"{GetParameterName(property.Name)}\")");

            if (property.Type.IsEntityType() || property.Type.ContainsEntity())
            {
                deserializeMethodCaller.AddArgument(_entityIdsParam);
            }

            return deserializeMethodCaller;
        }

        private MethodCallBuilder BuildUpdateMethodCall(ITypeDescriptor property, string firstArg)
        {
            var deserializeMethodCaller = MethodCallBuilder.New()
                .SetDetermineStatement(false)
                .SetMethodName(DeserializerMethodNameFromTypeName(property));

            deserializeMethodCaller.AddArgument(firstArg);

            if (property.IsEntityType() || property.ContainsEntity())
            {
                deserializeMethodCaller.AddArgument(_entityIdsParam);
            }

            return deserializeMethodCaller;
        }

        private static string DeserializerMethodNameFromTypeName(ITypeDescriptor typeDescriptor)
        {
            var ret = typeDescriptor.IsEntityType() ? "Update" : "Deserialize";
            ret += BuildDeserializeMethodName(typeDescriptor);
            return ret;
        }

        private static string BuildDeserializeMethodName(
            ITypeDescriptor typeDescriptor,
            bool parentIsList = false)
        {
            return typeDescriptor switch
            {
                ListTypeDescriptor listTypeDescriptor =>
                    BuildDeserializeMethodName(listTypeDescriptor.InnerType, true) + "Array",

                InterfaceTypeDescriptor
                {
                    ImplementedBy: { Count: > 1 },
                    ParentRuntimeType: { } parentRuntimeType
                } => parentRuntimeType.Name,

                INamedTypeDescriptor { Kind: TypeKind.EntityType } d =>
                    CreateEntityTypeName(d.RuntimeType.Name),

                INamedTypeDescriptor d =>
                    d.RuntimeType.Name,

                NonNullTypeDescriptor nonNullTypeDescriptor => parentIsList
                    ? BuildDeserializeMethodName(nonNullTypeDescriptor.InnerType) + "NonNullable"
                    : "NonNullable" + BuildDeserializeMethodName(nonNullTypeDescriptor.InnerType),

                _ => throw new ArgumentOutOfRangeException(nameof(typeDescriptor))
            };
        }
    }
}
