using System;
using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Types.Paging
{
    public class PageableData<T>
    {
        public PageableData(IEnumerable<T> source)
            : this(source, null)
        {
        }

        public PageableData(
            IEnumerable<T> source,
            IDictionary<string, object> properties)
            : this(source?.AsQueryable(), properties)
        {
        }

        public PageableData(IQueryable<T> source)
            : this(source, null)
        {
        }

        public PageableData(
            IQueryable<T> source,
            IDictionary<string, object> properties)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Properties = properties;
        }

        public IQueryable<T> Source { get; }

        public IDictionary<string, object> Properties { get; }
    }
}
