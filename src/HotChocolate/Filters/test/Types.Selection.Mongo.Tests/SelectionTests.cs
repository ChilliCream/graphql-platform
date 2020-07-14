using Squadron;
using Xunit;

namespace HotChocolate.Types.Selections
{
    public class SelectionTests
        : SelectionTestsBase
        , IClassFixture<MongoResource>
    {
        public SelectionTests(MongoResource provider)
            : base(new MongoProvider(provider))
        {
        }

        [Fact(Skip = "Not yet supported!")]
        public override void Execute_Selection_Array()
        {
        }

        [Fact(Skip = "Not yet supported!")]
        public override void Execute_Selection_ArrayDeep()
        {
        }

        [Fact(Skip = "Not yet supported!")]
        public override void Execute_Selection_DeepPaging()
        {
        }

        [Fact(Skip = "Not yet supported!")]
        public override void Execute_Selection_List()
        {
        }

        [Fact(Skip = "Not yet supported!")]
        public override void Execute_Selection_List_Interface()
        {
        }
        [Fact(Skip = "Not yet supported!")]
        public override void Execute_Selection_Set()
        {
        }

        [Fact(Skip = "Not yet supported!")]
        public override void Execute_Selection_Set_Interface()
        {
        }

        [Fact(Skip = "Not yet supported!")]
        public override void Execute_Selection_Nested_Filtering()
        {
        }

        [Fact(Skip = "Not yet supported!")]
        public override void Execute_Selection_Nested_FilteringAndSorting()
        {
        }

        [Fact(Skip = "Not yet supported!")]
        public override void Execute_Selection_Nested_Sorting()
        {
        }

        [Fact(Skip = "Not yet supported!")]
        public override void Execute_Selection_SortedSet()
        {
        }

        [Fact(Skip = "Not yet supported!")]
        public override void Execute_Selection_ComputedField()
        {
        }

        [Fact(Skip = "Not yet supported!")]
        public override void Execute_Selection_ComputedFieldLambda()
        {
        }

        [Fact(Skip = "Not yet supported!")]
        public override void Execute_Selection_ComputedFieldParent()
        {
        }

        [Fact(Skip = "Not yet supported!")]
        public override void Execute_Selection_Paging()
        {
        }

        [Fact(Skip = "Not yet supported!")]
        public override void Execute_Selection_Paging_OnlyMeta()
        {
        }
    }
}
