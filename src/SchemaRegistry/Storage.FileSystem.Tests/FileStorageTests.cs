using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace MarshmallowPie.Storage.FileSystem
{
    public class FileStorageTests
        : IDisposable
    {
        private string _path;

        public FileStorageTests()
        {
            _path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_path);
        }

        [Fact]
        public void Create_Storage_With_Invalid_Path()
        {
            // arrange
            // act
            Action action = () => new FileStorage(Guid.NewGuid().ToString("N"));

            // assert
            Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public async Task Create_Container()
        {
            // arrange
            var fileStorage = new FileStorage(_path);

            // act
            IFileContainer container = await fileStorage.CreateContainerAsync("abc");

            // assert
            Assert.True(Directory.Exists(Path.Combine(_path, "abc")));
        }

        [Fact]
        public async Task Delete_Container()
        {
            // arrange
            var fileStorage = new FileStorage(_path);
            IFileContainer container = await fileStorage.CreateContainerAsync("abc");

            // act
            await container.DeleteAsync();

            // assert
            Assert.False(Directory.Exists(Path.Combine(_path, "abc")));
        }

        [Fact]
        public async Task Create_File()
        {
            // arrange
            var fileStorage = new FileStorage(_path);
            IFileContainer container = await fileStorage.CreateContainerAsync("abc");

            // act

            // assert
            Assert.True(Directory.Exists(Path.Combine(_path, "abc")));
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(_path, true);
            }
            catch { }
        }
    }
}
