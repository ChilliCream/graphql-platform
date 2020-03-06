using System;
using System.Threading.Tasks;
using Xunit;

namespace MarshmallowPie.Processing.InMemory
{
    public class SessionManagerTests
    {
        [Fact]
        public async Task CreateSession()
        {
            // arrange
            var sessionManager = new SessionManager();

            // act
            string sessionId = await sessionManager.CreateSessionAsync();

            // assert
            Assert.NotNull(sessionId);
            Assert.NotEqual(string.Empty, sessionId);
        }

        [Fact]
        public async Task ValidateSession_Session_Is_Valid()
        {
            // arrange
            var sessionManager = new SessionManager();
            string sessionId = await sessionManager.CreateSessionAsync();

            // act
            sessionManager.ValidateSession(sessionId);

            // assert
            // no exception is thrown.
        }

        [Fact]
        public void ValidateSession_Session_Is_Invalid()
        {
            // arrange
            var sessionManager = new SessionManager();
            string sessionId = Guid.NewGuid().ToString();

            // act
            Action action = () => sessionManager.ValidateSession(sessionId);

            // assert
            Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public async Task RemoveSession()
        {
            // arrange
            var sessionManager = new SessionManager();
            string sessionId = await sessionManager.CreateSessionAsync();
            sessionManager.ValidateSession(sessionId);

            // act
            sessionManager.RemoveSession(sessionId);

            // assert
            Assert.Throws<ArgumentException>(
                () => sessionManager.ValidateSession(sessionId));
        }
    }
}
