using System.Security.Claims;

namespace HotChocolate
{
    public static class WellKnownContextData
    {
        public const string Id = "HotChocolate.Id";
        public const string Type = "HotChocolate.Type";
        public const string Principal = nameof(ClaimsPrincipal);
        public const string EventMessage = "HotChocolate.Execution.EventMessage";
    }
}
