using System.Reflection;

namespace HotChocolate.Configuration
{
    public class FieldBindingInfo
    {
        public string Name { get; set; }
        public MemberInfo Member { get; set; }
    }
}
