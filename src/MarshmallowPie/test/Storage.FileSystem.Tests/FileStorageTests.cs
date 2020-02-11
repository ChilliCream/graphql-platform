using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using IOFile = System.IO.File;

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
        public async Task Storage_Create_Container()
        {
            // arrange
            var fileStorage = new FileStorage(_path);

            // act
            IFileContainer container = await fileStorage.CreateContainerAsync("abc");

            // assert
            Assert.Equal("abc", container.Name);
            Assert.True(Directory.Exists(Path.Combine(_path, "abc")));
        }

        [Fact]
        public async Task Storage_Create_Container_Directory_Already_Exists()
        {
            // arrange
            var fileStorage = new FileStorage(_path);
            Directory.CreateDirectory(Path.Combine(_path, "abc"));

            // act
            Func<Task> action = () => fileStorage.CreateContainerAsync("abc");

            // assert
            await Assert.ThrowsAsync<ArgumentException>(action);
        }

        [Fact]
        public async Task Storage_Get_Container()
        {
            // arrange
            var fileStorage = new FileStorage(_path);
            Directory.CreateDirectory(Path.Combine(_path, "abc"));

            // act
            IFileContainer container = await fileStorage.GetContainerAsync("abc");

            // assert
            Assert.Equal("abc", container.Name);
        }

        [Fact]
        public async Task Storage_Get_Container_Directory_Does_Not_Exists()
        {
            // arrange
            var fileStorage = new FileStorage(_path);

            // act
            Func<Task> func = () => fileStorage.GetContainerAsync("abc");

            // assert
            await Assert.ThrowsAsync<DirectoryNotFoundException>(func);
        }

        [Fact]
        public async Task Container_Delete_Container()
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
        public async Task Container_Delete_Container_Directory_Does_Not_Exists()
        {
            // arrange
            var fileStorage = new FileStorage(_path);
            IFileContainer container = await fileStorage.CreateContainerAsync("abc");
            Directory.Delete(Path.Combine(_path, "abc"));

            // act
            Func<Task> action = () => container.DeleteAsync();

            // assert
            await Assert.ThrowsAsync<DirectoryNotFoundException>(action);
        }

        [Fact]
        public async Task Container_Create_File()
        {
            // arrange
            var fileStorage = new FileStorage(_path);
            IFileContainer container = await fileStorage.CreateContainerAsync("abc");

            // act
            await container.CreateFileAsync("def", new byte[] { 0 }, 0, 1);

            // assert
            Assert.True(IOFile.Exists(Path.Combine(_path, "abc", "def")));
        }

        [Fact]
        public async Task Container_Create_File_Directory_Does_Not_Exist()
        {
            // arrange
            var fileStorage = new FileStorage(_path);
            IFileContainer container = await fileStorage.CreateContainerAsync("abc");
            Directory.Delete(Path.Combine(_path, "abc"));

            // act
            Func<Task> action = () => container.CreateFileAsync(
                "def", Array.Empty<byte>(), 0, 0);

            // assert
            await Assert.ThrowsAsync<DirectoryNotFoundException>(action);
        }

        [Fact]
        public async Task Container_Create_File_File_Already_Exist()
        {
            // arrange
            var fileStorage = new FileStorage(_path);
            IFileContainer container = await fileStorage.CreateContainerAsync("abc");
            IOFile.WriteAllText(Path.Combine(_path, "abc", "def"), "ghi");

            // act
            Func<Task> action = () => container.CreateFileAsync(
                "def", Array.Empty<byte>(), 0, 0);

            // assert
            await Assert.ThrowsAsync<ArgumentException>(action);
        }

        [Fact]
        public async Task Container_Get_Files()
        {
            // arrange
            Directory.CreateDirectory(Path.Combine(_path, "abc"));
            IOFile.WriteAllText(Path.Combine(_path, "abc", "def"), "ghi");

            var fileStorage = new FileStorage(_path);
            IFileContainer container = await fileStorage.GetContainerAsync("abc");

            // act
            IEnumerable<IFile> files = await container.GetFilesAsync();

            // assert
            Assert.Collection(files,
                t => Assert.Equal("def", t.Name));
        }

        [Fact]
        public async Task Container_Get_Files_Container_Does_Not_Exists()
        {
            // arrange
            var fileStorage = new FileStorage(_path);
            IFileContainer container = await fileStorage.CreateContainerAsync("abc");
            Directory.Delete(Path.Combine(_path, "abc"));

            // act
            Func<Task> action = () => container.GetFilesAsync();

            // assert
            await Assert.ThrowsAsync<DirectoryNotFoundException>(action);
        }

        [Fact]
        public async Task File_Open_File()
        {
            // arrange
            Directory.CreateDirectory(Path.Combine(_path, "abc"));
            IOFile.WriteAllText(Path.Combine(_path, "abc", "def"), "ghi");

            var fileStorage = new FileStorage(_path);
            IFileContainer container = await fileStorage.GetContainerAsync("abc");
            IEnumerable<IFile> files = await container.GetFilesAsync();
            IFile file = files.Single();

            // act
            using Stream stream = await file.OpenAsync();
            using StreamReader sr = new StreamReader(stream);
            string content = sr.ReadToEnd();

            // assert
            Assert.Equal("ghi", content);
        }

        [Fact]
        public async Task File_Open_File_Does_Not_Exist()
        {
            // arrange
            Directory.CreateDirectory(Path.Combine(_path, "abc"));
            IOFile.WriteAllText(Path.Combine(_path, "abc", "def"), "ghi");

            var fileStorage = new FileStorage(_path);
            IFileContainer container = await fileStorage.GetContainerAsync("abc");
            IEnumerable<IFile> files = await container.GetFilesAsync();
            IFile file = files.Single();
            IOFile.Delete(Path.Combine(_path, "abc", "def"));

            // act
            Func<Task> action = () => file.OpenAsync();

            // assert
            await Assert.ThrowsAsync<FileNotFoundException>(action);
        }

        [Fact]
        public async Task File_Delete_File()
        {
            // arrange
            Directory.CreateDirectory(Path.Combine(_path, "abc"));
            IOFile.WriteAllText(Path.Combine(_path, "abc", "def"), "ghi");

            var fileStorage = new FileStorage(_path);
            IFileContainer container = await fileStorage.GetContainerAsync("abc");
            IEnumerable<IFile> files = await container.GetFilesAsync();
            IFile file = files.Single();

            // act
            await file.DeleteAsync();

            // assert
            Assert.False(IOFile.Exists(Path.Combine(_path, "abc", "def")));
        }

        [Fact]
        public async Task File_Delete_File_Does_Not_Exist()
        {
            // arrange
            Directory.CreateDirectory(Path.Combine(_path, "abc"));
            IOFile.WriteAllText(Path.Combine(_path, "abc", "def"), "ghi");

            var fileStorage = new FileStorage(_path);
            IFileContainer container = await fileStorage.GetContainerAsync("abc");
            IEnumerable<IFile> files = await container.GetFilesAsync();
            IFile file = files.Single();
            IOFile.Delete(Path.Combine(_path, "abc", "def"));

            // act
            Func<Task> action = () => file.DeleteAsync();

            // assert
            await Assert.ThrowsAsync<FileNotFoundException>(action);
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
