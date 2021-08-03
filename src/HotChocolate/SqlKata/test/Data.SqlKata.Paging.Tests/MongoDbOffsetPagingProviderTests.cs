using System;
using System.Linq;
using System.Reflection;
using HotChocolate.Data.SqlKata;
using HotChocolate.Data.SqlKata.Paging;
using HotChocolate.Internal;
using HotChocolate.Types.Descriptors;
using SqlKata.Driver;
using Xunit;

namespace HotChocolate.Data
{
    public class SqlKataOffsetPagingProviderTests
    {
        [Theory]
        [InlineData(nameof(AggregateFluent), true)]
        [InlineData(nameof(FindFluent), true)]
        [InlineData(nameof(MongoCollection), true)]
        [InlineData(nameof(ISqlKataExecutable), true)]
        [InlineData(nameof(IExecutable), false)]
        [InlineData(nameof(IQueryable), false)]
        public void CanHandle_MethodReturnType_MatchesResult(string methodName, bool expected)
        {
            // arrange
            var provider = new SqlKataOffsetPagingProvider();
            MethodInfo member = typeof(SqlKataOffsetPagingProviderTests).GetMethod(methodName)!;
            IExtendedType type = new DefaultTypeInspector().GetReturnType(member);

            // act
            var result = provider.CanHandle(type);

            // assert
            Assert.Equal(expected, result);
        }

        public IAggregateFluent<Foo> AggregateFluent() =>
            throw new InvalidOperationException();

        public IFindFluent<Foo, Foo> FindFluent() =>
            throw new InvalidOperationException();

        public IMongoCollection<Foo> MongoCollection() =>
            throw new InvalidOperationException();

        public ISqlKataExecutable ISqlKataExecutable() =>
            throw new InvalidOperationException();

        public IExecutable IExecutable() =>
            throw new InvalidOperationException();

        public IQueryable<Foo> IQueryable() =>
            throw new InvalidOperationException();

        public class Foo
        {
        }
    }
}
