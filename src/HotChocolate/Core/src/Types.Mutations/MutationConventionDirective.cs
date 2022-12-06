namespace HotChocolate.Types;

internal sealed class MutationConventionDirective : ISchemaDirective
{
    public string Name => "mutationConvention";

    public void ApplyConfiguration(
        IDescriptorContext context,
        DirectiveNode directiveNode,
        IDefinition definition,
        Stack<IDefinition> path)
    {
        if (definition is not ObjectFieldDefinition fieldDef)
        {
            // TODD : Error details and resources
            throw new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage("The schema building directive `@mutationConvention` can only be applied on object fields.")
                    .Build());
        }
        fieldDef.Configurations.Add(
            new CompleteConfiguration<ObjectFieldDefinition>(
                (c, d) =>
                {
                    c.ContextData
                        .GetMutationFields()
                        .Add(CreateMutationContextData(directiveNode, d));
                },
                fieldDef,
                ApplyConfigurationOn.BeforeNaming));
    }

    private static MutationContextData CreateMutationContextData(
        DirectiveNode directiveNode,
        ObjectFieldDefinition fieldDef)
    {
        var data = new MutationDirectiveData { Enabled = true };
        var args = directiveNode.Arguments;

        for (var i = 0; i < args.Count; i++)
        {
            var arg = args[i];

            switch (arg.Name.Value)
            {
                case "inputTypeName":
                    data.InputTypeName = GetStringValue(arg.Name.Value, arg.Value);
                    break;

                case "inputArgumentName":
                    data.InputArgumentName = GetStringValue(arg.Name.Value, arg.Value);
                    break;

                case "payloadFieldName":
                    data.PayloadFieldName = GetStringValue(arg.Name.Value, arg.Value);
                    break;

                case "payloadTypeName":
                    data.PayloadTypeName = GetStringValue(arg.Name.Value, arg.Value);
                    break;

                case "payloadPayloadErrorTypeName":
                    data.PayloadPayloadErrorTypeName = GetStringValue(arg.Name.Value, arg.Value);
                    break;

                case "payloadErrorsFieldName":
                    data.PayloadErrorsFieldName = GetStringValue(arg.Name.Value, arg.Value);
                    break;

                case "enabled":
                    if (!(GetBooleanValue(arg.Name.Value, arg.Value) ?? true))
                    {
                        data.Enabled = false;
                    }
                    break;

                default:
                    // TODO : error resources
                    throw new SchemaException(
                        SchemaErrorBuilder.New()
                            .SetMessage(
                                $"`{arg.Name.Value}` is not a valid argument name of the directive `@mutationConvention`.")
                            .Build());
            }
        }

        return new MutationContextData(
            fieldDef,
            data.InputTypeName,
            data.InputArgumentName,
            data.PayloadTypeName,
            data.PayloadFieldName,
            data.PayloadPayloadErrorTypeName,
            data.PayloadErrorsFieldName,
            data.Enabled);

        static string? GetStringValue(string argumentName, IValueNode literal)
        {
            if (literal.Kind is SyntaxKind.StringValue)
            {
                return ((StringValueNode)literal).Value;
            }

            if (literal.Kind is SyntaxKind.NullValue)
            {
                return null;
            }

            // TODO : error resources
            throw new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage($"Argument `{argumentName}` of the `@mutationConvention` directive must be null or a string value.")
                    .Build());
        }

        static bool? GetBooleanValue(string argumentName, IValueNode literal)
        {
            if (literal.Kind is SyntaxKind.BooleanValue)
            {
                return ((BooleanValueNode)literal).Value;
            }

            if (literal.Kind is SyntaxKind.NullValue)
            {
                return null;
            }

            // TODO : error resources
            throw new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage($"Argument `{argumentName}` of the `@mutationConvention` directive must be null or a boolean value.")
                    .Build());
        }
    }

    private ref struct MutationDirectiveData
    {
        public string? InputTypeName { get; set; }

        public string? InputArgumentName { get; set; }

        public string? PayloadFieldName { get; set; }

        public string? PayloadTypeName { get; set; }

        public string? PayloadPayloadErrorTypeName { get; set; }

        public string? PayloadErrorsFieldName { get; set; }

        public bool Enabled { get; set; }
    }
}
