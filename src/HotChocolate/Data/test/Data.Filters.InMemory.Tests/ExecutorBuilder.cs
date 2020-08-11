using System;
using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Data.Filters.Expressions
{
    public class ExecutorBuilder
    {
        private readonly IFilterInputType _inputType;
        private readonly QueryableFilterProvider _provider;

        public ExecutorBuilder(
            IFilterInputType inputType,
            FilterConvention filterConvention)
        {
            _inputType = inputType;
            _provider = filterConvention.Provider as QueryableFilterProvider ??
                throw new InvalidOperationException("Provider was null");
        }

        public Func<T, bool> Build<T>(IValueNode filter)
        {
            var visitorContext = new QueryableFilterContext(
                _inputType, true);

            _provider.Visitor.Visit(filter, visitorContext);

            if (visitorContext.TryCreateLambda(
                    out Expression<Func<T, bool>>? where))
            {
                return where.Compile();
            }
            throw new InvalidOperationException();
        }
    }
}
