namespace HotChocolate.Types;

/// <summary>
/// Marks a method as a batch resolver. A batch resolver receives lists of
/// parent objects and arguments, resolves them in a single invocation, and
/// returns a list of results — one per parent.
/// <para>
/// Unlike a normal resolver that is called once per parent object, a batch
/// resolver is called once for all sibling parent objects in a selection set.
/// This allows the resolver to perform a single operation (e.g. a database
/// query) for the entire batch instead of N individual operations.
/// </para>
/// <para>
/// The method's <c>[Parent]</c> parameter must be a list type (e.g.
/// <c>List&lt;T&gt;</c>, <c>IReadOnlyList&lt;T&gt;</c>, or <c>T[]</c>)
/// containing the parent objects. All argument parameters must also be list
/// types — one value per parent. The return type must be a list whose
/// element type becomes the GraphQL field type.
/// </para>
/// <example>
/// <code>
/// [ObjectType&lt;User&gt;]
/// public class UserExtensions
/// {
///     [BatchResolver]
///     public List&lt;string&gt; GetGreeting([Parent] List&lt;User&gt; users)
///     {
///         return users.Select(u =&gt; $"Hello, {u.Name}!").ToList();
///     }
/// }
/// </code>
/// </example>
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class BatchResolverAttribute : Attribute;
