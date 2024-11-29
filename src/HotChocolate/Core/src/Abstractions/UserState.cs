using System.Security.Claims;

namespace HotChocolate;

/// <summary>
/// The Hot Chocolate user state can be provided by GraphQL server implementations
/// and authorization implementations are depending on this state being added to
/// the global state.
/// </summary>
public sealed class UserState : IEquatable<UserState>
{
    /// <summary>
    /// Initializes a new instance of <see cref="UserState"/>.
    /// </summary>
    /// <param name="user">
    /// The currently signed in user.
    /// </param>
    /// <param name="isAuthenticated">
    /// Specifies if the currently signed in user is authenticated.
    /// </param>
    public UserState(ClaimsPrincipal user, bool? isAuthenticated = null)
    {
        User = user;
        IsAuthenticated = isAuthenticated;
    }

    /// <summary>
    /// The currently signed in user.
    /// </summary>
    public ClaimsPrincipal User { get; }

    /// <summary>
    /// Specifies if the currently signed in user is authenticated.
    /// If this property is null it means that this state has not yet been determined.
    /// </summary>
    public bool? IsAuthenticated { get; }

    /// <summary>
    /// Sets the is authenticated state.
    /// </summary>
    /// <param name="isAuthenticated">
    /// The authentication state.
    /// </param>
    /// <returns>
    /// Returns a new user state that contains the authentication state change.
    /// </returns>
    public UserState SetIsAuthenticated(bool isAuthenticated)
        => IsAuthenticated == isAuthenticated
            ? this
            : new UserState(User, isAuthenticated);

    /// <summary>
    /// Indicates whether the current user state is equal to another user state of the same type.
    /// </summary>
    /// <param name="other">A user state to compare with this user state.</param>
    /// <returns>
    /// <c>true</c> if the current user state is equal to the
    /// <paramref name="other">other user state</paramref> user state parameter;
    /// otherwise, <c>false</c>.
    /// </returns>
    public bool Equals(UserState? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return User.Equals(other.User) && IsAuthenticated == other.IsAuthenticated;
    }

    /// <summary>
    /// Indicates whether the current user state is equal to another user state of the same type.
    /// </summary>
    /// <param name="obj">A user state to compare with this user state.</param>
    /// <returns>
    /// <c>true</c> if the current user state is equal to the
    /// <paramref name="obj">other user state</paramref> parameter;
    /// otherwise, <c>false</c>.
    /// </returns>
    public override bool Equals(object? obj)
        => ReferenceEquals(this, obj) || (obj is UserState other && Equals(other));

    /// <summary>
    /// Serves as the user state hash function.
    /// </summary>
    /// <returns>
    /// A hash code for the current user state.
    /// </returns>
    public override int GetHashCode()
        => HashCode.Combine(User, IsAuthenticated);
}
