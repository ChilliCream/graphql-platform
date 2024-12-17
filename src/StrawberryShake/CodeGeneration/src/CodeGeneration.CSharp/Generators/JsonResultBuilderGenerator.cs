using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Descriptors;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.Descriptors.NamingConventions;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.CSharp.Generators;

public partial class JsonResultBuilderGenerator : ClassBaseGenerator<ResultBuilderDescriptor>
{
    private const string _entityStore = "entityStore";
    private const string _idSerializer = "idSerializer";
    private const string _resultDataFactory = "ResultDataFactory";
    private const string _serializerResolver = "serializerResolver";
    private const string _entityIds = "entityIds";
    private const string _obj = "obj";

    protected override void Generate(
        ResultBuilderDescriptor resultBuilderDescriptor,
        CSharpSyntaxGeneratorSettings settings,
        CodeWriter writer,
        out string fileName,
        out string? path,
        out string ns)
    {
        var resultTypeDescriptor =
            resultBuilderDescriptor.ResultNamedType as InterfaceTypeDescriptor ??
            throw new InvalidOperationException(
                "A result type can only be generated for complex types");

        fileName = resultBuilderDescriptor.RuntimeType.Name;
        path = State;
        ns = resultBuilderDescriptor.RuntimeType.NamespaceWithoutGlobal;

        var classBuilder = ClassBuilder
            .New()
            .SetAccessModifier(settings.AccessModifier)
            .SetName(fileName);

        var constructorBuilder = classBuilder
            .AddConstructor()
            .SetTypeName(fileName);

        classBuilder
            .AddImplements(
                TypeNames.OperationResultBuilder.WithGeneric(
                    resultTypeDescriptor.RuntimeType.ToString()));

        if (settings.IsStoreEnabled())
        {
            AddConstructorAssignedField(
                TypeNames.IEntityStore,
                GetFieldName(_entityStore),
                GetParameterName(_entityStore),
                classBuilder,
                constructorBuilder);

            AddConstructorAssignedField(
                TypeNames.IEntityIdSerializer,
                GetFieldName(_idSerializer),
                GetParameterName(_idSerializer),
                classBuilder,
                constructorBuilder);
        }

        classBuilder.AddProperty(
            GetPropertyName(_resultDataFactory),
            b => b.SetType(TypeNames.IOperationResultDataFactory
                    .WithGeneric(resultTypeDescriptor.RuntimeType.ToString()))
                .SetAccessModifier(AccessModifier.Protected)
                .SetOverride());

        var assignment = AssignmentBuilder
            .New()
            .SetLeftHandSide(GetPropertyName(_resultDataFactory))
            .SetRightHandSide(GetParameterName(_resultDataFactory))
            .SetAssertNonNull();

        constructorBuilder
            .AddCode(assignment)
            .AddParameter(
                GetParameterName(_resultDataFactory),
                b => b.SetType(TypeNames.IOperationResultDataFactory
                    .WithGeneric(resultTypeDescriptor.RuntimeType.ToString())));

        constructorBuilder
            .AddParameter(_serializerResolver)
            .SetType(TypeNames.ISerializerResolver);

        var valueParsers =
            resultBuilderDescriptor.ValueParsers
                .GroupBy(t => t.Name)
                .Select(t => t.First());

        foreach (var valueParser in valueParsers)
        {
            var parserFieldName = $"{GetFieldName(valueParser.Name)}Parser";

            classBuilder
                .AddField(parserFieldName)
                .SetReadOnly()
                .SetType(TypeNames.ILeafValueParser
                    .WithGeneric(valueParser.SerializedType, valueParser.RuntimeType));

            var getLeaveValueParser = MethodCallBuilder
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
                    .SetLeftHandSide(parserFieldName)
                    .SetRightHandSide(getLeaveValueParser));
        }

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
        if (namedTypeDescriptor is InterfaceTypeDescriptor interfaceType)
        {
            foreach (var @class in interfaceType.ImplementedBy)
            {
                AddRequiredDeserializeMethods(@class, classBuilder, processed);
            }
        }
        else if (namedTypeDescriptor is ObjectTypeDescriptor objectType)
        {
            var propertyProcessed = new HashSet<string>(
                objectType.Properties.Select(t => t.Name),
                StringComparer.Ordinal);
            var properties = new List<PropertyDescriptor>(
                objectType.Properties);

            // include properties from fragments
            foreach (var fragment in objectType.Deferred)
            {
                foreach (var property in fragment.Class.Properties)
                {
                    if (propertyProcessed.Add(property.Name))
                    {
                        properties.Add(EnsureDeferredFieldIsNullable(property));
                    }
                }
            }

            foreach (var property in properties)
            {
                AddDeserializeMethod(property.Type, classBuilder, processed);

                if (property.Type.NamedType() is INamedTypeDescriptor nt && !nt.IsLeaf())
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
        var methodName = DeserializerMethodNameFromTypeName(typeReference);

        if (processed.Add(methodName))
        {
            var methodBuilder = classBuilder
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

            var jsonElementNullCheck = IfBuilder
                .New()
                .SetCondition($"!{_obj}.HasValue")
                .AddCode(
                    typeReference.IsNonNull()
                        ? CodeLineBuilder.From("return default;")
                        : CodeLineBuilder.From("return null;"));

            methodBuilder
                .AddCode(jsonElementNullCheck)
                .AddEmptyLine();

            // When deserializing arrays of nullable values (e.g. [User] => [ { ... }, null, { ... }]) the second
            // element will be not null, but instead a JSON element of kind JsonValueKind.Null.
            var jsonElementNullValueKindCheck = IfBuilder
                .New()
                .SetCondition($"{_obj}.Value.ValueKind == global::System.Text.Json.JsonValueKind.Null")
                .AddCode(
            typeReference.IsNonNull()
                ? ExceptionBuilder.New(TypeNames.ArgumentNullException)
                : CodeLineBuilder.From("return null;"));

            methodBuilder
                .AddCode(jsonElementNullValueKindCheck)
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

            case ILeafTypeDescriptor { Kind: TypeKind.Leaf, } d:
                AddScalarTypeDeserializerMethod(methodBuilder, d);
                break;

            case ComplexTypeDescriptor { Kind: TypeKind.EntityOrData, } d:
                AddEntityOrDataTypeDeserializerMethod(
                    classBuilder,
                    methodBuilder,
                    d,
                    processed);
                break;

            case ComplexTypeDescriptor { Kind: TypeKind.AbstractData, } d:
                AddDataTypeDeserializerMethod(classBuilder, methodBuilder, d, processed);
                break;

            case ComplexTypeDescriptor { Kind: TypeKind.Data, } d:
                AddDataTypeDeserializerMethod(classBuilder, methodBuilder, d, processed);
                break;

            case INamedTypeDescriptor { Kind: TypeKind.Entity, } d:
                AddUpdateEntityMethod(classBuilder, methodBuilder, d, processed);
                break;

            case NonNullTypeDescriptor d:
                AddDeserializeMethodBody(classBuilder, methodBuilder, d.InnerType, processed);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(typeDescriptor));
        }
    }

