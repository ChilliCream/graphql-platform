using System;

namespace HotChocolate.Execution
{
    public class QuerySourceText
        : IQuery
    {
        public QuerySourceText(string source)
        {
            Text = source
                ?? throw new ArgumentNullException(nameof(source));
        }

        public string Text { get; }
    }
}
