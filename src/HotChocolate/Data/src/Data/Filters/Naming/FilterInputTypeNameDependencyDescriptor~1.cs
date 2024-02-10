using System;
using HotChocolate.Types;
using HotChocolate.Types.Helpers;

namespace HotChocolate.Data.Filters;

internal sealed class FilterInputTypeNameDependencyDescriptor<T>
    : IFilterInputTypeNameDependencyDescriptor<T>
{
    private readonly IFilterInputTypeDescriptor<T> _descriptor;
    private readonly Func<INamedType, string> _createName;

    public FilterInputTypeNameDependencyDescriptor(
        IFilterInputTypeDescriptor<T> descriptor,
        Func<INamedType, string> createName)
    {
        _descriptor = descriptor ??
            throw new ArgumentNullException(nameof(descriptor));
        _createName = createName ??
            throw new ArgumentNullException(nameof(createName));
    }

    public IFilterInputTypeDescriptor<T> DependsOn<TDependency>()
        where TDependency : IType
    {
        TypeNameHelper.AddNameFunction(_descriptor, _createName, typeof(TDependency));
        return _descriptor;
    }

    public IFilterInputTypeDescriptor<T> DependsOn(Type schemaType)
    {
        TypeNameHelper.AddNameFunction(_descriptor, _createName, schemaType);
        return _descriptor;
    }
}
