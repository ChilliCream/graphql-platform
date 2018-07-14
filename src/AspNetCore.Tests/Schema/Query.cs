namespace HotChocolate.AspNetCore
{
    public class Query
    {
        public Foo GetBasic()
        {
            return new Foo
            {
                A = "1",
                B = "2",
                C = 3
            };
        }

        public Foo GetWithScalarArgument(string a)
        {
            return new Foo
            {
                A = a,
                B = "2",
                C = 3
            };
        }

        public Foo GetWithObjectArgument(Foo b)
        {
            return new Foo
            {
                A = b.A,
                B = "2",
                C = b.C
            };
        }

        public bool GetWithEnum(TestEnum test)
        {
            return true;
        }
    }
}
