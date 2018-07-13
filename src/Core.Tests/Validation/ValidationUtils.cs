namespace HotChocolate.Validation
{
    public static class ValidationUtils
    {
        public static Schema CreateSchema()
        {
            return Schema.Create(c =>
            {
                c.RegisterQueryType<QueryType>();
                c.RegisterType<AlientType>();
                c.RegisterType<CatOrDogType>();
                c.RegisterType<CatType>();
                c.RegisterType<DogOrHumanType>();
                c.RegisterType<DogType>();
                c.RegisterType<HumanOrAlienType>();
                c.RegisterType<HumanType>();
                c.RegisterType<PetType>();
                c.RegisterType<ArgumentsType>();
            });
        }
    }
}
