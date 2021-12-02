using System.Linq;

#nullable enable

namespace HotChocolate.Types;

internal class MutationConventionTypeInterceptor : TypeInterceptor
{
    private TypeInitializer _typeInitializer = default!;
    private TypeRegistry _typeRegistry = default!;
    private TypeLookup _typeLookup = default!;
    private TypeReferenceResolver _typeReferenceResolver = default!;
    private IDescriptorContext _context = default!;
    private List<MutationContextData> _mutations = default!;
    private ObjectTypeDefinition? _mutationTypeDef;

    internal override void InitializeContext(
        IDescriptorContext context,
        TypeInitializer typeInitializer,
        TypeRegistry typeRegistry,
        TypeLookup typeLookup,
        TypeReferenceResolver typeReferenceResolver)
    {
        _context = context;
        _typeInitializer = typeInitializer;
        _typeRegistry = typeRegistry;
        _typeLookup = typeLookup;
        _typeReferenceResolver = typeReferenceResolver;
    }

    public override void OnAfterCompleteTypeNames()
    {
        _mutations = _context.ContextData.GetMutationFields();
    }

    internal override void OnAfterResolveRootType(
        ITypeCompletionContext completionContext,
        DefinitionBase? definition,
        OperationType operationType,
        IDictionary<string, object?> contextData)
    {
        if (operationType == OperationType.Mutation)
        {
            _mutationTypeDef = (ObjectTypeDefinition)definition;
        }
    }

    public override void OnAfterMergeTypeExtensions()
    {
        if (_mutationTypeDef is not null)
        {
            HashSet<MutationContextData> unprocessed = new(_mutations);
            var defLookup = _mutations.ToDictionary(t => t.Definition);
            var nameLookup = _mutations.ToDictionary(t => t.Name);
            Options rootOptions = CreateOptions(_context.ContextData);

            foreach (ObjectFieldDefinition mutationField in _mutationTypeDef.Fields)
            {
                if (mutationField.IsIntrospectionField)
                {
                    continue;
                }

                Options mutationOptions = rootOptions;

                if (defLookup.TryGetValue(mutationField, out MutationContextData? contextData) ||
                    nameLookup.TryGetValue(mutationField.Name, out contextData))
                {
                    mutationOptions = CreateOptions(contextData, mutationOptions);
                    unprocessed.Remove(contextData);
                }

                if (mutationOptions.Apply)
                {
                    var inputTypeName = mutationOptions.FormatInputTypeName(mutationField.Name);
                    InputObjectType inputType = CreateInputType(inputTypeName, mutationField);
                    RegisterType(inputType);
                    mutationField.Arguments.Clear();
                    mutationField.Arguments.Add(
                        new ArgumentDefinition(
                            mutationOptions.InputArgumentName,
                            type: TypeReference.Parse($"{inputTypeName}!")));

                    ITypeReference? typeRef = mutationField.Type;
                    var payloadTypeName = mutationOptions.FormatPayloadTypeName(mutationField.Name);

                    if (!_typeLookup.TryNormalizeReference(typeRef!, out typeRef) ||
                        !_typeRegistry.TryGetType(typeRef, out RegisteredType? registration))
                    {
                        // TODO : ERROR
                        throw new SchemaException(
                            SchemaErrorBuilder.New()
                                .SetMessage("Cannot Resolve PayLoad Type")
                                .Build());
                    }

                    ObjectType payloadType;
                    if (registration.Type.Name.Equals(payloadTypeName))
                    {
                        payloadType = (ObjectType)registration.Type; // TODO : this can also be something else like a interface.
                    }
                    else
                    {
                        var payloadFieldName = contextData?.PayloadFieldName ??
                            _context.Naming.FormatFieldName(registration.Type.Name);
                        payloadType = CreatePayloadType(payloadTypeName, payloadFieldName, mutationField.Type);
                        RegisterType(payloadType);
                        mutationField.Type = TypeReference.Parse($"{payloadTypeName}!");
                    }
                }
            }
        }
    }

