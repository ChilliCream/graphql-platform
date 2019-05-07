using System.Reflection;
using NJsonSchema.Infrastructure;

namespace HotChocolate
{
    public static class ParameterInfoExtensions
    {
        /// <summary>Returns the contents of the "returns" or "param" XML documentation tag for the specified parameter.</summary>
        /// <param name="parameter">The reflected parameter or return info.</param>
        /// <returns>The contents of the "returns" or "param" tag.</returns>
        public static string GetXmlSummary(this ParameterInfo parameter)
        {
            return parameter.GetXmlDocumentationAsync()
                .GetAwaiter()
                .GetResult();
        }
    }
}
