namespace Zeus
{



    public class Foo
    {
        public void Bar(IResolverBuilder builder)
        {
            IResolverCollection resolvers = builder
                .Add("Query", c => c)
                .Add("Query", "Books", c => c)
                .Add("Query", "DVDs", c => Test(c.Argument<string>("name"), c.Argument<int>("count")))
                .Build();
        }

        public string Test(string a, int x)
        {
            return default(string);
        }
    }


}
