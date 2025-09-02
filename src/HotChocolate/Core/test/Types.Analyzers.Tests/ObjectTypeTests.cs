namespace HotChocolate.Types;

public class ObjectTypeTests
{
    [Fact]
    public async Task GenerateSource_BatchDataLoader_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using HotChocolate.Types;

            namespace TestNamespace;

            public sealed class Author
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }

            public sealed class Book
            {
                public int Id { get; set; }
                public string Title { get; set; }
                public int AuthorId { get; set; }
            }

            [ObjectType<Book>]
            internal static partial class BookNode
            {
                [BindMember(nameof(Book.AuthorId))]
                public static Task<Author?> GetAuthorAsync(
                    [Parent] Book book,
                    CancellationToken cancellationToken)
                    => default;
            }
            """).MatchMarkdownAsync();
    }

    [Fact]
    public async Task GenerateSource_BindField_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using HotChocolate.Types;

            namespace TestNamespace;

            public sealed class LineItem
            {
                public int Id { get; set; }
                public int ProductId { get; set; }
                public int Quantity { get; set; }
            }

            public sealed class Product
            {
                public int Id { get; set; }
                public string Name { get; set; } = string.Empty;
            }

            [ObjectType<LineItem>]
            public static partial class LineItemType
            {
                [BindField("product")]
                public static Product GetProduct([Parent] LineItem lineItem)
                    => new Product { Id = lineItem.ProductId, Name = "Test Product" };
            }
            """).MatchMarkdownAsync();
    }

    [Fact]
    public async Task GenerateSource_BindField_And_BindMember_MatchesSnapshot()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using HotChocolate.Types;

            namespace TestNamespace;

            public sealed class User
            {
                public int Id { get; set; }
                public string Email { get; set; } = string.Empty;
                public int ProfileId { get; set; }
            }

            public sealed class Profile
            {
                public int Id { get; set; }
                public string DisplayName { get; set; } = string.Empty;
            }

            [ObjectType<User>]
            public static partial class UserType
            {
                [BindField("profile")]
                public static Profile GetProfile([Parent] User user)
                    => new Profile { Id = user.ProfileId, DisplayName = "Profile" };

                [BindMember(nameof(User.Email))]
                public static string GetEmailFormatted([Parent] User user)
                    => $"Email: {user.Email}";
            }
            """).MatchMarkdownAsync();
    }
}
