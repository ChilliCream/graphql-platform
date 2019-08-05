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
    }
}
