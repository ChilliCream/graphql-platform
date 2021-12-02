namespace HotChocolate.Types;

public struct MutationConventionOptions
{
    public string? InputTypeNamePattern { get; set; }

    public string? InputArgumentName { get; set; }

    public string? PayloadTypeNamePattern { get; set; }

    public string? PayloadErrorsFieldName { get; set; }

    public bool? ApplyToAllMutations { get; set; }
}

internal class MutationContextData
{
    public MutationContextData(
        ObjectFieldDefinition definition,
        string? inputTypeName,
        string? inputArgumentName,
        string? payloadFieldName,
        string? payloadTypeName,
        bool enabled)
    {
        Definition = definition;
        InputTypeName = inputTypeName;
        InputArgumentName = inputArgumentName;
        PayloadFieldName = payloadFieldName;
        PayloadTypeName = payloadTypeName;
        Enabled = enabled;
    }

    public NameString Name => Definition.Name;

    public ObjectFieldDefinition Definition { get; }

    public string? InputTypeName { get; }

    public string? InputArgumentName { get; }

    public string? PayloadFieldName { get; }

    public string? PayloadTypeName { get; }

    public bool Enabled { get; }
}

public class MutationAttribute : ObjectFieldDescriptorAttribute
{
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// The name of the argument of the field
    /// <code>
    /// type Mutation {
    ///   createUser(thisIsTheArgumentName: CreateUserCustomInput): CreateUserPayload
    /// }
    /// </code>
    /// </summary>
    public string? InputArgumentName { get; set; }

    /// <summary>
    /// The type name of the argument of the field
    /// <code>
    /// type Mutation {
    ///   createUser(input: ThisIsTheTypeName): CreateUserPayload
    /// }
    /// </code>
    /// </summary>
    public string? InputTypeName { get; set; }

    /// <summary>
    /// The name of the field in the payload
    /// <code>
    /// type CreateUserPayload {
    ///    thisIsTheFieldName: User
    /// }
    /// </code>
    /// </summary>
    public string? PayloadFieldName { get; set; }

    /// <summary>
    /// The type name of the field in the payload
    /// <code>
    /// type ThisIsTheTypeName {
    ///    user: User
    /// }
    /// </code>
    /// </summary>
    public string? PayloadTypeName { get; set; }

    public override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo member)
    {
        descriptor.Extend().OnBeforeNaming((c, d) =>
        {
            c.ContextData
                .GetMutationFields()
                .Add(new(d,
                    InputArgumentName,
                    InputTypeName,
                    PayloadFieldName,
                    PayloadTypeName,
                    Enabled));
        });
    }
}


