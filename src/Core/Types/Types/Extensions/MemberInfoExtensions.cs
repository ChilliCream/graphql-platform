using System.Reflection;
using System.Threading.Tasks;
using NJsonSchema.Infrastructure;

namespace HotChocolate.Types
{
    public static class MemberInfoExtensions
    {
        /// <summary>Returns the contents of the "summary" XML documentation tag for the specified member.</summary>
        /// <param name="member">The reflected member.</param>
        /// <returns>The contents of the "summary" tag for the member.</returns>
        public static string GetXmlSummary(this MemberInfo member)
        {
            var summary = Task.Run(member.GetXmlSummaryAsync)
                .GetAwaiter()
                .GetResult();
            return string.IsNullOrWhiteSpace(summary) ? null : summary;
        }
    }
}
