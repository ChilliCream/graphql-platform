namespace HotChocolate.Types;

/// <summary>
/// Represents types that can be used as argument types or as variable types.
/// These types essentially specify the data that can be passed into a GraphQL server.
///
/// Spec: https://spec.graphql.org/draft/#sec-Input-and-Output-Types
/// </summary>
public interface IInputType : IType, IHasRuntimeType;
