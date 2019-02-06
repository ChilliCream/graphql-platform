using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Stitching
{
    public class QueryMerger
    {
        private readonly MergeQueryRewriter _rewriter =
            new MergeQueryRewriter(null);
        private int _index = 0;


        public QueryMerger AddQuery(
            DocumentNode query,
            Dictionary<string, object> variables)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            if (variables == null)
            {
                throw new ArgumentNullException(nameof(variables));
            }

            // string requestName = $"__{_index++}_"
            //_rewriter.AddQuery(query);
            return this;
        }

        public DocumentNode Build()
        {
            return null;
        }

        public static QueryMerger New() => new QueryMerger();
    }
}
