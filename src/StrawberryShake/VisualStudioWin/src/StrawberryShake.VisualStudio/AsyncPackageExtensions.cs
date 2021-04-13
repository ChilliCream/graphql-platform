using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

namespace StrawberryShake.VisualStudio
{
    public static class AsyncPackageExtensions
    {
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
        public static TService GetService<TService>(this AsyncPackage package) =>
            GetServiceAsync<TService, TService>(package).Result;
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits

        public static Task<TService> GetServiceAsync<TService>(this AsyncPackage package) =>
            GetServiceAsync<TService, TService>(package);

        public static async Task<TCast> GetServiceAsync<TService, TCast>(this AsyncPackage package)
        {
            if (package is null)
            {
                throw new ArgumentNullException(nameof(package));
            }

            if ((await package.GetServiceAsync(typeof(TService)).ConfigureAwait(false)) is TCast casted)
            {
                return casted;
            }

            throw new ArgumentException($"The service {typeof(TCast).FullName} was not found.");
        }
    }
}
