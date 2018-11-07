using System.Collections.Generic;

namespace HotChocolate.Execution
{
    internal abstract class QueryResultVisitor<TContext>
    {
        public virtual void Visit(IQueryExecutionResult result)
        {
            if (result.Errors != null)
            {

            }

            if (result.Data != null)
            {

            }
        }



        protected virtual void Visit(
            ICollection<KeyValuePair<string, object>> dictionary,
            TContext context)
        {
            foreach (KeyValuePair<string, object> field in dictionary)
            {
                Visit(field, context);
            }
        }

        protected virtual void Visit(
            KeyValuePair<string, object> field,
            TContext context)
        {
            Visit(field.Value, context);
        }

        protected virtual void Visit(IList<object> list, TContext context)
        {
            for (int i = 0; i < list.Count; i++)
            {
                Visit(list[i], context);
            }
        }

        protected virtual void Visit(LeafValue value, TContext context)
        {

        }

        protected virtual void Visit(object value, TContext context)
        {
            if (value is IDictionary<string, object> dictionary)
            {
                Visit(dictionary, context);
            }
            else if (value is IList<object> list)
            {
                Visit(list, context);
            }
            else if (value is LeafValue leaf)
            {
                Visit(leaf, context);
            }
        }

    }
}
