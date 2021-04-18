using System;
using System.Collections.Generic;
using System.Linq;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Descriptors;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.Descriptors.NamingConventions;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.CSharp.Generators
{
    public partial class JsonResultBuilderGenerator : ClassBaseGenerator<ResultBuilderDescriptor>
    {
        private const string _entityStore = "_entityStore";
        private const string entityStore = "entityStore";
        private const string _idSerializer = "_idSerializer";
        private const string _resultDataFactory = "_resultDataFactory";
        private const string idSerializer = "idSerializer";
        private const string resultDataFactory = "resultDataFactory";
        private const string _serializerResolver = "serializerResolver";
        private const string _entityIds = "entityIds";
        private const string _obj = "obj";
        private const string _response = "response";

        protected override void Generate(
            ResultBuilderDescriptor resultBuilderDescriptor,
            CSharpSyntaxGeneratorSettings settings,
            CodeWriter writer,
            out string fileName,
            out string? path,
            out string ns)
        {
            InterfaceTypeDescriptor resultTypeDescriptor =
                resultBuilderDescriptor.ResultNamedType as InterfaceTypeDescriptor
                ?? throw new InvalidOperationException(
                    "A result type can only be generated for complex types");

            fileName = resultBuilderDescriptor.RuntimeType.Name;
            path = State;
            ns = resultBuilderDescriptor.RuntimeType.NamespaceWithoutGlobal;

            ClassBuilder classBuilder = ClassBuilder
                .New()
                .SetName(fileName);

            ConstructorBuilder constructorBuilder = classBuilder
                .AddConstructor()
                .SetTypeName(fileName);

            classBuilder
                .AddImplements(
                    TypeNames.IOperationResultBuilder.WithGeneric(
                        TypeNames.JsonDocument,
                        resultTypeDescriptor.RuntimeType.ToString()));

            if (settings.IsStoreEnabled())
            {
                AddConstructorAssignedField(
                    TypeNames.IEntityStore,
                    _entityStore,
                    entityStore,
                    classBuilder,
                    constructorBuilder);

                AddConstructorAssignedField(
                    TypeNames.IEntityIdSerializer,
                    _idSerializer,
                    idSerializer,
                    classBuilder,
                    constructorBuilder);
            }

            AddConstructorAssignedField(
                TypeNames.IOperationResultDataFactory
                    .WithGeneric(resultTypeDescriptor.RuntimeType.ToString()),
                _resultDataFactory,
                resultDataFactory,
                classBuilder,
                constructorBuilder);

            constructorBuilder
                .AddParameter(_serializerResolver)
                .SetType(TypeNames.ISerializerResolver);

            IEnumerable<ValueParserDescriptor> valueParsers = resultBuilderDescriptor
                .ValueParsers
                .GroupBy(t => t.Name)
                .Select(t => t.First());

            foreach (ValueParserDescriptor valueParser in valueParsers)
            {
                var parserFieldName = $"{GetFieldName(valueParser.Name)}Parser";

                classBuilder
                    .AddField(parserFieldName)
                    .SetReadOnly()
                    .SetType(
                        TypeNames.ILeafValueParser
                            .WithGeneric(valueParser.SerializedType, valueParser.RuntimeType));

                MethodCallBuilder getLeaveValueParser = MethodCallBuilder
                    .Inline()
                    .SetMethodName(_serializerResolver, "GetLeafValueParser")
                    .AddGeneric(valueParser.SerializedType.ToString())
                    .AddGeneric(valueParser.RuntimeType.ToString())
                    .AddArgument(valueParser.Name.AsStringToken());

                constructorBuilder.AddCode(
                    AssignmentBuilder
                        .New()
                        .SetAssertNonNull()
                        .SetAssertException(
                            ExceptionBuilder
                                .Inline(TypeNames.ArgumentException)
                                .AddArgument(
                                    $"\"No serializer for type `{valueParser.Name}` found.\""))
                        .SetLefthandSide(parserFieldName)
                        .SetRighthandSide(getLeaveValueParser));
            }

            AddBuildMethod(resultTypeDescriptor, classBuilder);

            AddBuildDataMethod(settings, resultTypeDescriptor, classBuilder);

            var processed = new HashSet<string>();
            AddRequiredDeserializeMethods(resultTypeDescriptor, classBuilder, processed);

            classBuilder.Build(writer);
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
                        !nt.IsLeaf())
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
                MethodBuilder methodBuilder = classBuilder
                    .AddMethod()
                    .SetPrivate()
                    .SetReturnType(typeReference.ToStateTypeReference())
                    .SetName(methodName);


                if (typeReference.IsOrContainsEntity())
                {
                    methodBuilder
                        .AddParameter(_session, x => x.SetType(TypeNames.IEntityStoreUpdateSession))
                        .AddParameter(_obj, x => x.SetType(TypeNames.JsonElement.MakeNullable()))
                        .AddParameter(
                            _entityIds,
                            x => x.SetType(TypeNames.ISet.WithGeneric(TypeNames.EntityId)));
                }
                else
                {
                    methodBuilder
                        .AddParameter(_obj)
                        .SetType(TypeNames.JsonElement.MakeNullable());
                }

                IfBuilder jsonElementNullCheck = IfBuilder
                    .New()
                    .SetCondition($"!{_obj}.HasValue")
                    .AddCode(
                        typeReference.IsNonNullable()
                            ? ExceptionBuilder.New(TypeNames.ArgumentNullException)
                            : CodeLineBuilder.From("return null;"));

                methodBuilder
                    .AddCode(jsonElementNullCheck)
                    .AddEmptyLine();

                AddDeserializeMethodBody(classBuilder, methodBuilder, typeReference, processed);
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
                    AddArrayHandler(classBuilder, methodBuilder, listTypeDescriptor, processed);
                    break;

                case ILeafTypeDescriptor { Kind: TypeKind.Leaf } d:
                    AddScalarTypeDeserializerMethod(methodBuilder, d);
                    break;

                case ComplexTypeDescriptor { Kind: TypeKind.EntityOrData } d:
                    AddEntityOrDataTypeDeserializerMethod(
                        classBuilder,
                        methodBuilder,
                        d,
                        processed);
                    break;

                case ComplexTypeDescriptor { Kind: TypeKind.AbstractData } d:
                    AddDataTypeDeserializerMethod(classBuilder, methodBuilder, d, processed);
                    break;

                case ComplexTypeDescriptor { Kind: TypeKind.Data } d:
                    AddDataTypeDeserializerMethod(classBuilder, methodBuilder, d, processed);
                    break;

                case INamedTypeDescriptor { Kind: TypeKind.Entity } d:
                    AddUpdateEntityMethod(classBuilder, methodBuilder, d, processed);
                    break;

                case NonNullTypeDescriptor d:
                    AddDeserializeMethodBody(classBuilder, methodBuilder, d.InnerType, processed);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(typeDescriptor));
            }
        }

        private void AddBuildMethod(
            InterfaceTypeDescriptor resultNamedType,
            ClassBuilder classBuilder)
        {
            var buildMethod = classBuilder
                .AddMethod()
                .SetAccessModifier(AccessModifier.Public)
                .SetName("Build")
                .SetReturnType(
                    TypeReferenceBuilder
                        .New()
                        .SetName(TypeNames.IOperationResult)
                        .AddGeneric(resultNamedType.RuntimeType.Name));

            buildMethod
                .AddParameter(_response)
                .SetType(TypeNames.Response.WithGeneric(TypeNames.JsonDocument));

            var concreteResultType =
                CreateResultInfoName(resultNamedType.ImplementedBy.First().RuntimeType.Name);

            // (IGetFooResult Result, GetFooResultInfo Info)? data = null;
            buildMethod.AddCode(
                AssignmentBuilder
                    .New()
                    .SetLefthandSide(
                        $"({resultNamedType.RuntimeType.Name} Result, {concreteResultType} " +
                        "Info)? data")
                    .SetRighthandSide("null"));

            // IReadOnlyList<IClientError>? errors = null;
            buildMethod.AddCode(
                AssignmentBuilder
                    .New()
                    .SetLefthandSide(
                        TypeNames.IReadOnlyList
                            .WithGeneric(TypeNames.IClientError)
                            .MakeNullable() + " errors")
                    .SetRighthandSide("null"));

            buildMethod.AddEmptyLine();



            buildMethod.AddEmptyLine();
            buildMethod.AddCode(
                IfBuilder.New()
                    .SetCondition("response.Exception is null")
                    .AddCode(CreateBuildDataSerialization())
                    .AddElse(CreateDataError("response.Exception"))
                );

            buildMethod.AddEmptyLine();
            buildMethod.AddCode(
                MethodCallBuilder
                    .New()
                    .SetReturn()
                    .SetNew()
                    .SetMethodName(TypeNames.OperationResult)
                    .AddGeneric(resultNamedType.RuntimeType.Name)
                    .AddArgument("data?.Result")
                    .AddArgument("data?.Info")
                    .AddArgument(_resultDataFactory)
                    .AddArgument("errors"));
        }

        private TryCatchBuilder CreateBuildDataSerialization()
        {
            return TryCatchBuilder
                    .New()
                    .AddTryCode(
                        IfBuilder
                            .New()
                            .SetCondition(
                                ConditionBuilder
                                    .New()
                                    .Set("response.Body != null"))
                            .AddCode(
                                IfBuilder
                                    .New()
                                    .SetCondition(
                                        ConditionBuilder
                                            .New()
                                            .Set("response.Body.RootElement.TryGetProperty(" +
                                                $"\"data\", out {TypeNames.JsonElement} " +
                                                "dataElement) && dataElement.ValueKind == " +
                                                $"{TypeNames.JsonValueKind}.Object"))
                                    .AddCode("data = BuildData(dataElement);"))
                            .AddCode(
                                IfBuilder
                                    .New()
                                    .SetCondition(
                                        ConditionBuilder
                                            .New()
                                            .Set(
                                                "response.Body.RootElement.TryGetProperty(" +
                                                $"\"errors\", out {TypeNames.JsonElement} " +
                                                "errorsElement)"))
                                    .AddCode($"errors = {TypeNames.ParseError}(errorsElement);")))
                    .AddCatchBlock(
                        CatchBlockBuilder
                            .New()
                            .SetExceptionVariable("ex")
                            .AddCode(CreateDataError()));
        }

        private static AssignmentBuilder CreateDataError(
            string exception = "ex")
        {
            string dict = TypeNames.Dictionary.WithGeneric(
                TypeNames.String, 
                TypeNames.Object.MakeNullable());
            
            string body = "response.Body?.ToString()";

            MethodCallBuilder createClientError =
                MethodCallBuilder
                    .Inline()
                    .SetNew()
                    .SetMethodName(TypeNames.ClientError)
                    .AddArgument($"{exception}.Message")
                    .AddArgument($"exception: {exception}")
                    .AddArgument($"extensions: new  {dict} {{ {{ \"body\", {body} }} }}");

            return AssignmentBuilder.New()
                .SetLefthandSide("errors")
                .SetRighthandSide(
                    ArrayBuilder.New()
                        .SetDetermineStatement(false)
                        .SetType(TypeNames.IClientError)
                        .AddAssignment(createClientError));
        }

        private MethodCallBuilder BuildUpdateMethodCall(PropertyDescriptor property)
        {
            MethodCallBuilder propertyAccessor = MethodCallBuilder
                .Inline()
                .SetMethodName(TypeNames.GetPropertyOrNull)
                .AddArgument(_obj)
                .AddArgument(property.FieldName.AsStringToken());

            return BuildUpdateMethodCall(property.Type, propertyAccessor).SetWrapArguments();
        }

        private MethodCallBuilder BuildUpdateMethodCall(
            ITypeDescriptor property,
            ICode argument)
        {
            MethodCallBuilder deserializeMethodCaller = MethodCallBuilder
                .Inline()
                .SetMethodName(DeserializerMethodNameFromTypeName(property));

            if (property.IsOrContainsEntity())
            {
                deserializeMethodCaller
                    .AddArgument(_session)
                    .AddArgument(argument)
                    .AddArgument(_entityIds);
            }
            else
            {
                deserializeMethodCaller.AddArgument(argument);
            }

            return deserializeMethodCaller;
        }

        private static string DeserializerMethodNameFromTypeName(ITypeDescriptor typeDescriptor)
        {
            var ret = typeDescriptor.IsEntity() ? "Update" : "Deserialize";
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

                INamedTypeDescriptor { Kind: TypeKind.Entity } d =>
                    CreateEntityType(
                            d.RuntimeType.Name,
                            d.RuntimeType.NamespaceWithoutGlobal)
                        .Name,

                // TODO: we should look a better way to solve the array naming issue.
                INamedTypeDescriptor d =>
                    d.RuntimeType.ToString() == TypeNames.ByteArray
                        ? "ByteArray"
                        : d.RuntimeType.Name,

                NonNullTypeDescriptor nonNullTypeDescriptor => parentIsList
                    ? BuildDeserializeMethodName(nonNullTypeDescriptor.InnerType) + "NonNullable"
                    : "NonNullable" + BuildDeserializeMethodName(nonNullTypeDescriptor.InnerType),

                _ => throw new ArgumentOutOfRangeException(nameof(typeDescriptor))
            };
        }
    }
}
