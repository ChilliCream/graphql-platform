using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using HotChocolate.Utilities.Properties;
#if NET6_0_OR_GREATER
using static System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes;
#endif

namespace HotChocolate.Utilities;

public sealed class ServiceFactory
{
    private static readonly IServiceProvider _empty = new EmptyServiceProvider();

    public IServiceProvider? Services { get; set; }

#if NET6_0_OR_GREATER
    public object? CreateInstance([DynamicallyAccessedMembers(PublicConstructors)] Type type)
#else
    public object? CreateInstance(Type type)
#endif
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
}
