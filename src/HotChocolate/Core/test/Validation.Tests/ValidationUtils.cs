namespace HotChocolate.Validation
{
    public static class ValidationUtils
    {
        public static Schema CreateSchema()
        {
            return Schema.Create(c =>
            {
                c.RegisterQueryType<QueryType>();
                c.RegisterType<AlienType>();
                c.RegisterType<CatOrDogType>();
                c.RegisterType<CatType>();
                c.RegisterType<DogOrHumanType>();
                c.RegisterType<DogType>();
                c.RegisterType<HumanOrAlienType>();
                c.RegisterType<HumanType>();
                c.RegisterType<PetType>();
                c.RegisterType<ArgumentsType>();
                c.RegisterSubscriptionType<SubscriptionType>();
                c.RegisterType<ComplexInputType>();
                c.RegisterType<ComplexInput2Type>();
            });
        }
    }
}
