namespace HotChocolate.Types;

public class MutationAttribute : ObjectFieldDescriptorAttribute
{
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
    /// The name of the argument of the field
    /// <code>
    /// type Mutation {
    ///   createUser(thisIsTheArgumentName: CreateUserCustomInput): CreateUserPayload
    /// }
    /// </code>
    /// </summary>
    public string? InputArgumentName { get; set; }

    /// <summary>
    /// The type name of the field in the payload
    /// <code>
    /// type ThisIsTheTypeName {
    ///    user: User
    /// }
    /// </code>
    /// </summary>
    public string? PayloadTypeName { get; set; }

    /// <summary>
    /// The name of the field in the payload
    /// <code>
    /// type CreateUserPayload {
    ///    thisIsTheFieldName: User
    /// }
    /// </code>
    /// </summary>
    public string? PayloadFieldName { get; set; }

    public string? PayloadErrorTypeName { get; set; }

    public bool Enabled { get; set; } = true;

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
                    InputTypeName,
                    InputArgumentName,
                    PayloadTypeName,
                    PayloadFieldName,
                    PayloadErrorTypeName,
                    Enabled));
        });
    }
}
