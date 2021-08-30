using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.TestHost;
using HotChocolate.AspNetCore.Utilities;
using Newtonsoft.Json;
using Snapshooter.Xunit;
using Xunit;
using System.IO;

namespace HotChocolate.AspNetCore
{
    public class HttpMultipartMiddlewareTests : ServerTestBase
    {
        public HttpMultipartMiddlewareTests(TestServerFactory serverFactory)
            : base(serverFactory)
        {
        }

        [Fact]
        public async Task EmptyForm_Test()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            var form = new MultipartFormDataContent();

            ClientQueryResult result = await server.PostMultipartAsync(form);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task EmptyOperations_Test()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            var form = new MultipartFormDataContent
            {
                { new StringContent(""), "operations" },
            };

            ClientQueryResult result = await server.PostMultipartAsync(form);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task IncompleteOperations_Test()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            var form = new MultipartFormDataContent
            {
                { new StringContent("{}"), "operations" },
                { new StringContent("{}"), "map" },
            };

            ClientQueryResult result = await server.PostMultipartAsync(form);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task MapWithNoOperations_Test()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            var form = new MultipartFormDataContent
            {
                { new StringContent("{}"), "map" },
            };

            ClientQueryResult result = await server.PostMultipartAsync(form);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task MapBeforeOperations_Test()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            var form = new MultipartFormDataContent
            {
                { new StringContent("{}"), "map" },
                { new StringContent("{}"), "operations" },
            };

            ClientQueryResult result = await server.PostMultipartAsync(form);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task EmptyMap_Test()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            var form = new MultipartFormDataContent
            {
                { new StringContent("{}"), "operations" },
                { new StringContent(""), "map" },
            };

            ClientQueryResult result = await server.PostMultipartAsync(form);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task InvalidMap_Test()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            var form = new MultipartFormDataContent
            {
                { new StringContent("{}"), "operations" },
                { new StringContent("data"), "map" },
            };

            ClientQueryResult result = await server.PostMultipartAsync(form);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task MissingFile_Test()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            var form = new MultipartFormDataContent
            {
                { new StringContent("{}"), "operations" },
                { new StringContent("{ \"1\": [\"variables.file\"] }"), "map" },
            };

            ClientQueryResult result = await server.PostMultipartAsync(form);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task MissingKeyInMap_Test()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            var form = new MultipartFormDataContent
            {
                { new StringContent("{}"), "operations" },
                { new StringContent("{ \"\": [\"variables.file\"] }"), "map" },
            };

            ClientQueryResult result = await server.PostMultipartAsync(form);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task MissingObjectPathsForKey_Test()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            // act
            var form = new MultipartFormDataContent
            {
                { new StringContent("{}"), "operations" },
                { new StringContent("{ \"1\": [] }"), "map" },
            };

            ClientQueryResult result = await server.PostMultipartAsync(form);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task Upload_File()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            var query = @"
                query ($upload: Upload!) {
                    singleUpload(file: $upload)
                }";

            var request = JsonConvert.SerializeObject(
                new ClientQueryRequest
                {
                    Query = query,
                    Variables = new Dictionary<string, object>
                    {
                        { "upload", null }
                    }
                });

            // act
            var form = new MultipartFormDataContent
            {
                { new StringContent(request), "operations" },
                { new StringContent("{ \"1\": [\"variables.upload\"] }"), "map" },
                { new StringContent("abc"), "1", "foo.bar" },
            };

            ClientQueryResult result = await server.PostMultipartAsync(form, path: "/upload");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task Upload_Optional_File()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            var query = @"
                query ($upload: Upload) {
                    optionalUpload(file: $upload)
                }";

            var request = JsonConvert.SerializeObject(
                new ClientQueryRequest
                {
                    Query = query,
                    Variables = new Dictionary<string, object>
                    {
                        { "upload", null }
                    }
                });

            // act
            var form = new MultipartFormDataContent
            {
                { new StringContent(request), "operations" },
                { new StringContent("{ \"1\": [\"variables.upload\"] }"), "map" },
                { new StringContent("abc"), "1", "foo.bar" },
            };

            ClientQueryResult result = await server.PostMultipartAsync(form, path: "/upload");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task Upload_Optional_File_In_InputObject()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            var query = @"
                query ($input: InputWithOptionalFileInput!) {
                    optionalObjectUpload(input: $input)
                }";

