namespace HotChocolate.Types
{
    internal class EnumValueConfig
    {
        private object _value;

        public string Name { get; set; }

        public string Description { get; set; }

        public string DeprecationReason { get; set; }

        public virtual object Value
        {
            get => _value;
            set
            {
                _value = value;
                if (string.IsNullOrEmpty(Name))
                {
                    Name = value.ToString().ToUpperInvariant();
                }
            }
        }
    }
}
