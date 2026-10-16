using Microsoft.Extensions.DependencyInjection;

namespace Mocha.EntityFrameworkCore;

internal static class OutboxDbContextServiceCollectionExtensions
{
    public static bool AddOutboxDbContext(this IServiceCollection services, Type contextType)
    {
        foreach (var descriptor in services)
        {
            if (descriptor.ServiceType == typeof(OutboxDbContextRegistration))
            {
                var registration = (OutboxDbContextRegistration)descriptor.ImplementationInstance!;

                if (registration.ContextType != contextType)
                {
                    throw new InvalidOperationException(
                        $"An outbox is already registered for DbContext '{registration.ContextType.FullName}'. "
                        + $"Registering an outbox for another DbContext '{contextType.FullName}' is not supported.");
                }

                return false;
            }
        }

        services.AddSingleton(new OutboxDbContextRegistration(contextType));
        return true;
    }

    private sealed record OutboxDbContextRegistration(Type ContextType);
}
