using System;
using System.Reflection;

namespace HotChocolate.Types.Descriptors
{
    public interface IDocumentationProvider
    {
        string GetDescription(Type type);
        string GetDescription(MemberInfo member);
        string GetDescription(ParameterInfo parameter);
    }
}
