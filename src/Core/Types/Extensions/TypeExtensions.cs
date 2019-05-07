using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NJsonSchema.Infrastructure;

namespace HotChocolate
{
    public static class TypeExtensions
    {
        /// <summary>Returns the contents of the "summary" XML documentation tag for the specified member.</summary>
        /// <param name="type">The type.</param>
        /// <returns>The contents of the "summary" tag for the member.</returns>
        public static string GetXmlSummary(this Type type)
        {
            return type.GetXmlSummaryAsync()
                .GetAwaiter()
                .GetResult();
        }
    }
}
