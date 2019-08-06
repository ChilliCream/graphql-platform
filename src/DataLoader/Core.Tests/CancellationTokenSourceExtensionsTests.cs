using System.Threading;
using Xunit;

namespace GreenDonut
{
    public class CancellationTokenSourceExtensionsTests
    {
        #region CreateLinkedCancellationToken

        [Fact(DisplayName = "CreateLinkedCancellationToken: Should return the provided token if source is null")]
        public void CreateLinkedCancellationTokenSourceNull()
        {
            // arrange
            CancellationTokenSource source = null;
            CancellationToken token = new CancellationTokenSource().Token;

            // act
            CancellationToken combinedToken = source
                .CreateLinkedCancellationToken(token);

            // assert
            Assert.Equal(token, combinedToken);
        }

        [Fact(DisplayName = "CreateLinkedCancellationToken: Should return the token of source if token is equal none")]
        public void CreateLinkedCancellationTokenNone()
        {
            // arrange
            var source = new CancellationTokenSource();
            CancellationToken token = CancellationToken.None;

            // act
            CancellationToken combinedToken = source
                .CreateLinkedCancellationToken(token);

            // assert
            Assert.Equal(source.Token, combinedToken);
        }

        [Fact(DisplayName = "CreateLinkedCancellationToken: Should return a combined token")]
        public void CreateLinkedCancellationToken()
        {
            // arrange
            var source = new CancellationTokenSource();
            CancellationToken token = new CancellationTokenSource().Token;

            // act
            CancellationToken combinedToken = source
                .CreateLinkedCancellationToken(token);

            // assert
            Assert.NotEqual(source.Token, combinedToken);
            Assert.NotEqual(token, combinedToken);
        }

        #endregion
    }
}
