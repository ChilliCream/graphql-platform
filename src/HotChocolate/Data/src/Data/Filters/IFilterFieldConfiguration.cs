using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Data.Filters;

public interface IFilterFieldConfiguration
    : ITypeSystemConfiguration
    , IDirectiveConfigurationProvider
    , IIgnoreConfiguration
    , IHasScope
{
    MemberInfo? Member { get; }

    IFilterFieldHandler? Handler { get; }

    Expression? Expression { get; }
}
