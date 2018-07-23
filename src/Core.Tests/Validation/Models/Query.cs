namespace HotChocolate.Validation
{
    public class Query
    {
        public Dog GetDog()
        {
            return null;
        }

        public Dog FindDog(ComplexInput complex)
        {
            return null;
        }

        public bool BooleanList(bool[] booleanListArg)
        {
            return true;
        }
    }
}