    private static InputObjectType CreateInputType(
        string typeName,
        ObjectFieldDefinition fieldDef)
    {
        var inputObjectDef = new InputObjectTypeDefinition(typeName);

        foreach (ArgumentDefinition argumentDef in fieldDef.Arguments)
        {
            var inputFieldDef = new InputFieldDefinition();
            argumentDef.CopyTo(inputFieldDef);
            inputObjectDef.Fields.Add(inputFieldDef);
        }

        return InputObjectType.CreateUnsafe(inputObjectDef);
    }

    private static ObjectType CreatePayloadType(
        string typeName,
        string fieldName,
        ITypeReference fieldTypeReference)
    {
        var objectDef = new ObjectTypeDefinition(typeName);

        var fieldDef = new ObjectFieldDefinition(
            fieldName,
            type: fieldTypeReference, // TODO : ensure this is nullable
            pureResolver: ctx => ctx.Parent<object?>());
        objectDef.Fields.Add(fieldDef);

        return ObjectType.CreateUnsafe(objectDef);
    }

    private static Options CreateOptions(
        IDictionary<string, object?> contextData)
    {
        if (contextData.TryGetValue(MutationContextDataKeys.Options, out var value) &&
            value is MutationConventionOptions options)
        {
            return new Options(
                options.InputTypeNamePattern,
                options.InputArgumentName,
                options.PayloadTypeNamePattern,
                options.PayloadErrorsFieldName,
                options.ApplyToAllMutations);
        }

        return new Options(null, null, null, null, null);
    }

    private static Options CreateOptions(
        MutationContextData contextData,
        Options parent = default)
    {
        return new Options(
            contextData.InputTypeName ?? parent.InputTypeNamePattern,
            contextData.InputArgumentName ?? parent.InputArgumentName,
            contextData.PayloadTypeName ?? parent.PayloadTypeNamePattern,
            parent.PayloadErrorsFieldName,
            contextData.Enabled);
    }

    private RegisteredType RegisterType(TypeSystemObjectBase type)
    {
        RegisteredType registeredType = _typeInitializer.InitializeType(type);
        _typeInitializer.CompleteTypeName(registeredType);
        return registeredType;
    }

    private readonly ref struct Options
    {
        public Options(
            string? inputTypeNamePattern,
            string? inputArgumentName,
            string? payloadTypeNamePattern,
            string? payloadErrorsFieldName,
            bool? apply)
        {
            InputTypeNamePattern = inputTypeNamePattern ??
                MutationConventionOptionDefaults.InputTypeNamePattern;
            InputArgumentName = inputArgumentName ??
                MutationConventionOptionDefaults.InputArgumentName;
            PayloadTypeNamePattern = payloadTypeNamePattern ??
                MutationConventionOptionDefaults.PayloadTypeNamePattern;
            PayloadErrorsFieldName = payloadErrorsFieldName ??
                MutationConventionOptionDefaults.PayloadErrorsFieldName;
            Apply = apply ??
                MutationConventionOptionDefaults.ApplyToAllMutations;
        }

        public string InputTypeNamePattern { get; }

        public string InputArgumentName { get; }

        public string PayloadTypeNamePattern { get; }

        public string PayloadErrorsFieldName { get; }

        public bool Apply { get; }

        public string FormatInputTypeName(string mutationName)
            => InputTypeNamePattern.Replace(
                $"{{{MutationConventionOptionDefaults.MutationName}}}",
                char.ToUpper(mutationName[0]) + mutationName.Substring(1));

        public string FormatPayloadTypeName(string mutationName)
            => PayloadTypeNamePattern.Replace(
                $"{{{MutationConventionOptionDefaults.MutationName}}}",
                char.ToUpper(mutationName[0]) + mutationName.Substring(1));
    }
}

