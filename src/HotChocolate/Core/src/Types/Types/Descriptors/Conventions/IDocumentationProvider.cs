using System;
using System.Reflection;

namespace HotChocolate.Types.Descriptors
{
    public interface IDocumentationProvider
    {
        string GetSummary(Type type);
        string GetSummary(MemberInfo member);
        string GetSummary(ParameterInfo parameter);
    }
}
