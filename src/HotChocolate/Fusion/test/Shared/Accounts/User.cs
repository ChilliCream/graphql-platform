using HotChocolate.Types.Relay;

namespace HotChocolate.Fusion.Shared.Accounts;

[Node]
public record User(int Id, string Name, DateTime Birthdate, string Username);
