using System;

namespace HotChocolate;

public interface ITypeNamingConvention
{
    string GetTypeName(Type type);
}
