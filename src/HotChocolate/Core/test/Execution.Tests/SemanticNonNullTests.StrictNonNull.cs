using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

public partial class SemanticNonNullTests
{
    public class StrictNonNull
    {
        #region Scalar

        [Fact]
        public async Task Async_Scalar_Returns_Null_Should_Error()
        {
            var result = await new ServiceCollection()
                .AddGraphQL()
                .ModifyOptions(o =>
                {
                    o.EnableSemanticNonNull = true;
                })
                .AddQueryType<Query>()
                .ExecuteRequestAsync("""
                                     {
                                       scalarReturningNull
                                     }
                                     """);

            result.MatchSnapshot();
        }

        [Fact]
        public async Task Async_Scalar_Throwing_Should_Null_And_Error()
        {
            var result = await new ServiceCollection()
                .AddGraphQL()
                .ModifyOptions(o =>
                {
                    o.EnableSemanticNonNull = true;
                })
                .AddQueryType<Query>()
                .ExecuteRequestAsync("""
                                     {
                                       scalarThrowingError
                                     }
                                     """);

            result.MatchSnapshot();
        }

        [Fact]
        public async Task Async_Nullable_Scalar_Returns_Null_Should_Null_Without_Error()
        {
            var result = await new ServiceCollection()
                .AddGraphQL()
                .ModifyOptions(o =>
                {
                    o.EnableSemanticNonNull = true;
                })
                .AddQueryType<Query>()
                .ExecuteRequestAsync("""
                                     {
                                       nullableScalarReturningNull
                                     }
                                     """);

            result.MatchSnapshot();
        }

        [Fact]
        public async Task Pure_Scalar_Returns_Null_Should_Error()
        {
            var result = await new ServiceCollection()
                .AddGraphQL()
                .ModifyOptions(o =>
                {
                    o.EnableSemanticNonNull = true;
                })
                .AddQueryType<Query>()
                .ExecuteRequestAsync("""
                                     {
                                       pureScalarReturningNull
                                     }
                                     """);

            result.MatchSnapshot();
        }

        [Fact]
        public async Task Pure_Scalar_Throwing_Should_Null_And_Error()
        {
            var result = await new ServiceCollection()
                .AddGraphQL()
                .ModifyOptions(o =>
                {
                    o.EnableSemanticNonNull = true;
                })
                .AddQueryType<Query>()
                .ExecuteRequestAsync("""
                                     {
                                       pureScalarThrowingError
                                     }
                                     """);

            result.MatchSnapshot();
        }

        [Fact]
        public async Task Pure_Nullable_Scalar_Returns_Null_Should_Null_Without_Error()
        {
            var result = await new ServiceCollection()
                .AddGraphQL()
                .ModifyOptions(o =>
                {
                    o.EnableSemanticNonNull = true;
                })
                .AddQueryType<Query>()
                .ExecuteRequestAsync("""
                                     {
                                       pureNullableScalarReturningNull
                                     }
                                     """);

            result.MatchSnapshot();
        }

        #endregion

        #region Scalar List

        [Fact]
        public async Task Async_Scalar_List_Returns_Null_Should_Error()
        {
            var result = await new ServiceCollection()
                .AddGraphQL()
                .ModifyOptions(o =>
                {
                    o.EnableSemanticNonNull = true;
                })
                .AddQueryType<Query>()
                .ExecuteRequestAsync("""
                                     {
                                       scalarListReturningNull
                                     }
                                     """);

            result.MatchSnapshot();
        }

        [Fact]
        public async Task Async_Scalar_List_Throwing_Should_Null_And_Error()
        {
            var result = await new ServiceCollection()
                .AddGraphQL()
                .ModifyOptions(o =>
                {
                    o.EnableSemanticNonNull = true;
                })
                .AddQueryType<Query>()
                .ExecuteRequestAsync("""
                                     {
                                       scalarListThrowingError
                                     }
                                     """);

            result.MatchSnapshot();
        }

