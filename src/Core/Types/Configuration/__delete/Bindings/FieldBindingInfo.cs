using System.Reflection;

namespace HotChocolate.Configuration
{
    internal class FieldBindingInfo
    {
        public string Name { get; set; }
        public MemberInfo Member { get; set; }
    }
}
