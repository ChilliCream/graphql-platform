using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class Issue8325ProbeTests
{
    [Fact]
    public async Task InputParser_Does_Not_Set_Unspecified_Properties()
    {
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Issue8325Query>()
            .AddType<Issue8325SortInputType>()
            .BuildRequestExecutorAsync();

        var result = await executor.ExecuteAsync(
            """
            {
              isOnlyIdSet(order: [{ id: ASC }])
            }
            """);

        var json = result.ToJson();
        Assert.DoesNotContain("\"errors\"", json);
        Assert.Contains("\"isOnlyIdSet\": true", json);
    }

    [Fact]
    public async Task InputParser_Sets_Explicit_Null_Properties()
    {
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Issue8325Query>()
            .AddType<Issue8325SortInputType>()
            .BuildRequestExecutorAsync();

        var result = await executor.ExecuteAsync(
            """
            {
              isIdAndTitleSet(order: [{ id: ASC, title: null }])
            }
            """);

        var json = result.ToJson();
        Assert.DoesNotContain("\"errors\"", json);
        Assert.Contains("\"isIdAndTitleSet\": true", json);
    }

    public sealed class Issue8325Query
    {
        public bool IsOnlyIdSet(IReadOnlyList<Issue8325SortInput> order)
        {
            var sort = order[0];
            return sort.IsIdSet && !sort.IsTitleSet && sort.Id is Issue8325SortOperation.ASC && sort.Title is null;
        }

        public bool IsIdAndTitleSet(IReadOnlyList<Issue8325SortInput> order)
        {
            var sort = order[0];
            return sort.IsIdSet && sort.IsTitleSet && sort.Id is Issue8325SortOperation.ASC && sort.Title is null;
        }
    }

    public sealed class Issue8325SortInput
    {
        private bool _set_id;
        private Issue8325SortOperation? _value_id;
        private bool _set_title;
        private Issue8325SortOperation? _value_title;

        public Issue8325SortOperation? Id
        {
            get => _value_id;
            set
            {
                _set_id = true;
                _value_id = value;
            }
        }

        public Issue8325SortOperation? Title
        {
            get => _value_title;
            set
            {
                _set_title = true;
                _value_title = value;
            }
        }

        public bool IsIdSet => _set_id;

        public bool IsTitleSet => _set_title;
    }

    public enum Issue8325SortOperation
    {
        ASC,
        DESC
    }

    public sealed class Issue8325SortInputType : InputObjectType<Issue8325SortInput>
    {
        protected override void Configure(IInputObjectTypeDescriptor<Issue8325SortInput> descriptor)
        {
            descriptor.Name("Issue8325SortInput");
            descriptor.BindFieldsExplicitly();
            descriptor.Field(t => t.Id).Type<EnumType<Issue8325SortOperation>>();
            descriptor.Field(t => t.Title).Type<EnumType<Issue8325SortOperation>>();
        }
    }
}
