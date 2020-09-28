using System.Collections.Generic;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Stitching.Processing
{
    public interface IFetch
    {
        NameString Schema { get; }

        NameString Type { get; }

        IReadOnlyList<Field> Requires { get; }

        /// <summary>
        /// Gets the selections that can be handled by this <see cref="IFetch" />.
        /// </summary>
        SelectionSetNode Provides { get; }

        IFetchHandler Handler { get; }
    }

    public interface IFetchHandler
    {
        bool CanHandleSelections(
            IPreparedOperation operation,
            ISelectionSet selectionSet,
            IObjectType typeContext,
            out int handledSelections);

        IFetchCall CompileFetch(
            IPreparedOperation operation,
            ISelectionSet selectionSet,
            IObjectType typeContext,
            out IReadOnlyList<ISelection> handledSelections);
    }


    public class GraphQLFetchHandler : IFetchHandler
    {
        private static readonly MatchSelectionsVisitor _matchSelections = 
            new MatchSelectionsVisitor();
        private readonly ISchema _schema;
        private readonly SelectionSetNode _provides;

        public bool CanHandleSelections(
            IPreparedOperation operation, 
            ISelectionSet selectionSet, 
            IObjectType typeContext,
            out int handledSelections)
        {
            var context = new MatchSelectionsContext(
                _schema, operation, selectionSet, typeContext);
            _matchSelections.Visit(_provides, context);
            handledSelections = context.Count;
            return context.Count != 0;
        }

        public IFetchCall CompileFetch(
            IPreparedOperation operation, 
            ISelectionSet selectionSet, 
            IObjectType typeContext,
            out IReadOnlyList<ISelection> handledSelections)
        {
            throw new System.NotImplementedException();
        }
    }




}
