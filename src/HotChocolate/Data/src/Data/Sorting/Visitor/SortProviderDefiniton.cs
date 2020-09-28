using System;
using System.Collections.Generic;

namespace HotChocolate.Data.Sorting
{
    public class SortProviderDefinition
    {
        public IList<(Type Handler, ISortFieldHandler? HandlerInstance)> Handlers { get; } =
            new List<(Type Handler, ISortFieldHandler? HandlerInstance)>();

        public IList<(Type Handler, ISortOperationHandler? HandlerInstance)> OperationHandlers
        {
            get;
        } = new List<(Type Handler, ISortOperationHandler? HandlerInstance)>();
    }
}
