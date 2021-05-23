using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Validation.Types;
using DirectiveLocation = HotChocolate.Types.DirectiveLocation;
using System;

#nullable enable

namespace HotChocolate.Validation
{
    public static class ValidationUtils
    {
        public static DocumentValidatorContext CreateContext(ISchema? schema = null)
        {
            return new() { Schema = schema ?? CreateSchema() };
        }

        public static void Prepare(this IDocumentValidatorContext context, DocumentNode document)
        {
            context.Fragments.Clear();

            for (var i = 0; i < document.Definitions.Count; i++)
            {
                IDefinitionNode definitionNode = document.Definitions[i];
                if (definitionNode.Kind == SyntaxKind.FragmentDefinition)
                {
                    var fragmentDefinition = (FragmentDefinitionNode)definitionNode;
                    context.Fragments[fragmentDefinition.Name.Value] = fragmentDefinition;
                }
            }
        }

        public static void RegisterDirective(
            this ISchemaConfiguration context,
            string name,
            DirectiveLocation location) =>
            RegisterDirective(context, name, location, x => x);

        public static void RegisterDirective(
            this ISchemaConfiguration context,
            string name,
            DirectiveLocation location,
            Func<IDirectiveTypeDescriptor, IDirectiveTypeDescriptor> configure) =>
            context.RegisterDirective(new DirectiveType(x =>
                configure(x.Name(name).Location(location))));

        public static Schema CreateSchema()
        {
            return Schema.Create(c =>
            {
                c.RegisterQueryType<QueryType>();
                c.RegisterMutationType<MutationType>();
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
                c.RegisterType<Complex2InputType>();
                c.RegisterType<Complex3InputType>();
                c.RegisterType<InvalidScalar>();
                c.RegisterDirective<ComplexDirective>();
                c.RegisterDirective(new CustomDirectiveType("directive"));
                c.RegisterDirective(new CustomDirectiveType("directive1"));
                c.RegisterDirective(new CustomDirectiveType("directive2"));
                c.RegisterDirective("onMutation", DirectiveLocation.Mutation);
                c.RegisterDirective("onQuery", DirectiveLocation.Query);
                c.RegisterDirective("onSubscription", DirectiveLocation.Subscription);
                c.RegisterDirective("onFragmentDefinition", DirectiveLocation.FragmentDefinition);
                c.RegisterDirective("onVariableDefinition", DirectiveLocation.VariableDefinition);
                c.RegisterDirective("directiveA",
                     DirectiveLocation.Field | DirectiveLocation.FragmentDefinition);
                c.RegisterDirective("directiveB",
                     DirectiveLocation.Field | DirectiveLocation.FragmentDefinition);
                c.RegisterDirective("directiveC",
                     DirectiveLocation.Field | DirectiveLocation.FragmentDefinition);
                c.RegisterDirective("repeatable",
                     DirectiveLocation.Field | DirectiveLocation.FragmentDefinition,
                     x => x.Repeatable());
            });
        }
    }
}
