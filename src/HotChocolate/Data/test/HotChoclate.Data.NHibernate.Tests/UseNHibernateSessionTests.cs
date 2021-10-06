using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Execution;
using FluentNHibernate.Cfg.Db;
using HotChoclate.Data;
using Microsoft.Extensions.DependencyInjection;
using NHibernate;
using NHibernate.Tool.hbm2ddl;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data
{
    public class UseNHibernateSessionTests
    {
        [Fact]
        public async Task Execute_Queryable()
        {
            // arrange
            
            IServiceProvider services =
                new ServiceCollection()
                    .AddNHibernateFactory(typeof(Author).Assembly, SQLiteConfiguration.Standard.InMemory,true)
                    .AddGraphQL()
                    .AddFiltering()
                    .AddSorting()
                    .AddProjections()
                    .AddQueryType<Query>()
                    .Services
                    .BuildServiceProvider();

            IRequestExecutor executor =
                await services.GetRequiredService<IRequestExecutorResolver>()
                    .GetRequestExecutorAsync();
   
            using (ISession session = services.GetRequiredService<ISession>())
            {
                IQueryable<Author> authors = session.Query<Author>();
                authors.ToList().Add(new Author { Name = "foo" });

                foreach (Author author in authors)
                    await session.SaveAsync(author).ConfigureAwait(false);
            }

            // act
            IExecutionResult result = await executor.ExecuteAsync("{ authors { name } }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Execute_Queryable_Task()
        {
            // arrange
            IServiceProvider services =
                new ServiceCollection()
                    .AddNHibernateFactory(typeof(Author).Assembly, SQLiteConfiguration.Standard.InMemory, true)
                    .AddGraphQL()
                    .AddFiltering()
                    .AddSorting()
                    .AddProjections()
                    .AddQueryType<QueryTask>()
                    .Services
                    .BuildServiceProvider();

            IRequestExecutor executor =
                await services.GetRequiredService<IRequestExecutorResolver>()
                    .GetRequestExecutorAsync();

            using (ISession session = services.GetRequiredService<ISession>())
            {
                IList<Author> authors = session.Query<Author>().ToList();
                authors.Add(new Author {Name = "foo"});

                foreach (Author author in authors)
                {
                    await session.SaveAsync(author).ConfigureAwait(false);
                }
            }

            // act
            IExecutionResult result = await executor.ExecuteAsync("{ authors { name } }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Execute_Queryable_ValueTask()
        {
            // arrange
            IServiceProvider services =
                new ServiceCollection()
                    .AddNHibernateFactory(typeof(Author).Assembly, SQLiteConfiguration.Standard.InMemory, true)
                    .AddGraphQL()
                    .AddFiltering()
                    .AddSorting()
                    .AddProjections()
                    .AddQueryType<QueryValueTask>()
                    .Services
                    .BuildServiceProvider();

            IRequestExecutor executor =
                await services.GetRequiredService<IRequestExecutorResolver>()
                    .GetRequestExecutorAsync();

            using (ISession session = services.GetRequiredService<ISession>())
            {
                IList<Author> authors = session.Query<Author>().ToList();
                authors.Add(new Author {Name = "foo"});

                foreach (Author author in authors)
                {
                    await session.SaveAsync(author).ConfigureAwait(false);
                }
            }

            // act
            IExecutionResult result = await executor.ExecuteAsync("{ authors { name } }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Execute_Queryable_OffsetPaging_TotalCount()
        {
            // arrange
            IServiceProvider services =
                new ServiceCollection()
                    .AddNHibernateFactory(typeof(Author).Assembly, SQLiteConfiguration.Standard.InMemory, true)
                    .AddGraphQL()
                    .AddFiltering()
                    .AddSorting()
                    .AddProjections()
                    .AddQueryType<Query>()
                    .Services
                    .BuildServiceProvider();

            IRequestExecutor executor =
                await services.GetRequiredService<IRequestExecutorResolver>()
                    .GetRequestExecutorAsync();
         
            using (ISession session = services.GetRequiredService<ISession>())
            {
                IList<Author> authors = session.Query<Author>().ToList();
                authors.Add(new Author {Name = "foo"});
                authors.Add(new Author {Name = "bar"});

                foreach (Author author in authors)
                {
                    await session.SaveAsync(author).ConfigureAwait(false);
                }
            }


            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"query Test {
                    authorOffsetPaging {
                        items {
                            name
                        }
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                        }
                        totalCount
                    }
                }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Execute_Queryable_OffsetPaging_TotalCount_Task()
        {
            // arrange
            IServiceProvider services =
                new ServiceCollection()
                    .AddNHibernateFactory(typeof(Author).Assembly, SQLiteConfiguration.Standard.InMemory, true)
                    .AddGraphQL()
                    .AddFiltering()
                    .AddSorting()
                    .AddProjections()
                    .AddQueryType<QueryTask>()
                    .Services
                    .BuildServiceProvider();

            IRequestExecutor executor =
                await services.GetRequiredService<IRequestExecutorResolver>()
                    .GetRequestExecutorAsync();

            using (ISession session = services.GetRequiredService<ISession>())
            {
                IList<Author> authors = session.Query<Author>().ToList();
                authors.Add(new Author {Name = "foo"});
                authors.Add(new Author {Name = "bar"});

                foreach (Author author in authors)
                {
                    await session.SaveAsync(author).ConfigureAwait(false);
                }
            }


            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"query Test {
                    authorOffsetPaging {
                        items {
                            name
                        }
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                        }
                        totalCount
                    }
                }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Execute_Queryable_OffsetPaging_TotalCount_ValueTask()
        {
            // arrange
            IServiceProvider services =
                new ServiceCollection()
                    .AddNHibernateFactory(typeof(Author).Assembly, SQLiteConfiguration.Standard.InMemory, true)
                    .AddGraphQL()
                    .AddFiltering()
                    .AddSorting()
                    .AddProjections()
                    .AddQueryType<QueryValueTask>()
                    .Services
                    .BuildServiceProvider();

            IRequestExecutor executor =
                await services.GetRequiredService<IRequestExecutorResolver>()
                    .GetRequestExecutorAsync();

            using (ISession session = services.GetRequiredService<ISession>())
            {
                IList<Author> authors = session.Query<Author>().ToList();
                authors.Add(new Author {Name = "foo"});
                authors.Add(new Author {Name = "bar"});

                foreach (Author author in authors)
                {
                    await session.SaveAsync(author).ConfigureAwait(false);
                }
            }

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"query Test {
                    authorOffsetPaging {
                        items {
                            name
                        }
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                        }
                        totalCount
                    }
                }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Execute_Queryable_OffsetPaging()
        {
            // arrange
            IServiceProvider services =
                new ServiceCollection()
                    .AddNHibernateFactory(typeof(Author).Assembly, SQLiteConfiguration.Standard.InMemory, true)
                    .AddGraphQL()
                    .AddFiltering()
                    .AddSorting()
                    .AddProjections()
                    .AddQueryType<Query>()
                    .Services
                    .BuildServiceProvider();

            IRequestExecutor executor =
                await services.GetRequiredService<IRequestExecutorResolver>()
                    .GetRequestExecutorAsync();
       
            using (ISession session = services.GetRequiredService<ISession>())
            {
                IList<Author> authors = session.Query<Author>().ToList();
                authors.Add(new Author {Name = "foo"});
                authors.Add(new Author {Name = "bar"});

                foreach (Author author in authors)
                {
                    await session.SaveAsync(author).ConfigureAwait(false);
                }
            }

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"query Test {
                    authorOffsetPaging {
                        items {
                            name
                        }
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                        }
                    }
                }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Execute_Queryable_OffsetPaging_Task()
        {
            // arrange
            IServiceProvider services =
                new ServiceCollection()
                    .AddNHibernateFactory(typeof(Author).Assembly, SQLiteConfiguration.Standard.InMemory, true)
                    .AddGraphQL()
                    .AddFiltering()
                    .AddSorting()
                    .AddProjections()
                    .AddQueryType<QueryTask>()
                    .Services
                    .BuildServiceProvider();

            IRequestExecutor executor =
                await services.GetRequiredService<IRequestExecutorResolver>()
                    .GetRequestExecutorAsync();

            using (ISession session = services.GetRequiredService<ISession>())
            {
                IList<Author> authors = session.Query<Author>().ToList();
                authors.Add(new Author {Name = "foo"});
                authors.Add(new Author {Name = "bar"});

                foreach (Author author in authors)
                {
                    await session.SaveAsync(author).ConfigureAwait(false);
                }
            }

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                @"query Test {
                    authorOffsetPaging {
                        items {
                            name
                        }
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                        }
                    }
                }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Execute_Queryable_OffsetPaging_ValueTask()
        {
            // arrange
            IServiceProvider services =
                new ServiceCollection()
                    .AddNHibernateFactory(typeof(Author).Assembly, SQLiteConfiguration.Standard.InMemory, true)
                    .AddGraphQL()
                    .AddFiltering()
                    .AddSorting()
                    .AddProjections()
                    .AddQueryType<QueryValueTask>()
                    .Services
                    .BuildServiceProvider();

            IRequestExecutor executor =
                await services.GetRequiredService<IRequestExecutorResolver>()
                    .GetRequestExecutorAsync();
           
            using (ISession session = services.GetRequiredService<ISession>())
            {
                IList<Author> authors = session.Query<Author>().ToList();
                authors.Add(new Author {Name = "foo"});
                authors.Add(new Author {Name = "bar"});

                foreach (Author author in authors)
                {
                    await session.SaveAsync(author).ConfigureAwait(false);
                }

                // act
                IExecutionResult result = await executor.ExecuteAsync(
                    @"query Test {
                    authorOffsetPaging {
                        items {
                            name
                        }
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                        }
                    }
                }");

                // assert
                result.ToJson().MatchSnapshot();
            }
        }

        [Fact]
        public async Task Execute_Single()
        {
            // arrange
            IServiceProvider services =
                new ServiceCollection()
                    .AddNHibernateFactory(typeof(Author).Assembly, SQLiteConfiguration.Standard.InMemory, true)
                    .AddGraphQL()
                    .AddFiltering()
                    .AddSorting()
                    .AddProjections()
                    .AddQueryType<Query>()
                    .Services
                    .BuildServiceProvider();

            IRequestExecutor executor =
                await services.GetRequiredService<IRequestExecutorResolver>()
                    .GetRequestExecutorAsync();

            using (ISession session = services.GetRequiredService<ISession>())
            {
                IList<Author> authors = session.Query<Author>().ToList();
                authors.Add(new Author {Name = "foo"});

                foreach (Author author in authors)
                {
                    await session.SaveAsync(author).ConfigureAwait(false);
                }
            }

            // act
            IExecutionResult result = await executor.ExecuteAsync("{ author { name } }");

            // assert
            result.ToJson().MatchSnapshot();
        }

        private static string CreateConnectionString()
        {
            return $"Data Source={Guid.NewGuid():N}.db";
        }

        private void CreateSchema(NHibernate.Cfg.Configuration configuration,  DbConnection connection)
        {
            new SchemaExport(configuration).Execute(true, true, false, connection, Console.Out);
        }
    }
}
