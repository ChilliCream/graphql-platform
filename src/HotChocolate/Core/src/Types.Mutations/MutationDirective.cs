namespace HotChocolate.Types;

/// <summary>
/// This is a schema building directive for schema-first.
/// </summary>
internal sealed class MutationDirective : ISchemaDirective
{
    public string Name => "mutation";

    public void ApplyConfiguration(
        IDescriptorContext context,
        DirectiveNode directiveNode,
        IDefinition definition,
        Stack<IDefinition> path)
    {
        if (definition is not ObjectFieldDefinition fieldDef)
        {
            throw ThrowHelper.MutationConvDirective_In_Wrong_Location(directiveNode);
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
        var data = new MutationDirectiveData { Enabled = true, };
        var args = directiveNode.Arguments;

        for (var i = 0; i < args.Count; i++)
        {
            var arg = args[i];

            switch (arg.Name.Value)
            {
                case "inputTypeName":
                    data.InputTypeName = ExpectStringValue(arg.Name.Value, arg.Value);
                    break;

                case "inputArgumentName":
                    data.InputArgumentName = ExpectStringValue(arg.Name.Value, arg.Value);
                    break;

                case "payloadFieldName":
                    data.PayloadFieldName = ExpectStringValue(arg.Name.Value, arg.Value);
                    break;

                case "payloadTypeName":
                    data.PayloadTypeName = ExpectStringValue(arg.Name.Value, arg.Value);
                    break;

                case "payloadPayloadErrorTypeName":
                    data.PayloadPayloadErrorTypeName = ExpectStringValue(arg.Name.Value, arg.Value);
                    break;

                case "payloadErrorsFieldName":
                    data.PayloadErrorsFieldName = ExpectStringValue(arg.Name.Value, arg.Value);
                    break;

                case "enabled":
                    if (!(ExpectBooleanValue(arg.Name.Value, arg.Value) ?? true))
                    {
                        data.Enabled = false;
                    }
                    break;

                default:
                    throw ThrowHelper.UnknownDirectiveArgument(arg.Name.Value);
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

        static string? ExpectStringValue(string argumentName, IValueNode literal)
        {
            if (literal.Kind is SyntaxKind.StringValue)
            {
                return ((StringValueNode)literal).Value;
            }

            if (literal.Kind is SyntaxKind.NullValue)
            {
                return null;
            }

            throw ThrowHelper.DirectiveArgument_Unexpected_Value(argumentName, "string");
        }

        static bool? ExpectBooleanValue(string argumentName, IValueNode literal)
        {
            if (literal.Kind is SyntaxKind.BooleanValue)
            {
                return ((BooleanValueNode)literal).Value;
            }

            if (literal.Kind is SyntaxKind.NullValue)
            {
                return null;
            }

            throw ThrowHelper.DirectiveArgument_Unexpected_Value(argumentName, "boolean");
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
