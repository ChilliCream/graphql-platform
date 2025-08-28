using HotChocolate.Types.Descriptors;

namespace HotChocolate.Configuration;

internal sealed record ConventionRegistration(
    ConventionKey Key,
    Func<IServiceProvider, IConvention> Factory);
