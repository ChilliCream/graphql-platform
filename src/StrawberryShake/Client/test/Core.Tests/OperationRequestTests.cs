using System.Collections.Generic;
using System.Net.Http.Headers;
using Moq;
using Xunit;

namespace StrawberryShake
{
    public class OperationRequestTests
    {
        [Fact]
        public void Equals_With_Variables()
        {
            // arrange
            var document = new Mock<IDocument>();

            var a = new OperationRequest(
                null,
                "abc",
                document.Object,
                new Dictionary<string, object?>{ { "a", "a" } });

            var b = new OperationRequest(
                null,
                "abc",
                document.Object,
                new Dictionary<string, object?>{ { "a", "a" } });

            // act
            // assert
            Assert.True(a.Equals(b));
        }

        [Fact]
        public void Equals_With_Variables_List()
        {
            // arrange
            var document = new Mock<IDocument>();

            var a = new OperationRequest(
                null,
                "abc",
                document.Object,
                new Dictionary<string, object?> { { "a", new List<object?> { 1, 2, 3 } } });

            var b = new OperationRequest(
                null,
                "abc",
                document.Object,
                new Dictionary<string, object?> { { "a", new List<object?> { 1, 2, 3 } } });

            // act
            // assert
            Assert.True(a.Equals(b));

            b = new OperationRequest(
                null,
                "abc",
                document.Object,
                new Dictionary<string, object?> { { "a", new List<object?> { 1, 3, 2 } } });

            // act
            // assert
            Assert.False(a.Equals(b));

            b = new OperationRequest(
                null,
                "abc",
                document.Object,
                new Dictionary<string, object?> { { "a", new List<object?> { 1, 3 } } });

            // act
            // assert
            Assert.False(a.Equals(b));

            b = new OperationRequest(
                null,
                "abc",
                document.Object,
                new Dictionary<string, object?> { { "a", new List<object?> { 1, 2, 3, 4 } } });

            // act
            // assert
            Assert.False(a.Equals(b));
        }

        [Fact]
        public void Equals_No_Variables()
        {
            // arrange
            var document = new Mock<IDocument>();

            var a = new OperationRequest(
                null,
                "abc",
                document.Object);

            var b = new OperationRequest(
                null,
                "abc",
                document.Object);

            // act
            // assert
            Assert.True(a.Equals(b));
        }

        [Fact]
        public void Equals_With_ContentHeaders()
        {
            // arrange
            var document = new Mock<IDocument>();

            var a = new OperationRequest(
                null,
                "abc",
                document.Object,
                null,
                RequestStrategy.Default,
                new Dictionary<string, string> { { "a", "aa" } });

            var b = new OperationRequest(
                null,
                "abc",
                document.Object,
                null,
                RequestStrategy.Default,
                new Dictionary<string, string> { { "a", "aa" } });

            var c = new OperationRequest(
                null,
                "abc",
                document.Object,
                null,
                RequestStrategy.Default,
                new Dictionary<string, string> { { "c", "cc" } });

            // act
            // assert
            Assert.True(a.Equals(b));

            // act
            // assert
            Assert.False(a.Equals(c));
        }

        [Fact]
        public void Equals_With_RequestHeaders()
        {
            // arrange
            var document = new Mock<IDocument>();

            var a = new OperationRequest(
                null,
                "abc",
                document.Object,
                null,
                RequestStrategy.Default,
                null,
                new Dictionary<string, string> { { "a", "aa" } });

            var b = new OperationRequest(
                null,
                "abc",
                document.Object,
                null,
                RequestStrategy.Default,
                null,
                new Dictionary<string, string> { { "a", "aa" } });

            var c = new OperationRequest(
                null,
                "abc",
                document.Object,
                null,
                RequestStrategy.Default,
                null,
                new Dictionary<string, string> { { "c", "cc" } });

            // act
            // assert
            Assert.True(a.Equals(b));

            // act
            // assert
            Assert.False(a.Equals(c));
        }

        [Fact]
        public void Equals_With_AuthenticationHeaderValue()
        {
            // arrange
            var document = new Mock<IDocument>();

            var a = new OperationRequest(
                null,
                "abc",
                document.Object,
                null,
                RequestStrategy.Default,
                null,
                null,
                new AuthenticationHeaderValue("Bearer", "myToken"));

            var b = new OperationRequest(
                null,
                "abc",
                document.Object,
                null,
                RequestStrategy.Default,
                null,
                null,
                new AuthenticationHeaderValue("Bearer", "myToken"));

            var c = new OperationRequest(
                null,
                "abc",
                document.Object,
                null,
                RequestStrategy.Default,
                null,
                null,
                new AuthenticationHeaderValue("Bearer", "myToken2"));

            // act
            // assert
            Assert.True(a.Equals(b));

            // act
            // assert
            Assert.False(a.Equals(c));
        }

