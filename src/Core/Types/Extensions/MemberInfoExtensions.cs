using System.Reflection;
using NJsonSchema.Infrastructure;

namespace HotChocolate
{
    public static class MemberInfoExtensions
    {
        /// <summary>Returns the contents of the "summary" XML documentation tag for the specified member.</summary>
        /// <param name="member">The reflected member.</param>
        /// <returns>The contents of the "summary" tag for the member.</returns>
        public static string GetXmlSummary(this MemberInfo member)
        {
            return member.GetXmlSummaryAsync()
                .GetAwaiter()
                .GetResult();
        }
    }
}
