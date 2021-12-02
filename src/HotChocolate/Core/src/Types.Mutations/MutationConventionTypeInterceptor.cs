using System.Linq;

#nullable enable

namespace HotChocolate.Types;

internal class MutationConventionTypeInterceptor : TypeInterceptor
{
    private TypeRegistry _typeRegistry = default!;
    private IDescriptorContext _context = default!;
    private List<MutationContextData> _mutations = default!;
    private ObjectTypeDefinition? _mutationTypeDef;

    internal override void InitializeContext(
        IDescriptorContext context,
        TypeReferenceResolver typeReferenceResolver)
    {
        _context = context;
    }

    public override void OnAfterCompleteTypeNames()
    {
        _mutations = _context.ContextData.GetMutationFields();
    }

    public override void OnAfterCompleteName(
        ITypeCompletionContext completionContext,
        DefinitionBase? definition,
        IDictionary<string, object?> contextData)
    {
        if (completionContext.IsMutationType ?? false)
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

                    var payloadTypeName = mutationOptions.FormatInputTypeName(mutationField.Name);
                    


                }


            }
        }
    }

    private static InputObjectType CreateInputType(
        string typeName,
        ObjectFieldDefinition field)
    {
        var inputObject = new InputObjectTypeDefinition(typeName);

        foreach (ArgumentDefinition argument in field.Arguments)
        {
            var inputField = new InputFieldDefinition();
            argument.CopyTo(inputField);
            inputObject.Fields.Add(inputField);
        }

        return InputObjectType.CreateUnsafe(inputObject);
    }

    private static Options CreateOptions(
        IDictionary<string, object?> contextData,
        Options parent = default)
    {
        if (contextData.TryGetValue(MutationContextDataKeys.Options, out var value) &&
            value is MutationConventionOptions options)
        {
            return new Options(
                options.InputTypeNamePattern ?? parent.InputTypeNamePattern,
                options.InputArgumentName ?? parent.InputArgumentName,
                options.PayloadTypeNamePattern ?? parent.PayloadTypeNamePattern,
                options.PayloadErrorsFieldName ?? parent.PayloadErrorsFieldName,
                options.ApplyToAllMutations ?? parent.Apply);
        }

        return parent;
    }

    private static Options CreateOptions(
        MutationContextData contextData,
        Options parent = default)
        => new(
            contextData.InputTypeName ?? parent.InputTypeNamePattern,
            contextData.InputArgumentName ?? parent.InputArgumentName,
            contextData.PayloadTypeName?? parent.PayloadTypeNamePattern,
            parent.PayloadErrorsFieldName,
            contextData.Enabled);

    private readonly ref struct Options
    {
        public Options()
        {
            InputTypeNamePattern = MutationConventionOptionDefaults.InputTypeNamePattern;
            InputArgumentName = MutationConventionOptionDefaults.InputArgumentName;
            PayloadTypeNamePattern = MutationConventionOptionDefaults.PayloadTypeNamePattern;
            PayloadErrorsFieldName = MutationConventionOptionDefaults.PayloadErrorsFieldName;
            Apply = MutationConventionOptionDefaults.ApplyToAllMutations;
        }

        public Options(
            string inputTypeNamePattern,
            string inputArgumentName,
            string payloadTypeNamePattern,
            string payloadErrorsFieldName,
            bool apply)
        {
            InputTypeNamePattern = inputTypeNamePattern;
            InputArgumentName = inputArgumentName;
            PayloadTypeNamePattern = payloadTypeNamePattern;
            PayloadErrorsFieldName = payloadErrorsFieldName;
            Apply = apply;
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