        [Fact]
        public async Task Async_Nullable_Scalar_List_Returns_Null_Should_Null_Without_Error()
        {
            var result = await new ServiceCollection()
                .AddGraphQL()
                .ModifyOptions(o =>
                {
                    o.EnableSemanticNonNull = true;
                })
                .AddQueryType<Query>()
                .ExecuteRequestAsync("""
                                     {
                                       nullableScalarListReturningNull
                                     }
                                     """);

            result.MatchSnapshot();
        }

        [Fact]
        public async Task Pure_Scalar_List_Returns_Null_Should_Error()
        {
            var result = await new ServiceCollection()
                .AddGraphQL()
                .ModifyOptions(o =>
                {
                    o.EnableSemanticNonNull = true;
                })
                .AddQueryType<Query>()
                .ExecuteRequestAsync("""
                                     {
                                       pureScalarListReturningNull
                                     }
                                     """);

            result.MatchSnapshot();
        }

        [Fact]
        public async Task Pure_Scalar_List_Throwing_Should_Null_And_Error()
        {
            var result = await new ServiceCollection()
                .AddGraphQL()
                .ModifyOptions(o =>
                {
                    o.EnableSemanticNonNull = true;
                })
                .AddQueryType<Query>()
                .ExecuteRequestAsync("""
                                     {
                                       pureScalarListThrowingError
                                     }
                                     """);

            result.MatchSnapshot();
        }

        [Fact]
        public async Task Pure_Nullable_Scalar_List_Returns_Null_Should_Null_Without_Error()
        {
            var result = await new ServiceCollection()
                .AddGraphQL()
                .ModifyOptions(o =>
                {
                    o.EnableSemanticNonNull = true;
                })
                .AddQueryType<Query>()
                .ExecuteRequestAsync("""
                                     {
                                       pureNullableScalarListReturningNull
                                     }
                                     """);

            result.MatchSnapshot();
        }

        #endregion

        #region Scalar List Item

        [Fact]
        public async Task Async_Scalar_List_Item_Returns_Null_Should_Error_Item()
        {
            var result = await new ServiceCollection()
                .AddGraphQL()
                .ModifyOptions(o =>
                {
                    o.EnableSemanticNonNull = true;
                })
                .AddQueryType<Query>()
                .ExecuteRequestAsync("""
                                     {
                                       scalarListItemReturningNull
                                     }
                                     """);

            result.MatchSnapshot();
        }

        [Fact]
        public async Task Async_Scalar_List_Item_Throwing_Should_Null_And_Error_Item()
        {
            var result = await new ServiceCollection()
                .AddGraphQL()
                .ModifyOptions(o =>
                {
                    o.EnableSemanticNonNull = true;
                })
                .AddQueryType<Query>()
                .ExecuteRequestAsync("""
                                     {
                                       scalarListItemThrowingError
                                     }
                                     """);

            result.MatchSnapshot();
        }

        [Fact]
        public async Task Async_Nullable_Scalar_List_Item_Returns_Null_Should_Null_Item_Without_Error()
        {
            var result = await new ServiceCollection()
                .AddGraphQL()
                .ModifyOptions(o =>
                {
                    o.EnableSemanticNonNull = true;
                })
                .AddQueryType<Query>()
                .ExecuteRequestAsync("""
                                     {
                                       nullableScalarListItemReturningNull
                                     }
                                     """);

            result.MatchSnapshot();
        }

        [Fact]
        public async Task Pure_Scalar_List_Item_Returns_Null_Should_Error_Item()
        {
            var result = await new ServiceCollection()
                .AddGraphQL()
                .ModifyOptions(o =>
                {
                    o.EnableSemanticNonNull = true;
                })
                .AddQueryType<Query>()
                .ExecuteRequestAsync("""
                                     {
                                       pureScalarListItemReturningNull
                                     }
                                     """);

            result.MatchSnapshot();
        }

