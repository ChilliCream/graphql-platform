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

        public Dog FindDog2(ComplexInput2 complex)
        {
            return null;
        }

        public bool BooleanList(bool[] booleanListArg)
        {
            return true;
        }

        public Human GetHuman()
        {
            return null;
        }

        public Human GetPet()
        {
            return null;
        }

        public object GetCatOrDog()
        {
            return null;
        }

        public object GetDogOrHuman()
        {
            return null;
        }

        public string[] GetStringList()
        {
            return null;
        }

        public string GetFieldWithArg(
            string arg,
            string arg1,
            string arg2,
            string arg3,
            string arg4)
        {
            return null;
        }
    }
}
