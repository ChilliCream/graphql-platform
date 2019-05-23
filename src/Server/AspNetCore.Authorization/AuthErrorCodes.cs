#if ASPNETCLASSIC
namespace HotChocolate.AspNetClassic.Authorization
#else
namespace HotChocolate.AspNetCore.Authorization
#endif
{
    internal static class AuthErrorCodes
    {
        public const string NotAuthorized = "AUTH_NOT_AUTHORIZED";
        public const string NoDefaultPolicy = "AUTH_NO_DEFAULT_POLICY";
        public const string PolicyNotFound = "AUTH_POLICY_NOT_FOUNT";
    }
}
