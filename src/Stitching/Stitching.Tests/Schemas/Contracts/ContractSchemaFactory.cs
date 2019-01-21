namespace HotChocolate.Stitching.Schemas.Contracts
{
    public static class ContractSchemaFactory
    {
        public static ISchema Create()
        {
            return Schema.Create(c =>
            {
                c.RegisterQueryType<QueryType>();
                c.RegisterType<LifeInsuranceContractType>();
                c.RegisterType<SomeOtherContractType>();
                c.UseGlobalObjectIdentifier();
            });
        }
    }
}
