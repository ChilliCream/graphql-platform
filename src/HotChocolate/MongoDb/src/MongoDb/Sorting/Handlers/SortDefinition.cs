using MongoDB.Driver;
using System.Linq.Expressions;

namespace HotChocolate.MongoDb.Sorting.Convention.Extensions.Handlers
{
    public class SortDefinition
    {
        public SortDefinition(LambdaExpression lambdaExpression, int direction)
        {
            LambdaExpression = lambdaExpression;
            Direction = direction;
        }

        public LambdaExpression LambdaExpression { get; set; }
        public int Direction { get; }
    }
}