        [Fact]
        public void GetHashCode_With_Variables()
        {
            // arrange
            var document = new Mock<IDocument>();

            var a = new OperationRequest(
                null,
                "abc",
                document.Object,
                new Dictionary<string, object?>{ { "a", "a" } });

            var b = new OperationRequest(
                null,
                "abc",
                document.Object,
                new Dictionary<string, object?>{ { "a", "a" } });

            // act
            var hashCodeA = a.GetHashCode();
            var hashCodeB = b.GetHashCode();

            // assert
            Assert.Equal(hashCodeA, hashCodeB);
        }

        [Fact]
        public void GetHashCode_No_Variables()
        {
            // arrange
            var document = new Mock<IDocument>();

            var a = new OperationRequest(
                null,
                "abc",
                document.Object);

            var b = new OperationRequest(
                null,
                "abc",
                document.Object);

            // act
            var hashCodeA = a.GetHashCode();
            var hashCodeB = b.GetHashCode();

            // assert
            Assert.Equal(hashCodeA, hashCodeB);
        }

        [Fact]
        public void GetHashCode_With_ContentHeaders()
        {
            // arrange
            var document = new Mock<IDocument>();

            var a = new OperationRequest(
                null,
                "abc",
                document.Object,
                null,
                RequestStrategy.Default,
                new Dictionary<string, string> { { "a", "aa" } });

            var b = new OperationRequest(
                null,
                "abc",
                document.Object,
                null,
                RequestStrategy.Default,
                new Dictionary<string, string> { { "a", "aa" } });

            // act
            var hashCodeA = a.GetHashCode();
            var hashCodeB = b.GetHashCode();

            // assert
            Assert.Equal(hashCodeA, hashCodeB);
        }

        [Fact]
        public void GetHashCode_With_RequestHeaders()
        {
            // arrange
            var document = new Mock<IDocument>();

            var a = new OperationRequest(
                null,
                "abc",
                document.Object,
                null,
                RequestStrategy.Default,
                null,
                new Dictionary<string, string> { { "a", "aa" } });

            var b = new OperationRequest(
                null,
                "abc",
                document.Object,
                null,
                RequestStrategy.Default,
                null,
                new Dictionary<string, string> { { "a", "aa" } });

            // act
            var hashCodeA = a.GetHashCode();
            var hashCodeB = b.GetHashCode();

            // assert
            Assert.Equal(hashCodeA, hashCodeB);
        }

        [Fact]
        public void GetHashCode_With_AuthenticationHeaderValue()
        {
            // arrange
            var document = new Mock<IDocument>();

            var a = new OperationRequest(
                null,
                "abc",
                document.Object,
                null,
                RequestStrategy.Default,
                null,
                null,
                new AuthenticationHeaderValue("Bearer", "myToken"));

            var b = new OperationRequest(
                null,
                "abc",
                document.Object,
                null,
                RequestStrategy.Default,
                null,
                null,
                new AuthenticationHeaderValue("Bearer", "myToken"));

            // act
            var hashCodeA = a.GetHashCode();
            var hashCodeB = b.GetHashCode();

            // assert
            Assert.Equal(hashCodeA, hashCodeB);
        }

        [Fact]
        public void Deconstruct()
        {
            // arrange
            var document = new Mock<IDocument>();

            var request = new OperationRequest(
                null,
                "abc",
                document.Object);

            // act
            string? id;
            string name;
            IDocument doc;
            IReadOnlyDictionary<string, object?> vars;
            IReadOnlyDictionary<string, object?>? ext;
            IReadOnlyDictionary<string, object?>? contextData;
            RequestStrategy strategy;
            IDictionary<string, string> contentHeaders;
            IDictionary<string, string> requestHeaders;
            AuthenticationHeaderValue? authenticationHeaderValue;
            (id, name, doc, vars, ext, contextData, strategy, contentHeaders, requestHeaders, authenticationHeaderValue) = request;

            // assert
            Assert.Equal(request.Id, id);
            Assert.Equal(request.Name, name);
            Assert.Equal(request.Document, doc);
            Assert.Equal(request.Variables, vars);
            Assert.Null(ext);
            Assert.Null(contextData);
            Assert.Equal(request.Strategy, strategy);
            Assert.Equal(request.ContentHeaders, contentHeaders);
            Assert.Equal(request.RequestHeaders, requestHeaders);
            Assert.Equal(request.AuthenticationHeaderValue, authenticationHeaderValue);
        }
    }
}
