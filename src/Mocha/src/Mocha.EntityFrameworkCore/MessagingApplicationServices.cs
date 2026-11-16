using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Mocha.EntityFrameworkCore;

/// <summary>
/// The application services a messaging DbContext was configured from, available to the services
/// that messaging registers in the DbContext internal service provider.
/// </summary>
internal sealed class MessagingApplicationServices(IDbContextOptions options)
{
    public IServiceProvider Services { get; } =
        options.FindExtension<MessagingDbContextOptionsExtension>()!.ApplicationServices;
}