        [Fact]
        public async Task Pure_Scalar_List_Item_Throwing_Should_Null_And_Error_Item()
        {
            var result = await new ServiceCollection()
                .AddGraphQL()
                .ModifyOptions(o =>
                {
                    o.EnableSemanticNonNull = true;
                })
                .AddQueryType<Query>()
                .ExecuteRequestAsync("""
                                     {
                                       pureScalarListItemThrowingError
                                     }
                                     """);

            result.MatchSnapshot();
        }

        [Fact]
        public async Task Pure_Nullable_Scalar_List_Item_Returns_Null_Should_Null_Item_Without_Error()
        {
            var result = await new ServiceCollection()
                .AddGraphQL()
                .ModifyOptions(o =>
                {
                    o.EnableSemanticNonNull = true;
                })
                .AddQueryType<Query>()
                .ExecuteRequestAsync("""
                                     {
                                       pureNullableScalarListItemReturningNull
                                     }
                                     """);

            result.MatchSnapshot();
        }

        #endregion

        #region Object

        [Fact]
        public async Task Async_Object_Returns_Null_Should_Error()
        {
            var result = await new ServiceCollection()
                .AddGraphQL()
                .ModifyOptions(o =>
                {
                    o.EnableSemanticNonNull = true;
                })
                .AddQueryType<Query>()
                .ExecuteRequestAsync("""
                                     {
                                       objectReturningNull {
                                         property
                                       }
                                     }
                                     """);

            result.MatchSnapshot();
        }

        [Fact]
        public async Task Async_Object_Throwing_Should_Null_And_Error()
        {
            var result = await new ServiceCollection()
                .AddGraphQL()
                .ModifyOptions(o =>
                {
                    o.EnableSemanticNonNull = true;
                })
                .AddQueryType<Query>()
                .ExecuteRequestAsync("""
                                     {
                                       objectThrowingError {
                                         property
                                       }
                                     }
                                     """);

            result.MatchSnapshot();
        }

        [Fact]
        public async Task Async_Nullable_Object_Returns_Null_Should_Null_Without_Error()
        {
            var result = await new ServiceCollection()
                .AddGraphQL()
                .ModifyOptions(o =>
                {
                    o.EnableSemanticNonNull = true;
                })
                .AddQueryType<Query>()
                .ExecuteRequestAsync("""
                                     {
                                       nullableObjectReturningNull {
                                         property
                                       }
                                     }
                                     """);

            result.MatchSnapshot();
        }

        [Fact]
        public async Task Pure_Object_Returns_Null_Should_Error()
        {
            var result = await new ServiceCollection()
                .AddGraphQL()
                .ModifyOptions(o =>
                {
                    o.EnableSemanticNonNull = true;
                })
                .AddQueryType<Query>()
                .ExecuteRequestAsync("""
                                     {
                                       pureObjectReturningNull {
                                         property
                                       }
                                     }
                                     """);

            result.MatchSnapshot();
        }

        [Fact]
        public async Task Pure_Object_Throwing_Should_Null_And_Error()
        {
            var result = await new ServiceCollection()
                .AddGraphQL()
                .ModifyOptions(o =>
                {
                    o.EnableSemanticNonNull = true;
                })
                .AddQueryType<Query>()
                .ExecuteRequestAsync("""
                                     {
                                       pureObjectThrowingError {
                                         property
                                       }
                                     }
                                     """);

            result.MatchSnapshot();
        }

        [Fact]
        public async Task Pure_Nullable_Object_Returns_Null_Should_Null_Without_Error()
        {
            var result = await new ServiceCollection()
                .AddGraphQL()
                .ModifyOptions(o =>
                {
                    o.EnableSemanticNonNull = true;
                })
                .AddQueryType<Query>()
                .ExecuteRequestAsync("""
                                     {
                                       pureNullableObjectReturningNull {
                                         property
                                       }
                                     }
                                     """);

            result.MatchSnapshot();
        }

        #endregion

        #region Object List

        [Fact]
        public async Task Async_Object_List_Returns_Null_Should_Error()
        {
            var result = await new ServiceCollection()
                .AddGraphQL()
                .ModifyOptions(o =>
                {
                    o.EnableSemanticNonNull = true;
                })
                .AddQueryType<Query>()
                .ExecuteRequestAsync("""
                                     {
                                       objectListReturningNull {
                                         property
                                       }
                                     }
                                     """);

            result.MatchSnapshot();
        }

