using System.Threading.Tasks;
using Xunit;

namespace MarshmallowPie.Processing.InMemory
{
    public class SessionCreatorTests
    {
        [Fact]
        public async Task CreateSession()
        {
            // arrange
            var sessionCreator = new SessionCreator();

            // act
            string sessionId = await sessionCreator.CreateSessionAsync();

            // assert
            Assert.NotNull(sessionId);
            Assert.NotEqual(string.Empty, sessionId);
        }
    }
}
