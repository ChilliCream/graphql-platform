using System.CommandLine.Binding;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AspNetCore.CommandLine;

internal static class Bind
{
    public static ServiceProviderBinder<T> FromServiceProvider<T>() where T : notnull
        => ServiceProviderBinder<T>.Instance;

    internal sealed class ServiceProviderBinder<T> : BinderBase<T> where T : notnull
    {
        public static ServiceProviderBinder<T> Instance { get; } = new();

        protected override T GetBoundValue(BindingContext bindingContext)
            => bindingContext.GetRequiredService<T>();
    }
}
