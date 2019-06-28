using System;
using System.Threading.Tasks;
using HotChocolate.PersistedQueries.FileSystem;
using Moq;
using Xunit;

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
        public async Task When_ReadQueryAsync_Is_Called_If_File_Path_Is_Null_Or_White_Space_Then_ArgumentNullException_Is_Thrown(string filePath)
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _systemUnderTest.ReadQueryAsync(filePath)
            );
        }
        
        
    }
}