        [Fact]
        public async Task Async_Object_List_Throwing_Should_Null_FAnd_Error()
        {
            var result = await new ServiceCollection()
                .AddGraphQL()
                .ModifyOptions(o =>
                {
                    o.EnableSemanticNonNull = true;
                })
                .AddQueryType<Query>()
                .ExecuteRequestAsync("""
                                     {
                                       objectListThrowingError {
                                         property
                                       }
                                     }
                                     """);

            result.MatchSnapshot();
        }

        [Fact]
        public async Task Async_Nullable_Object_List_Returns_Null_Should_Null_Without_Error()
        {
            var result = await new ServiceCollection()
                .AddGraphQL()
                .ModifyOptions(o =>
                {
                    o.EnableSemanticNonNull = true;
                })
                .AddQueryType<Query>()
                .ExecuteRequestAsync("""
                                     {
                                       nullableObjectListReturningNull {
                                         property
                                       }
                                     }
                                     """);

            result.MatchSnapshot();
        }

        [Fact]
        public async Task Pure_Object_List_Returns_Null_Should_Error()
        {
            var result = await new ServiceCollection()
                .AddGraphQL()
                .ModifyOptions(o =>
                {
                    o.EnableSemanticNonNull = true;
                })
                .AddQueryType<Query>()
                .ExecuteRequestAsync("""
                                     {
                                       pureObjectListReturningNull {
                                         property
                                       }
                                     }
                                     """);

            result.MatchSnapshot();
        }

        [Fact]
        public async Task Pure_Object_List_Throwing_Should_Null_FAnd_Error()
        {
            var result = await new ServiceCollection()
                .AddGraphQL()
                .ModifyOptions(o =>
                {
                    o.EnableSemanticNonNull = true;
                })
                .AddQueryType<Query>()
                .ExecuteRequestAsync("""
                                     {
                                       pureObjectListThrowingError {
                                         property
                                       }
                                     }
                                     """);

            result.MatchSnapshot();
        }

        [Fact]
        public async Task Pure_Nullable_Object_List_Returns_Null_Should_Null_Without_Error()
        {
            var result = await new ServiceCollection()
                .AddGraphQL()
                .ModifyOptions(o =>
                {
                    o.EnableSemanticNonNull = true;
                })
                .AddQueryType<Query>()
                .ExecuteRequestAsync("""
                                     {
                                       pureNullableObjectListReturningNull {
                                         property
                                       }
                                     }
                                     """);

            result.MatchSnapshot();
        }

        #endregion

        #region Object List Item

        [Fact]
        public async Task Async_Object_List_Item_Returns_Null_Should_Error_Item()
        {
            var result = await new ServiceCollection()
                .AddGraphQL()
                .ModifyOptions(o =>
                {
                    o.EnableSemanticNonNull = true;
                })
                .AddQueryType<Query>()
                .ExecuteRequestAsync("""
                                     {
                                       objectListItemReturningNull {
                                         property
                                       }
                                     }
                                     """);

            result.MatchSnapshot();
        }

        [Fact]
        public async Task Async_Object_List_Item_Throwing_Should_Null_And_Error_Item()
        {
            var result = await new ServiceCollection()
                .AddGraphQL()
                .ModifyOptions(o =>
                {
                    o.EnableSemanticNonNull = true;
                })
                .AddQueryType<Query>()
                .ExecuteRequestAsync("""
                                     {
                                       objectListItemThrowingError {
                                         property
                                       }
                                     }
                                     """);

            result.MatchSnapshot();
        }

        [Fact]
        public async Task Async_Nullable_Object_List_Item_Returns_Null_Should_Null_Item_Without_Error()
        {
            var result = await new ServiceCollection()
                .AddGraphQL()
                .ModifyOptions(o =>
                {
                    o.EnableSemanticNonNull = true;
                })
                .AddQueryType<Query>()
                .ExecuteRequestAsync("""
                                     {
                                       nullableObjectListItemReturningNull {
                                         property
                                       }
                                     }
                                     """);

            result.MatchSnapshot();
        }

