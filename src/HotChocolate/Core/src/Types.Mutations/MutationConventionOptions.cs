namespace HotChocolate.Types;

/// <summary>
/// Represents the global mutation convention settings.
/// The global convention settings will be applied to all
/// mutations and can be overriden on a per field basis.
/// </summary>
public struct MutationConventionOptions
{
    /// <summary>
    /// Specifies a name pattern for the input type name of a mutation.
    /// The pattern is specified like the following:
    /// <code>"{MutationName}Input"</code>
    ///
    /// <code>
    /// type Mutation {
    ///   createUser(input: ThisTypeName): CreateUserPayload
    /// }
    ///
    /// type CreateUserPayload {
    ///    user: User
    ///    errors: [CreateUserError!]
    /// }
    /// </code>
    /// </summary>
    public string? InputTypeNamePattern { get; set; }

    /// <summary>
    /// The name of the input argument.
    /// <code>
    /// type Mutation {
    ///   createUser(thisIsTheArgumentName: CreateUserCustomInput): CreateUserPayload
    /// }
    ///
    /// type CreateUserPayload {
    ///    user: User
    ///    errors: [CreateUserError!]
    /// }
    /// </code>
    /// </summary>
    public string? InputArgumentName { get; set; }

    /// <summary>
    /// Specifies a name pattern for the payload type name of a mutation.
    /// The pattern is specified like the following:
    /// <code>"{MutationName}Payload"</code>
    ///
    /// <code>
    /// type Mutation {
    ///   createUser(input: CreateUserCustomInput): ThisTypeName
    /// }
    ///
    /// type ThisTypeName {
    ///    user: User
    ///    errors: [CreateUserError!]
    /// }
    /// </code>
    /// </summary>
    public string? PayloadTypeNamePattern { get; set; }

    /// <summary>
    /// Specifies a name pattern for the error union type name of a mutation.
    /// The pattern is specified like the following:
    /// <code>"{MutationName}Error"</code>
    ///
    /// <code>
    /// type Mutation {
    ///   createUser(input: CreateUserCustomInput): CreateUserPayload
    /// }
    ///
    /// type CreateUserPayload {
    ///    user: User
    ///    errors: [ThisTypeName!]
    /// }
    /// </code>
    /// </summary>
    public string? PayloadErrorTypeNamePattern { get; set; }

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
    /// Defines if the mutation conventions shall be automatically applied to all mutation fields.
    /// </summary>
    public bool? ApplyToAllMutations { get; set; }
}