    private static MethodCallBuilder BuildUpdateMethodCall(PropertyDescriptor property)
    {
        var propertyAccessor = MethodCallBuilder
            .Inline()
            .SetMethodName(TypeNames.GetPropertyOrNull)
            .AddArgument(_obj)
            .AddArgument(property.FieldName.AsStringToken());

        var setNullForgiving = property.Kind is PropertyKind.DeferredField;

        return BuildUpdateMethodCall(
            property.Type,
            propertyAccessor,
            setNullForgiving)
            .SetWrapArguments();
    }

    private static MethodCallBuilder BuildFragmentMethodCall(DeferredFragmentDescriptor fragment)
    {
        return MethodCallBuilder
            .Inline()
            .SetMethodName(TypeNames.ContainsFragment)
            .AddArgument(_obj)
            .AddArgument(fragment.FragmentIndicatorField.AsStringToken());
    }

    private static MethodCallBuilder BuildUpdateMethodCall(
        ITypeDescriptor property,
        ICode argument,
        bool setNullForgiving)
    {
        var deserializeMethodCaller = MethodCallBuilder
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

        if (setNullForgiving)
        {
            deserializeMethodCaller.SetNullForgiving();
        }

        return deserializeMethodCaller;
    }

    private static string DeserializerMethodNameFromTypeName(ITypeDescriptor typeDescriptor)
    {
        var ret = typeDescriptor.IsEntity() ? "Update_" : "Deserialize_";
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
                ImplementedBy.Count: > 1,
                ParentRuntimeType: { } parentRuntimeType,
            } => parentRuntimeType.Name,

            INamedTypeDescriptor { Kind: TypeKind.Entity, } d =>
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

            _ => throw new ArgumentOutOfRangeException(nameof(typeDescriptor)),
        };
    }
}
