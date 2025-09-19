using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Data.Sorting;

public interface ISortFieldConfiguration
    : ITypeSystemConfiguration
    , IDirectiveConfigurationProvider
    , IIgnoreConfiguration
    , IHasScope
{
    public MemberInfo? Member { get; }

    public ISortFieldHandler? Handler { get; }

    public Expression? Expression { get; }
}
