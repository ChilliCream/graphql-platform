using System;
using System.Reflection;

namespace HotChocolate.Types.Descriptors
{
    public interface IDocumentationProvider
    {
        string GetTypeSummary(Type type);
        string GetMemberSummary(MemberInfo member);
        string GetParameterSummary(ParameterInfo parameter);
    }
}
