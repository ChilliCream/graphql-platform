namespace HotChocolate.Types;

/// <summary>
/// <para>
/// Represents types that can be used as argument types or as variable types.
/// These types essentially specify the data that can be passed into a GraphQL server.
/// </para>
/// <para>
/// Spec: https://spec.graphql.org/draft/#sec-Input-and-Output-Types
/// </para>
/// </summary>
public interface IInputType : IType;
