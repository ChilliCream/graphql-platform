using System;

namespace HotChocolate;

public interface IGenericTypeArgumentNamingConvention
{
    string GetGenericTypeArgumentName(Type type);
}
