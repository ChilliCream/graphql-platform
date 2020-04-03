using System.IO;
using System;
using Snapshooter.Xunit;
using Xunit;

namespace GreenDonut
{
    public class DataLoaderOptionsTests
    {
        #region Constructor

        [Fact(DisplayName = "Constructor: Should not throw any exception")]
        public void ConstructorNoException()
        {
            // act
            Action verify = () => new DataLoaderOptions<string>();

            // assert
            Assert.Null(Record.Exception(verify));
        }

        [Fact(DisplayName = "Constructor: Should set all properties")]
        public void ConstructorAllProps()
        {
            // act
            var options = new DataLoaderOptions<string>
            {
                AutoDispatching = true,
                Batching = false,
                BatchRequestDelay = TimeSpan.FromSeconds(1),
                Cache = new TaskCache(1),
                CacheKeyResolver = k => k,
                CacheSize = 1,
                Caching = false,
                MaxBatchSize = 1
            };

            // assert
            options.MatchSnapshot(matchOptions => matchOptions
                .Assert(fieldOption =>
                    Assert.NotNull(fieldOption.Field<object>("CacheKeyResolver"))));
        }

        [Fact(DisplayName = "Constructor: Should result in defaults")]
        public void ConstructorEmpty()
        {
            // act
            var options = new DataLoaderOptions<string>();

            // assert
            options.MatchSnapshot();
        }

        #endregion
    }
}