        [Fact]
        public async Task Pure_Object_List_Item_Returns_Null_Should_Error_Item()
        {
            var result = await new ServiceCollection()
                .AddGraphQL()
                .ModifyOptions(o =>
                {
                    o.EnableSemanticNonNull = true;
                })
                .AddQueryType<Query>()
                .ExecuteRequestAsync("""
                                     {
                                       pureObjectListItemReturningNull {
                                         property
                                       }
                                     }
                                     """);

            result.MatchSnapshot();
        }

        [Fact]
        public async Task Pure_Object_List_Item_Throwing_Should_Null_And_Error_Item()
        {
            var result = await new ServiceCollection()
                .AddGraphQL()
                .ModifyOptions(o =>
                {
                    o.EnableSemanticNonNull = true;
                })
                .AddQueryType<Query>()
                .ExecuteRequestAsync("""
                                     {
                                       pureObjectListItemThrowingError {
                                         property
                                       }
                                     }
                                     """);

            result.MatchSnapshot();
        }

        [Fact]
        public async Task Pure_Nullable_Object_List_Item_Returns_Null_Should_Null_Item_Without_Error()
        {
            var result = await new ServiceCollection()
                .AddGraphQL()
                .ModifyOptions(o =>
                {
                    o.EnableSemanticNonNull = true;
                })
                .AddQueryType<Query>()
                .ExecuteRequestAsync("""
                                     {
                                       pureNullableObjectListItemReturningNull {
                                         property
                                       }
                                     }
                                     """);

            result.MatchSnapshot();
        }

        #endregion

        [Fact]
        public async Task Mutation_With_MutationConventions()
        {
            var result = await new ServiceCollection()
                .AddGraphQL()
                .ModifyOptions(o =>
                {
                    o.StrictValidation = false;
                    o.EnableSemanticNonNull = true;
                })
                .AddMutationConventions()
                .AddMutationType<Mutation>()
                .ExecuteRequestAsync("""
                                     mutation {
                                       someMutationReturningNull {
                                         scalarReturningNull
                                       }
                                     }
                                     """);

            result.MatchSnapshot();
        }

        [Fact]
        public async Task Query_With_Connection()
        {
            var result = await new ServiceCollection()
                .AddGraphQL()
                .ModifyOptions(o =>
                {
                    o.EnableSemanticNonNull = true;
                })
                .AddMutationConventions()
                .AddQueryType<Query>()
                .ExecuteRequestAsync("""
                                     {
                                       scalarConnection {
                                         edges {
                                           node
                                         }
                                       }
                                     }
                                     """);

            result.MatchSnapshot();
        }

        [Fact]
        public async Task Query_With_NullableConnectionNodes()
        {
            var result = await new ServiceCollection()
                .AddGraphQL()
                .ModifyOptions(o =>
                {
                    o.EnableSemanticNonNull = true;
                })
                .AddMutationConventions()
                .AddQueryType<Query>()
                .ExecuteRequestAsync("""
                                     {
                                       nullableScalarConnection {
                                         edges {
                                           node
                                         }
                                       }
                                     }
                                     """);

            result.MatchSnapshot();
        }

        [Fact]
        public async Task Pure_Scalar_ListOfList_Nullable_Outer_And_Inner_Middle_Returns_Null_Should_Null_And_Error()
        {
            var result = await new ServiceCollection()
                .AddGraphQL()
                .ModifyOptions(o =>
                {
                    o.EnableSemanticNonNull = true;
                })
                .AddMutationConventions()
                .AddQueryType<Query>()
                .ExecuteRequestAsync("""
                                     {
                                       nestedScalarArrayNullableOuterItems
                                     }
                                     """);

            result.MatchSnapshot();
        }

