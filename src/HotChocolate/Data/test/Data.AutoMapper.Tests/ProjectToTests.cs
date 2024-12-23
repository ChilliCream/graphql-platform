using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AutoMapper;
using HotChocolate.Execution;
using HotChocolate.Resolvers;

namespace HotChocolate.Data.Projections;

public class ProjectToTests
{
    private static readonly Blog[] _blogEntries =
    [
        new()
        {
            Name = "TestA",
            Url = "testa.com",
            Author =
                new Author
                {
                    Name = "Phil",
                    Membership = new PremiumMember { Name = "foo", Premium = "A", },
                },
            TitleImage = new Image { Url = "https://testa.com/image.png", },
            Posts = new[]
            {
                new Post
                {
                    Title = "titleA",
                    Content = "contentA",
                    Author = new Author
                    {
                        Name = "Anna",
                        Membership =
                            new StandardMember { Name = "foo", Standard = "FLAT", },
                    },
                },
                new Post
                {
                    Title = "titleB",
                    Content = "contentB",
                    Author = new Author
                    {
                        Name = "Max",
                        Membership =
                            new StandardMember { Name = "foo", Standard = "FLAT", },
                    },
                },
            },
        },
        new()
        {
            Name = "TestB",
            Url = "testb.com",
            TitleImage = new Image { Url = "https://testb.com/image.png", },
            Author = new Author
            {
                Name = "Kurt",
                Membership =
                    new StandardMember { Name = "foo", Standard = "FLAT", },
            },
            Posts = new[]
            {
                new Post
                {
                    Title = "titleC",
                    Content = "contentC",
                    Author = new Author
                    {
                        Name = "Charles",
                        Membership =
                            new PremiumMember { Name = "foo", Premium = "FLAT", },
                    },
                },
                new Post
                {
                    Title = "titleD",
                    Content = "contentD",
                    Author = new Author
                    {
                        Name = "Simone",
                        Membership =
                            new PremiumMember { Name = "foo", Premium = "FLAT", },
                    },
                },
            },
        },
    ];

    [Fact]
    public async Task Execute_ManyToOne()
    {
        // arrange
        var tester = await CreateSchema();

        // act
        // assert
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    @"
                    {
                      posts {
                        postId
                        title
                        blog {
                          url
                        }
                      }
                    }")
                .Build());

        var snapshot = new Snapshot(postFix: TestEnvironment.TargetFramework);
        snapshot.AddSqlFrom(res1);
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Execute_ManyToOne_Deep()
    {
        // arrange
        var tester = await CreateSchema();

        // act
        // assert
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    @"
                    query Test {
                        posts {
                            postId
                            title
                            blog {
                                url
                                posts {
                                    title
                                    blog {
                                        url
                                        posts {
                                            title
                                        }
                                    }
                                }
                            }
                        }
                    }")
                .Build());

        var snapshot = new Snapshot(postFix: TestEnvironment.TargetFramework);
        snapshot.AddSqlFrom(res1);
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Execute_OneToOne()
    {
        // arrange
        var tester = await CreateSchema();

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    @"
                    {
                      blogs {
                        url
                        titleImage {
                          url
                        }
                      }
                    }")
                .Build());

