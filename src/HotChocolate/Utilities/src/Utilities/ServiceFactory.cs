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
            try
            {
                return ActivatorHelper.CompileFactory(type).Invoke(Services ?? _empty);
            }
            catch (Exception ex)
            {
                throw new ServiceException(
                    string.Format(
                        UtilityResources.ServiceFactory_CreateInstanceFailed,
                        type.FullName,
                        CultureInfo.InvariantCulture),
                    ex);
            }
        }

        object? IServiceProvider.GetService(Type serviceType) =>
            CreateInstance(serviceType);
    }
}
