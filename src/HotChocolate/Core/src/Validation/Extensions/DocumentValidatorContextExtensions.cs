using HotChocolate.Language;

namespace HotChocolate.Validation
{
    internal static class DocumentValidatorContextExtensions
    {
        public static Path? CreateErrorPath(this IDocumentValidatorContext context)
        {
            if (context.Path.Count == 0)
            {
                return null;
            }

            Path? segment = null;

            for (var i = 0; i < context.Path.Count; i++)
            {
                if (context.Path[i] is FieldNode field)
                {
                    string name = field.Alias?.Value ?? field.Name.Value;
                    segment = segment is null ? Path.New(name) : segment.Append(name);
                }
            }

            return segment;
        }
    }
}