        [Fact]
        public async Task
            Pure_Scalar_ListOfList_Nullable_Middle_Item_Outer_And_Inner_Return_Null_Should_Null_And_Error()
        {
            var result = await new ServiceCollection()
                .AddGraphQL()
                .ModifyOptions(o =>
                {
                    o.EnableSemanticNonNull = true;
                })
                .AddMutationConventions()
                .AddQueryType<Query>()
                .ExecuteRequestAsync("""
                                     {
                                       nestedScalarArrayNullableMiddleItem
                                     }
                                     """);

            result.MatchSnapshot();
        }

        public class Query
        {
            #region Scalar

            [GraphQLStrictNonNullType]
            public Task<string> ScalarReturningNull()
            {
                return Task.FromResult<string>(null!);
            }

            [GraphQLStrictNonNullType]
            public Task<string> ScalarThrowingError()
            {
                throw new Exception("Something went wrong");
            }

            [GraphQLStrictNonNullType]
            public Task<string?> NullableScalarReturningNull()
            {
                return Task.FromResult<string?>(null);
            }

            [GraphQLStrictNonNullType]
            public string PureScalarReturningNull => null!;

            [GraphQLStrictNonNullType]
            public string PureScalarThrowingError => throw new Exception("Somethin went wrong");

            [GraphQLStrictNonNullType]
            public string? PureNullableScalarReturningNull => null;

            #endregion

            #region Scalar List

            [GraphQLStrictNonNullType]
            public Task<string[]> ScalarListReturningNull()
            {
                return Task.FromResult<string[]>(null!);
            }

            [GraphQLStrictNonNullType]
            public Task<string[]> ScalarListThrowingError()
            {
                throw new Exception("Something went wrong");
            }

            [GraphQLStrictNonNullType]
            public Task<string[]?> NullableScalarListReturningNull()
            {
                return Task.FromResult<string[]?>(null);
            }

            [GraphQLStrictNonNullType]
            public string[] PureScalarListReturningNull => null!;

            [GraphQLStrictNonNullType]
            public string[] PureScalarListThrowingError => throw new Exception("Somethin went wrong");

            [GraphQLStrictNonNullType]
            public string[]? PureNullableScalarListReturningNull => null;

            #endregion

            #region Scalar List Item

            [GraphQLStrictNonNullType]
            public Task<string[]> ScalarListItemReturningNull()
            {
                return Task.FromResult<string[]>(["a", null!, "c"]);
            }

            [GraphQLStrictNonNullType]
            public Task<string[]> ScalarListItemThrowingError(IResolverContext context)
            {
                // TODO: How can you create a terminating error for a single item?
                context.ReportError(ErrorBuilder.New().SetMessage("Another error").SetPath(context.Path.Append(1))
                    .Build());
                return Task.FromResult<string[]>(["a", null!, "c"]);
            }

            [GraphQLStrictNonNullType]
            public Task<string?[]> NullableScalarListItemReturningNull()
            {
                return Task.FromResult<string?[]>(["a", null, "c"]);
            }

            [GraphQLStrictNonNullType]
            public string[] PureScalarListItemReturningNull => ["a", null!, "c"];

            // TODO: This is no longer a pure resolver as soon as it access the IResolverContext, right?
            [GraphQLStrictNonNullType]
            public string[] PureScalarListItemThrowingError(IResolverContext context)
            {
                // TODO: How can you create a terminating error for a single item?
                context.ReportError(ErrorBuilder.New().SetMessage("Another error").SetPath(context.Path.Append(1))
                    .Build());
                return ["a", null!, "c"];
            }

            [GraphQLStrictNonNullType]
            public string?[] PureNullableScalarListItemReturningNull => ["a", null, "c"];

            #endregion

            #region Object

            [GraphQLStrictNonNullType]
            public Task<SomeObject> ObjectReturningNull()
            {
                return Task.FromResult<SomeObject>(null!);
            }

            [GraphQLStrictNonNullType]
            public Task<SomeObject> ObjectThrowingError()
            {
                throw new Exception("Something went wrong");
            }

