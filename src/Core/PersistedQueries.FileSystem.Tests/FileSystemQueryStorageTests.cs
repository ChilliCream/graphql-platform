using System;
using System.IO;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;
using IOPath = System.IO.Path;

namespace HotChocolate.PersistedQueries.FileSystem
{
    public class FileSystemQueryStorageTests
    {
        [Fact]
        public async Task Write_Query_To_Storage()
        {
            string path = null;

            try
            {
                // arrange
                path = IOPath.Combine(
                    IOPath.GetTempPath(),
                    "d_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(path);

                var storage = new FileSystemQueryStorage(
                    new DefaultQueryFileMap(path));

                var query = new QuerySourceText("{ foo }");
                var queryId = "1234";

                // act
                await storage.WriteQueryAsync(queryId, query);

                // assert
                Assert.True(File.Exists(IOPath.Combine(path, "1234")));
                QuerySyntaxSerializer.Serialize(
                    Utf8GraphQLParser.Parse(
                        File.ReadAllBytes(IOPath.Combine(path, "1234"))))
                    .MatchSnapshot();
            }
            finally
            {
                if (path != null)
                {
                    Directory.Delete(path, true);
                }
            }
        }

        [InlineData(null)]
        [InlineData("")]
        [Theory]
        public async Task Write_Query_QueryId_Invalid(string queryId)
        {
            string path = null;

            try
            {
                // arrange
                path = IOPath.Combine(
                    IOPath.GetTempPath(),
                    "d_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(path);

                var storage = new FileSystemQueryStorage(
                    new DefaultQueryFileMap(path));

                var query = new QuerySourceText("{ foo }");

                // act
                Func<Task> action =
                    () => storage.WriteQueryAsync(queryId, query);

                // assert
                await Assert.ThrowsAsync<ArgumentNullException>(action);
            }
            finally
            {
                if (path != null)
                {
                    Directory.Delete(path, true);
                }
            }
        }

        [Fact]
        public async Task Write_Query_Query_Is_Null()
        {
            string path = null;

            try
            {
                // arrange
                path = IOPath.Combine(
                    IOPath.GetTempPath(),
                    "d_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(path);

                var storage = new FileSystemQueryStorage(
                    new DefaultQueryFileMap(path));

                // act
                Func<Task> action =
                    () => storage.WriteQueryAsync("1234", null);

                // assert
                await Assert.ThrowsAsync<ArgumentNullException>(action);
            }
            finally
            {
                if (path != null)
                {
                    Directory.Delete(path, true);
                }
            }
        }

        [Fact]
        public async Task Read_Query_From_Storage()
        {
            string path = null;

            try
            {
                // arrange
                path = IOPath.Combine(
                    IOPath.GetTempPath(),
                    "d_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(path);

                var storage = new FileSystemQueryStorage(
                    new DefaultQueryFileMap(path));

                var queryId = "1234";
                await File.WriteAllTextAsync(
                    IOPath.Combine(path, queryId),
                    "{ foo }");

                // act
                QueryDocument query = await storage.TryReadQueryAsync(queryId);

                // assert
                Assert.NotNull(query);
                QuerySyntaxSerializer.Serialize(
                    query.Document)
                    .MatchSnapshot();
            }
            finally
            {
                if (path != null)
                {
                    Directory.Delete(path, true);
                }
            }
        }

        [InlineData(null)]
        [InlineData("")]
        [Theory]
        public async Task Read_Query_QueryId_Invalid(string queryId)
        {
            string path = null;

            try
            {
                // arrange
                path = IOPath.Combine(
                    IOPath.GetTempPath(),
                    "d_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(path);

                var storage = new FileSystemQueryStorage(
                    new DefaultQueryFileMap(path));

                // act
                Func<Task> action = () => storage.TryReadQueryAsync(queryId);

                // assert
                await Assert.ThrowsAsync<ArgumentNullException>(action);
            }
            finally
            {
                if (path != null)
                {
                    Directory.Delete(path, true);
                }
            }
        }
    }
}
