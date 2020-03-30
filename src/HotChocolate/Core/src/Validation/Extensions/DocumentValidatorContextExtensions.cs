using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Validation
{
    internal static class DocumentValidatorContextExtensions
    {
        public static IReadOnlyList<object> CreateErrorPath(this IDocumentValidatorContext context)
        {
            var list = new List<object>();
            for (int i = 0; i < context.Path.Count; i++)
            {
                if (context.Path[i] is FieldNode field)
                {
                    list.Add(field.Alias?.Value ?? field.Name.Value);
                }
            }
            return list;
        }
    }
}
