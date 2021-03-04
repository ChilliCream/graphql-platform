using System;
using System.Threading;
using System.Threading.Tasks;
using StrawberryShake.Transport.Http;
using Xunit;

namespace StrawberryShake.Transport.InMemory
{
    public class InMemoryConnectionTests
    {
        [Fact]
        public void Constructor_AllArgs_NoException()
        {
            // arrange
            Func<CancellationToken, ValueTask<IInMemoryClient>> create = _ => default!;

            // act
            Exception? ex = Record.Exception(() => new InMemoryConnection(create));

            // assert
            Assert.Null(ex);
        }

        [Fact]
        public void Constructor_NoName_ThrowException()
        {
            // arrange
            Func<CancellationToken, ValueTask<IInMemoryClient>> create = null!;

            // act
            Exception? ex = Record.Exception(() => new InMemoryConnection(create));

            // assert
            Assert.IsType<ArgumentNullException>(ex);
        }
    }
}
