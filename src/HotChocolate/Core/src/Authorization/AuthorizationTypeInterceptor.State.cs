using HotChocolate.Types.Descriptors;

namespace HotChocolate.Authorization;

internal sealed partial class AuthorizationTypeInterceptor
{
    private sealed class State
    {
        public State(AuthorizationOptions options)
        {
            Options = options;
        }

        /// <summary>
        /// Provides access to the authorization options.
        /// </summary>
        public AuthorizationOptions Options { get; }

        /// <summary>
        ///  Gets the types to which authorization middleware need to be applied.
        /// </summary>
        public HashSet<TypeReference> NeedsAuth { get; } = [];

        /// <summary>
        /// Gets the types to which are annotated with the @authorize directive.
        /// </summary>
        public HashSet<TypeReference> AuthTypes { get; } = [];

        /// <summary>
        /// Gets a lookup table from abstract types to concrete types that need authorization.
        /// </summary>
        public Dictionary<TypeReference, List<TypeReference>> AbstractToConcrete { get; } = new();

        /// <summary>
        /// Gets a helper queue for processing types.
        /// </summary>
        public List<TypeReference> Queue { get; } = [];

        /// <summary>
        /// Gets a helper set for tracking process completion.
        /// </summary>
        public HashSet<TypeReference> Completed { get; } = [];
    }
}
