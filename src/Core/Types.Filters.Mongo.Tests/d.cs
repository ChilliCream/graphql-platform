
namespace HotChocolate.Types.Filters
{
    public class MongoFilterTests
    {
        public void Test123()
        {
            MongoClient client = new MongoClient();
            IMongoDatabase database = client.GetDatabase("abc");




        }

        public class Model
        {
            public string Foo { get; set; }
            public int Bar { get; set; }
            public bool Baz { get; set; }
        }
    }
}
