namespace Generator.ClassGenerator
{
    public class Statement : IClassPart
    {
        private readonly string _statement;

        public Statement(string statement)
        {
            _statement = statement;
        }

        public string Generate()
        {
            return _statement;
            
        }

        public override string ToString()
        {
            return Generate();
        }
    }
}
