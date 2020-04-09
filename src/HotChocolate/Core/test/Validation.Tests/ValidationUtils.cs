using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Language;
using HotChocolate.Validation.Types;

#nullable enable

namespace HotChocolate.Validation
{
    public static class ValidationUtils
    {
        public static IDocumentValidatorContext CreateContext(ISchema? schema = null)
        {
            return new DocumentValidatorContext { Schema = schema ?? CreateSchema() };
        }

        public static void Prepare(this IDocumentValidatorContext context, DocumentNode document)
        {
            context.Fragments.Clear();

            for (int i = 0; i < document.Definitions.Count; i++)
            {
                IDefinitionNode definitionNode = document.Definitions[i];
                if (definitionNode.Kind == NodeKind.FragmentDefinition)
                {
                    var fragmentDefinition = (FragmentDefinitionNode)definitionNode;
                    context.Fragments[fragmentDefinition.Name.Value] = fragmentDefinition;
                }
            }
        }

        public static void RegisterDirective(
            this ISchemaConfiguration context,
            string name,
            Types.DirectiveLocation location)
                => context.RegisterDirective(
                    new DirectiveType(x => x.Name(name).Location(location)));

        public static Schema CreateSchema()
        {
            return Schema.Create(c =>
            {
                c.RegisterQueryType<QueryType>();
                c.RegisterType<AlienType>();
                c.RegisterType<CatOrDogType>();
                c.RegisterType<CatType>();
                c.RegisterType<DogOrHumanType>();
                c.RegisterType<DogType>();
                c.RegisterType<HumanOrAlienType>();
                c.RegisterType<HumanType>();
                c.RegisterType<PetType>();
                c.RegisterType<ArgumentsType>();
                c.RegisterSubscriptionType<SubscriptionType>();
                c.RegisterType<ComplexInputType>();
                c.RegisterType<ComplexInput2Type>();
                c.RegisterType<ComplexInput3Type>();
                c.RegisterType<InvalidScalar>();
                c.RegisterDirective<ComplexDirective>();
                c.RegisterDirective("onMutation", Types.DirectiveLocation.Mutation);
                c.RegisterDirective("onQuery", Types.DirectiveLocation.Query);
                c.RegisterDirective("onSubscription", Types.DirectiveLocation.Subscription);
                c.RegisterDirective("onFragmentDefinition", Types.DirectiveLocation.FragmentDefinition);
                c.RegisterDirective("onVariableDefinition", Types.DirectiveLocation.VariableDefinition);
            });
        }
    }
}
