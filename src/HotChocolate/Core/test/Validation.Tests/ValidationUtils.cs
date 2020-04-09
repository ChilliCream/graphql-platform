using HotChocolate.Language;
using HotChocolate.Types;
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
                c.RegisterType<BeingType>();
                c.RegisterType<ArgumentsType>();
                c.RegisterSubscriptionType<SubscriptionType>();
                c.RegisterType<ComplexInputType>();
                c.RegisterType<ComplexInput2Type>();
                c.RegisterType<ComplexInput3Type>();
                c.RegisterType<InvalidScalar>();
                c.RegisterDirective<ComplexDirective>();
                c.RegisterDirective(new CustomDirectiveType("directive"));
                c.RegisterDirective(new CustomDirectiveType("directive1"));
                c.RegisterDirective(new CustomDirectiveType("directive2"));
            });
        }
    }
}
