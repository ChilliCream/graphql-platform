using System;
using System.Globalization;
using HotChocolate.Utilities.Properties;

namespace HotChocolate.Utilities
{
    public class ServiceFactory : IServiceProvider
    {
        private static readonly IServiceProvider _empty = new EmptyServiceProvider();

        public IServiceProvider? Services { get; set; }

        public object? CreateInstance(Type type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            try
            {
                return ActivatorHelper.CompileFactory(type).Invoke(Services ?? _empty);
            }
            catch (Exception ex)
            {
                throw new ServiceException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        UtilityResources.ServiceFactory_CreateInstanceFailed,
                        type.FullName),
                    ex);
            }
        }

        object? IServiceProvider.GetService(Type serviceType) =>
            CreateInstance(serviceType);
    }
}
