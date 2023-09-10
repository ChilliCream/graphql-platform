namespace HotChocolate.Types;

/// <summary>
/// Represent the mutation convention field option that allows to override the global convention
/// settings on a per field basis.
/// </summary>
public struct MutationFieldOptions
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
}
