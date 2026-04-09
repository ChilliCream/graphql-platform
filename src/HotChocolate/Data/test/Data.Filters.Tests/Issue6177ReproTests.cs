using HotChocolate.Types;
using HotChocolate.Types.Relay;

namespace HotChocolate.Data.Filters;

public class Issue6177ReproTests
{
    [Fact]
    public void Custom_Id_Operation_Filter_Type_Is_Used_For_Id_Attributed_Field()
        => SchemaBuilder.New()
            .AddFiltering()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("subscriptions")
                    .Resolve(new List<SubscriptionNode>())
                    .UseFiltering<SubscriptionFilterType>())
            .Create()
            .MatchSnapshot();

    [Fact]
    public void Custom_Id_Operation_Filter_Type_Is_Used_When_Configured_With_Instance()
        => SchemaBuilder.New()
            .AddFiltering()
            .AddQueryType(
                d => d
                    .Name("Query")
                    .Field("subscriptions")
                    .Resolve(new List<SubscriptionNode>())
                    .UseFiltering<SubscriptionFilterTypeWithInstance>())
            .Create()
            .MatchSnapshot();

    public class SubscriptionNode
    {
        [ID]
        public int Id { get; set; }
    }

    public class SubscriptionFilterType : FilterInputType<SubscriptionNode>
    {
        protected override void Configure(IFilterInputTypeDescriptor<SubscriptionNode> descriptor)
        {
            descriptor.BindFieldsExplicitly();
            descriptor.Field(f => f.Id).Type<SubscriptionIdOperationFilterInput>();
        }
    }

    public class SubscriptionFilterTypeWithInstance : FilterInputType<SubscriptionNode>
    {
        protected override void Configure(IFilterInputTypeDescriptor<SubscriptionNode> descriptor)
        {
            descriptor.BindFieldsExplicitly();
            descriptor.Field(f => f.Id).Type(new SubscriptionIdOperationFilterInput());
        }
    }

    public class SubscriptionIdOperationFilterInput : IdOperationFilterInputType
    {
        protected override void Configure(IFilterInputTypeDescriptor descriptor)
        {
            descriptor.Operation(DefaultFilterOperations.Equals).Type<IdType>();
            descriptor.Operation(DefaultFilterOperations.In).Type<ListType<IdType>>();
            descriptor.AllowAnd(false).AllowOr(false);
        }
    }
}
