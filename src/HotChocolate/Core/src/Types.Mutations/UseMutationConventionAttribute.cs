namespace HotChocolate.Types;

/// <summary>
/// By annotating a mutation with this attribute one can override the global
/// mutation convention settings on a per mutation basis.
/// </summary>
public class UseMutationConventionAttribute : ObjectFieldDescriptorAttribute
{
    /// <summary>
    /// The type name of the mutation input type.
    /// <code>
    /// type Mutation {
    ///   createUser(input: ThisIsTheTypeName): CreateUserPayload
    /// }
    /// </code>
    /// </summary>
    public string? InputTypeName { get; set; }

    /// <summary>
    /// The name of the input argument.
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
    /// type Mutation {
    ///   createUser(input: CreateUserCustomInput): ThisIsTheTypeName
    /// }
    /// </code>
    /// </summary>
    public string? PayloadTypeName { get; set; }

    /// <summary>
    /// The name of the field in the payload type that represents our data.
    /// <code>
    /// type Mutation {
    ///   createUser(input: CreateUserCustomInput): CreateUserPayload
    /// }
    ///
    /// type CreateUserPayload {
    ///    thisIsTheFieldName: User
    ///    errors: [CreateUserError!]
    /// }
    /// </code>
    /// </summary>
    public string? PayloadFieldName { get; set; }

    /// <summary>
    /// The name of the error union type for this mutation.
    /// <code>
    /// type Mutation {
    ///   createUser(input: CreateUserCustomInput): CreateUserPayload
    /// }
    ///
    /// type CreateUserPayload {
    ///    user: User
    ///    errors: [ThisIsTheTypeName!]
    /// }
    /// </code>
    /// </summary>
    public string? PayloadErrorTypeName { get; set; }

    /// <summary>
    /// The name of the errors field name on the payload type.
    /// <code>
    /// type Mutation {
    ///   createUser(input: CreateUserCustomInput): CreateUserPayload
    /// }
    ///
    /// type CreateUserPayload {
    ///    user: User
    ///    thisIsTheFieldName: [CreateUserError!]
    /// }
    /// </code>
    /// </summary>
    public string? PayloadErrorsFieldName { get; set; }

    /// <summary>
    /// Overrides a the global <see cref="MutationConventionOptions.ApplyToAllMutations" />
    /// setting on a specific mutation.
    /// </summary>
    public bool Disable { get; set; }

    protected override void OnConfigure(
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
                    PayloadErrorsFieldName,
                    !Disable));
        });
    }
}