            var request = JsonConvert.SerializeObject(
                new ClientQueryRequest
                {
                    Query = query,
                    Variables = new Dictionary<string, object>
                    {
                        { "input", new Dictionary<string, object> { { "file", null } } }
                    }
                });

            // act
            var form = new MultipartFormDataContent
            {
                { new StringContent(request), "operations" },
                { new StringContent("{ \"1\": [\"variables.input.file\"] }"), "map" },
                { new StringContent("abc"), "1", "foo.bar" },
            };

            ClientQueryResult result = await server.PostMultipartAsync(form, path: "/upload");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task Upload_Optional_File_In_Inline_InputObject()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            var query = @"
                query ($upload: Upload!) {
                    optionalObjectUpload(input: { file: $upload })
                }";

            var request = JsonConvert.SerializeObject(
                new ClientQueryRequest
                {
                    Query = query,
                    Variables = new Dictionary<string, object>
                    {
                        { "upload", null }
                    }
                });

            // act
            var form = new MultipartFormDataContent
            {
                { new StringContent(request), "operations" },
                { new StringContent("{ \"1\": [\"variables.upload\"] }"), "map" },
                { new StringContent("abc"), "1", "foo.bar" },
            };

            ClientQueryResult result = await server.PostMultipartAsync(form, path: "/upload");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task Upload_File_In_InputObject()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            var query = @"
                query ($input: InputWithFileInput!) {
                    objectUpload(input: $input)
                }";

            var request = JsonConvert.SerializeObject(
                new ClientQueryRequest
                {
                    Query = query,
                    Variables = new Dictionary<string, object>
                    {
                        { "input", new Dictionary<string, object> { { "file", null } } }
                    }
                });

            // act
            var form = new MultipartFormDataContent
            {
                { new StringContent(request), "operations" },
                { new StringContent("{ \"1\": [\"variables.input.file\"] }"), "map" },
                { new StringContent("abc"), "1", "foo.bar" },
            };

            ClientQueryResult result = await server.PostMultipartAsync(form, path: "/upload");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task Upload_File_Inline_InputObject()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            var query = @"
                query ($upload: Upload!) {
                    objectUpload(input: { file: $upload })
                }";

            var request = JsonConvert.SerializeObject(
                new ClientQueryRequest
                {
                    Query = query,
                    Variables = new Dictionary<string, object>
                    {
                        { "upload", null }
                    }
                });

            // act
            var form = new MultipartFormDataContent
            {
                { new StringContent(request), "operations" },
                { new StringContent("{ \"1\": [\"variables.upload\"] }"), "map" },
                { new StringContent("abc"), "1", "foo.bar" },
            };

            ClientQueryResult result = await server.PostMultipartAsync(form, path: "/upload");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task Upload_File_In_List()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            var query = @"
                query ($input: [[InputWithFileInput!]]) {
                    listUpload(input: $input)
                }";

            var request = JsonConvert.SerializeObject(
                new ClientQueryRequest
                {
                    Query = query,
                    Variables = new Dictionary<string, object>
                    {
                        {
                            "input",
                            new List<object>
                            {
                                new List<object>
                                {
                                    new Dictionary<string, object> { { "file", null } }
                                }
                            }
                        }
                    }
                });

            // act
            var form = new MultipartFormDataContent
            {
                { new StringContent(request), "operations" },
                { new StringContent("{ \"1\": [\"variables.input.0.0.file\"] }"), "map" },
                { new StringContent("abc"), "1", "foo.bar" },
            };

            ClientQueryResult result = await server.PostMultipartAsync(form, path: "/upload");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task Upload_Too_Large_File_Test()
        {
            // arrange
            TestServer server = CreateStarWarsServer();

            var query = @"
                query ($upload: Upload!) {
                    singleUpload(file: $upload)
                }";

            var request = JsonConvert.SerializeObject(
                new ClientQueryRequest
                {
                    Query = query,
                    Variables = new Dictionary<string, object>
                    {
                        { "upload", null }
                    }
                });

            var count = 1024 * 1024 * 129;
            var buffer = new byte[count];

            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = 0xFF;
            }

            // act
            var form = new MultipartFormDataContent
            {
                { new StringContent(request), "operations" },
                { new StringContent("{ \"1\": [\"variables.upload\"] }"), "map" },
                { new ByteArrayContent(buffer), "1", "foo.bar" },
            };

            ClientQueryResult result = await server.PostMultipartAsync(form, path: "/upload");

            // assert
            result.MatchSnapshot();
        }
    }
}
