using HotChocolate.Authorization.Properties;
using HotChocolate.Language;
using HotChocolate.Validation;
using static HotChocolate.WellKnownContextData;

namespace HotChocolate.Authorization;

internal sealed class AuthorizeValidationResultAggregator : IValidationResultAggregator
{
    private readonly IServiceProvider _services;

    public AuthorizeValidationResultAggregator(
        IServiceProvider services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }


}
