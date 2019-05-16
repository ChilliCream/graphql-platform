using System;
using System.Reflection;

namespace HotChocolate.Utilities
{
    public interface IDocumentationProvider
    {
        string GetTypeSummary(Type type);
        string GetMemberSummary(MemberInfo member);
        string GetParameterSummary(ParameterInfo parameter);
    }
}
