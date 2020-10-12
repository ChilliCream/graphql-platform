using HotChocolate.Language;

namespace HotChocolate.ApolloFederation
{
    public class Representation
    {
        public NameString Typename { get; set; }
        public ObjectValueNode Data { get; set; }
    }
}
