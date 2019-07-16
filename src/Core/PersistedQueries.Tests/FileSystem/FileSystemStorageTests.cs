using System;
using System.IO;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.PersistedQueries.FileSystem;
using Moq;
using Xunit;
using SysPath = System.IO.Path;

namespace HotChocolate.PersistedQueries.Tests.FileSystem
{
    public class FileSystemStorageTests
    {
        private readonly Mock<IQueryFileMap> _queryFileMapMock;
        private readonly Mock<IQueryRequestBuilder> _queryRequestBuilderMock;
        private readonly Mock<IReadOnlyQueryRequest> _queryRequestMock;
        private readonly FileSystemStorage _systemUnderTest;

        public FileSystemStorageTests()
        {
            _queryFileMapMock = new Mock<IQueryFileMap>();

            _queryRequestMock = new Mock<IReadOnlyQueryRequest>();

            _queryRequestBuilderMock = new Mock<IQueryRequestBuilder>();
            _queryRequestBuilderMock.Setup(q => q.SetQuery(It.IsAny<string>()))
                .Returns(_queryRequestBuilderMock.Object);
            _queryRequestBuilderMock.Setup(q => q.Create())
                .Returns(_queryRequestMock.Object);

            _systemUnderTest = new FileSystemStorage(
                _queryFileMapMock.Object,
                _queryRequestBuilderMock.Object
            );
        }

        [Fact]
        public void When_FileSystemStorage_Is_Constructed_If_Query_File_Map_Is_Null_Then_Argument_Null_Exception_Is_Thrown()
        {
            Assert.Throws<ArgumentNullException>(
                () => new FileSystemStorage(null, _queryRequestBuilderMock.Object)
            );
        }

        [Fact]
        public void When_FileSystemStorage_Is_Constructed_If_Query_Request_Builder_Is_Null_Then_Argument_Null_Exception_Is_Thrown()
        {
            Assert.Throws<ArgumentNullException>(
                () => new FileSystemStorage(_queryFileMapMock.Object, null)
            );
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t\n ")]
        public async Task When_ReadQueryAsync_Is_Called_If_Query_Id_Is_Null_Or_White_Space_Then_ArgumentNullException_Is_Thrown(string queryId)
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _systemUnderTest.ReadQueryAsync(queryId)
            );
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t\n ")]
        public async Task When_ReadQueryAsync_Is_Called_If_File_Path_For_Query_Id_Is_Null_Or_White_Space_Then_QueryNotFoundException_Is_Thrown(string filePath)
        {
            // Arrange
            const string queryId = "Some silly query Id";

            _queryFileMapMock.Setup(f => f.MapToFilePath(It.IsAny<string>()))
                .Returns(filePath);

            // Act
            var exception = await Assert.ThrowsAsync<QueryNotFoundException>(() => _systemUnderTest.ReadQueryAsync(queryId));

            // Assert
            Assert.Equal($"Unable to find query with identifier '{queryId}'.", exception.Message);

            _queryFileMapMock.Verify(f => f.MapToFilePath(queryId));
        }

        [Fact]
        public async Task When_ReadQueryAsync_Is_Called_If_File_Path_Does_Not_Exist_Then_QueryNotFound_Exception_Is_Thrown()
        {
            // Arrange
            var invalidFilePath = SysPath.Combine(Directory.GetCurrentDirectory(), "Bogus-Query.graphql");

            _queryFileMapMock.Setup(f => f.MapToFilePath(It.IsAny<string>()))
                .Returns(invalidFilePath);

            // Act
            await Assert.ThrowsAsync<QueryNotFoundException>(() => _systemUnderTest.ReadQueryAsync("query"));
        }

        [Fact]
        public async Task When_ReadQueryAsync_Is_Called_Then_Query_Builder_Source_Is_Set_From_File_Content()
        {
            // Arrange
            const string queryFileRelativePath = "FileSystem/StoredQueries/SimpleQuery.graphql";
            var simpleQueryFile = SysPath.Combine(Directory.GetCurrentDirectory(), queryFileRelativePath);

            _queryFileMapMock.Setup(f => f.MapToFilePath(It.IsAny<string>()))
                .Returns(simpleQueryFile);

            // Act
            await _systemUnderTest.ReadQueryAsync("query");

            // Assert
            using (var fileStream = new FileStream(simpleQueryFile, FileMode.Open))
            {
                using (var reader = new StreamReader(fileStream))
                {
                    var expectedContent = await reader.ReadToEndAsync();

                    _queryRequestBuilderMock.Verify(q => q.SetQuery(expectedContent));
                }
            }
        }

        [Fact]
        public async Task When_ReadQueryAsync_Is_Called_Then_Create_Response_Is_Returned()
        {
            // Arrange
            const string queryFileRelativePath = "FileSystem/StoredQueries/SimpleQuery.graphql";
            var simpleQueryFile = SysPath.Combine(Directory.GetCurrentDirectory(), queryFileRelativePath);

            _queryFileMapMock.Setup(f => f.MapToFilePath(It.IsAny<string>()))
                .Returns(simpleQueryFile);

            var mockQuery = new Mock<IQuery>();
            _queryRequestMock.Setup(q => q.Query).Returns(mockQuery.Object);

            // Act
            var query = await _systemUnderTest.ReadQueryAsync("query");

            // Assert
            Assert.NotNull(query);
            Assert.Same(mockQuery.Object, query);
        }
    }
}