        // assert
        var snapshot = new Snapshot();
        snapshot.AddSqlFrom(res1);
        await snapshot.MatchAsync();
    }

    [Fact]
    public async Task Execute_OneToOne_Deep()
    {
        // arrange
        var tester = await CreateSchema();

        // act
        // assert
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    @"
                    query Test {
                        posts {
                            postId
                            title
                            blog {
                                url
                                titleImage {
                                    url
                                }
                            }
                        }
                    }")
                .Build());

        var snapshot = new Snapshot(postFix: TestEnvironment.TargetFramework);
        snapshot.AddSqlFrom(res1);
        await snapshot.MatchAsync();
    }

    [Fact(Skip = "Automapper does not understand abstract mappings like we need it to")]
    public async Task Execute_Derived_CompleteSelectionSet()
    {
        // arrange
        var tester = await CreateSchema();

        // act
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    @"
                    query Test {
                        members {
                            name
                            ... on PremiumMemberDto { premium }
                            ... on StandardMemberDto { standard }
                        }
                    }")
                .Build());

        // assert
        var snapshot = new Snapshot();
        snapshot.AddSqlFrom(res1);
        await snapshot.MatchAsync();
    }

    [Fact(Skip = "Automapper does not understand abstract mappings like we need it to")]
    public async Task Execute_Derived_PartialSelectionSet()
    {
        // arrange
        var tester = await CreateSchema();

        // act
        // assert
        var res1 = await tester.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    @"
                    query Test {
                        members {
                            name
                            ... on PremiumMemberDto { premium }
                        }
                    }")
                .Build());

        var snapshot = new Snapshot();
        snapshot.AddSqlFrom(res1);
        await snapshot.MatchAsync();
    }

    public async ValueTask<IRequestExecutor> CreateSchema()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddDbContextPool<BloggingContext>(x
            => x.UseSqlite($"Data Source={Guid.NewGuid():N}.db"));
        var mapperConfig = new MapperConfiguration(mc =>
        {
            mc.AddProfile(new PostProfile());
            mc.AddProfile(new BlogProfile());
            mc.AddProfile(new ImageProfile());
            mc.AddProfile(new MembershipProfile());
            mc.AddProfile(new AuthorProfile());
        });

        var mapper = mapperConfig.CreateMapper();
        services.AddSingleton(sp =>
        {
            // abusing the mapper factory to add to the database. You didnt see this.
            using var scope = sp.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<BloggingContext>();
            context.Database.EnsureCreated();
            context.Blogs.AddRange(_blogEntries);
            context.SaveChanges();

            return mapper;
        });

        return await services
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddInterfaceType<MembershipDto>()
            .AddType<PremiumMemberDto>()
            .AddType<StandardMemberDto>()
            .AddProjections()
            .UseSqlLogging()
            .BuildRequestExecutorAsync();
    }

    public class Query
    {
        [UseSqlLogging]
        [UseProjection]
        public IQueryable<PostDto> GetPosts(BloggingContext dbContext, IResolverContext context)
            => dbContext.Posts.ProjectTo<Post, PostDto>(context);

        [UseSqlLogging]
        [UseProjection]
        public IQueryable<BlogDto> GetBlogs(BloggingContext dbContext, IResolverContext context)
            => dbContext.Blogs.ProjectTo<Blog, BlogDto>(context);

        [UseSqlLogging]
        [UseProjection]
        public IQueryable<AuthorDto> GetAuthors(BloggingContext dbContext, IResolverContext context)
            => dbContext.Authors.ProjectTo<Author, AuthorDto>(context);

        [UseSqlLogging]
        [UseProjection]
        public IQueryable<ImageDto> GetImages(BloggingContext dbContext, IResolverContext context)
            => dbContext.Images.ProjectTo<Image, ImageDto>(context);

        [UseSqlLogging]
        [UseProjection]
        public IQueryable<MembershipDto> GetMembers(
            BloggingContext dbContext,
            IResolverContext context)
            => dbContext.Memberships.ProjectTo<Membership, MembershipDto>(context);
    }

    public class BloggingContext : DbContext
    {
        public DbSet<Blog> Blogs { get; set; } = default!;

        public DbSet<Post> Posts { get; set; } = default!;

        public DbSet<Author> Authors { get; set; } = default!;

        public DbSet<Image> Images { get; set; } = default!;

        public DbSet<Membership> Memberships { get; set; } = default!;

        public BloggingContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Membership>()
                .HasDiscriminator<string>("d")
                .HasValue<PremiumMember>("premium")
                .HasValue<StandardMember>("standard");
        }
    }

    public class PostDto
    {
        public int PostId { get; set; }

        public string? Title { get; set; }

        public string? Content { get; set; }

        public int BlogId { get; set; }

        public AuthorDto? Author { get; set; }

        public BlogDto? Blog { get; set; }
    }

    public class Blog
    {
        public int BlogId { get; set; }

        public string? Name { get; set; }

        public string? Url { get; set; }

        public int AuthorId { get; set; }

        public Author? Author { get; set; }

        public int ImageId { get; set; }

        public Image? TitleImage { get; set; }

        public ICollection<Post>? Posts { get; set; }
    }

    public class Author
    {
        public int AuthorId { get; set; }

        public string? Name { get; set; }

        public ICollection<Post> Posts { get; set; } = default!;

        public int MembershipId { get; set; }
        public Membership? Membership { get; set; }

        public ICollection<Blog> Blogs { get; set; } = default!;
    }

    public class Image
    {
        public int ImageId { get; set; }

        public string? Url { get; set; }

        public Post? Post { get; set; }
    }

    public class ImageDto
    {
        public string? Url { get; set; }

        public PostDto? Post { get; set; }
    }

    public class BlogDto
    {
        public int BlogId { get; set; }

        public string? Name { get; set; }

        public string? Url { get; set; }

        public ICollection<PostDto>? Posts { get; set; }

        public AuthorDto? Author { get; set; }

        public ImageDto? TitleImage { get; set; }
    }

    public class AuthorDto
    {
        public string? Name { get; set; }

        public ICollection<PostDto>? Posts { get; set; }

        public ICollection<BlogDto>? Blogs { get; set; }
    }

    public class Post
    {
        public int? PostId { get; set; }

        public string? Title { get; set; }

        public string? Content { get; set; }

        public int? BlogId { get; set; }

        public Blog? Blog { get; set; }

        public int? AuthorId { get; set; }

        public Author? Author { get; set; }
    }

    public class Membership
    {
        public int MembershipId { get; set; }

        public string? Name { get; set; }
    }

    public class PremiumMember : Membership
    {
        public string? Premium { get; set; }
    }

    public class StandardMember : Membership
    {
        public string? Standard { get; set; }
    }

    public class MembershipDto
    {
        public string? Name { get; set; }
    }

    public class PremiumMemberDto : MembershipDto
    {
        public string? Premium { get; set; }
    }

    public class StandardMemberDto : MembershipDto
    {
        public string? Standard { get; set; }
    }

    public class PostProfile : Profile
    {
        public PostProfile()
        {
            CreateMap<Post, PostDto>().ForAllMembers(x => x.ExplicitExpansion());
        }
    }

    public class BlogProfile : Profile
    {
        public BlogProfile()
        {
            CreateMap<Blog, BlogDto>().ForAllMembers(x => x.ExplicitExpansion());
        }
    }

    public class AuthorProfile : Profile
    {
        public AuthorProfile()
        {
            CreateMap<Author, AuthorDto>().ForAllMembers(x => x.ExplicitExpansion());
        }
    }

    public class ImageProfile : Profile
    {
        public ImageProfile()
        {
            CreateMap<Image, ImageDto>().ForAllMembers(x => x.ExplicitExpansion());
        }
    }

    public class MembershipProfile : Profile
    {
        public MembershipProfile()
        {
            CreateMap<PremiumMember, PremiumMemberDto>();
            CreateMap<StandardMember, StandardMemberDto>();
            CreateMap<Membership, MembershipDto>().IncludeAllDerived();
        }
    }
}