            [GraphQLStrictNonNullType]
            public Task<SomeObject?> NullableObjectReturningNull()
            {
                return Task.FromResult<SomeObject?>(null);
            }

            [GraphQLStrictNonNullType]
            public SomeObject PureObjectReturningNull => null!;

            [GraphQLStrictNonNullType]
            public SomeObject PureObjectThrowingError => throw new Exception("Somethin went wrong");

            [GraphQLStrictNonNullType]
            public SomeObject? PureNullableObjectReturningNull => null;

            #endregion

            #region Object List

            [GraphQLStrictNonNullType]
            public Task<SomeObject[]> ObjectListReturningNull()
            {
                return Task.FromResult<SomeObject[]>(null!);
            }

            [GraphQLStrictNonNullType]
            public Task<SomeObject[]> ObjectListThrowingError()
            {
                throw new Exception("Something went wrong");
            }

            [GraphQLStrictNonNullType]
            public Task<SomeObject[]?> NullableObjectListReturningNull()
            {
                return Task.FromResult<SomeObject[]?>(null);
            }

            [GraphQLStrictNonNullType]
            public SomeObject[] PureObjectListReturningNull => null!;

            [GraphQLStrictNonNullType]
            public SomeObject[] PureObjectListThrowingError => throw new Exception("Somethin went wrong");

            [GraphQLStrictNonNullType]
            public SomeObject[]? PureNullableObjectListReturningNull => null;

            #endregion

            #region Object List Item

            [GraphQLStrictNonNullType]
            public Task<SomeObject[]> ObjectListItemReturningNull()
            {
                return Task.FromResult<SomeObject[]>([new("a"), null!, new("c")]);
            }

            [GraphQLStrictNonNullType]
            public Task<SomeObject[]> ObjectListItemThrowingError(IResolverContext context)
            {
                context.ReportError(ErrorBuilder.New().SetMessage("Another error").SetPath(context.Path.Append(1))
                    .Build());
                return Task.FromResult<SomeObject[]>([new("a"), null!, new("c")]);
            }

            [GraphQLStrictNonNullType]
            public Task<SomeObject?[]> NullableObjectListItemReturningNull()
            {
                return Task.FromResult<SomeObject?[]>([new("a"), null, new("c")]);
            }

            [GraphQLStrictNonNullType]
            public SomeObject[] PureObjectListItemReturningNull => [new("a"), null!, new("c")];

            // TODO: This is no longer a pure resolver as soon as it access the IResolverContext, right?
            [GraphQLStrictNonNullType]
            public SomeObject[] PureObjectListItemThrowingError(IResolverContext context)
            {
                context.ReportError(ErrorBuilder.New().SetMessage("Another error").SetPath(context.Path.Append(1))
                    .Build());
                return [new("a"), null!, new("c")];
            }

            [GraphQLStrictNonNullType]
            public SomeObject?[] PureNullableObjectListItemReturningNull => [new("a"), null, new("c")];

            #endregion

            #region Nested Array

            [GraphQLStrictNonNullType]
            public string?[][]? NestedScalarArrayNullableOuterItems()
            {
                return [["a1", null!, "c1"], null!, ["a2", null!, "c2"]];
            }

            [GraphQLStrictNonNullType]
            public string[]?[] NestedScalarArrayNullableMiddleItem()
            {
                return [["a1", null!, "c1"], null!, ["a2", null!, "c2"]];
            }

            #endregion

            [UsePaging]
            [GraphQLStrictNonNullType]
            public string[] ScalarConnection() => new[] { "a", null!, "c" };

            [UsePaging]
            [GraphQLStrictNonNullType]
            public string?[] NullableScalarConnection() => new[] { "a", null, "c" };
        }

        public record SomeObject([property:GraphQLStrictNonNullType] string Property);

        public class Mutation
        {
            [UseMutationConvention(PayloadFieldName = "scalarReturningNull")]
            [GraphQLStrictNonNullType]
            public Task<string> SomeMutationReturningNull() => Task.FromResult<string>(null!);
        }
    }
}
