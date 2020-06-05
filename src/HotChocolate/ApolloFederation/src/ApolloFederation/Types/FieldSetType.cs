using HotChocolate.ApolloFederation.Properties;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.ApolloFederation
{
    // Todo: this is not final, must perform validation on the field set
    public class FieldSetType
        : ScalarType<string, StringValueNode>
    {
        public FieldSetType() 
            : base("_FieldSet", BindingBehavior.Implicit)
        {
            Description = FederationResources.FieldsetType_Description;
        }

        protected override string ParseLiteral(StringValueNode literal) => literal.Value;
        protected override StringValueNode ParseValue(string value) => new StringValueNode(value);
    }
}