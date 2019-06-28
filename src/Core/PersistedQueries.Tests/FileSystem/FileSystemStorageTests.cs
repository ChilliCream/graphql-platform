using System;
using System.IO;
using System.Threading.Tasks;
using HotChocolate.PersistedQueries.FileSystem;
using Moq;
using Xunit;
using SysPath = System.IO.Path;

namespace HotChocolate.PersistedQueries.Tests.FileSystem
{
    public class FileSystemStorageTests
    {
        private readonly Mock<IQueryFileMap> _queryFileMapMock;
        private readonly FileSystemStorage _systemUnderTest;

        public FileSystemStorageTests()
        {
            _queryFileMapMock = new Mock<IQueryFileMap>();
            _systemUnderTest = new FileSystemStorage(
                _queryFileMapMock.Object
            );
        }

        [Fact]
        public void When_FileSystemStorage_Is_Constructed_If_Query_File_Map_Is_Null_Then_Argument_Null_Exception_Is_Thrown()
        {
            Assert.Throws<ArgumentNullException>(
                () => new FileSystemStorage(null)
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
        public async Task When_ReadQueryAsync_Is_Called_Then_TODO()
        {
            // Arrange
            var simpleQueryFile = SysPath.Combine(Directory.GetCurrentDirectory(), "FileSystem/StoredQueries/SimpleQuery.graphql");

            _queryFileMapMock.Setup(f => f.MapToFilePath(It.IsAny<string>()))
                .Returns(simpleQueryFile);

            // Act
            var exception = await Assert.ThrowsAsync<NotImplementedException>(() => _systemUnderTest.ReadQueryAsync("query"));

            // Assert
            Assert.Equal("Waiting for IQuery things", exception.Message);
            Assert.Equal(@"query {
    foo
}", exception.Data["FilePath"]);
        }
    }
}
