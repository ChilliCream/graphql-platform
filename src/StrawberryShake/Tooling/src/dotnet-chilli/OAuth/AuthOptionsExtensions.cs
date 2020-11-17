using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake.Tools.Abstractions;
using StrawberryShake.Tools.Options;
using HCErrorBuilder = HotChocolate.ErrorBuilder;

namespace StrawberryShake.Tools.OAuth
{
    public static class AuthOptionsExtensions
    {

        public static async Task<AccessToken?> RequestTokenAsync(
            this AuthOptions auth,
            IConsoleOutput output,
            CancellationToken cancellationToken)
        {
            if (auth.Token is { Length: > 0 })
            {
                return new AccessToken(
                    auth.Token!.Trim(),
                    auth.Scheme is { Length: > 0 } ? auth.Scheme!.Trim() : AuthOptions.DefaultScheme);
            }

            if (auth.TokenEndpoint is { Length: > 0 } || auth.ClientId is { Length: > 0 } || auth.ClientSecret is { Length: > 0 })
            {
                using IActivity activity = output.WriteActivity("Request token");

                ValidateOAuthArguments(auth, activity);
                var scopes = auth.Scopes is { Length: > 0 }
                    ? auth.Scopes.Where(t => t is { Length: > 0 })
                    : Enumerable.Empty<string>();

                string token = await TokenClient.GetTokenAsync(
                     auth.TokenEndpoint!.Trim(),
                     auth.ClientId!,
                     auth.ClientSecret!.Trim(),
                     scopes,
                     cancellationToken
                ).ConfigureAwait(false);

                return new AccessToken(token, AuthOptions.DefaultScheme);
            }

            return null;
        }

        private static void ValidateOAuthArguments(AuthOptions auth, IActivity activity)
        {
            if (!(auth.TokenEndpoint is { Length: > 0 }))
            {
                activity.WriteError(
                    HCErrorBuilder.New()
                        .SetMessage("TokenEndpoint has to be set.")
                        .SetCode("ARGUMENT_MISSING")
                        .Build());
            }

            if (!(auth.ClientId is { Length: > 0 }))
            {
                activity.WriteError(
                    HCErrorBuilder.New()
                        .SetMessage("ClientId has to be set.")
                        .SetCode("ARGUMENT_MISSING")
                        .Build());
            }

            if (!(auth.ClientSecret is { Length: > 0 }))
            {
                activity.WriteError(
                    HCErrorBuilder.New()
                        .SetMessage("ClientSecret has to be set.")
                        .SetCode("ARGUMENT_MISSING")
                        .Build());
            }
        }
    }
}
