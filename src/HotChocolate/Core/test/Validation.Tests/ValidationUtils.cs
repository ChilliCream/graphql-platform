using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Validation.Types;
using DirectiveLocation = HotChocolate.Types.DirectiveLocation;

namespace HotChocolate.Validation;

public static class ValidationUtils
{
    public static DocumentValidatorContext CreateContext(
        DocumentNode document,
        ISchemaDefinition? schema = null,
        IFeatureCollection? features = null,
        int maxAllowedErrors = 5,
        int maxLocationsPerError = 5)
    {
        schema ??= CreateSchema();

        var context = new DocumentValidatorContext();
        context.Initialize(schema, default, document, maxAllowedErrors, maxLocationsPerError, features);
        return context;
    }

    public static ISchemaBuilder AddDirectiveType(
        this ISchemaBuilder builder,
        string name,
        DirectiveLocation location) =>
        AddDirectiveType(builder, name, location, x => x);

    public static ISchemaBuilder AddDirectiveType(
        this ISchemaBuilder builder,
        string name,
        DirectiveLocation location,
        Func<IDirectiveTypeDescriptor, IDirectiveTypeDescriptor> configure) =>
        builder.AddDirectiveType(new DirectiveType(x =>
            configure(x.Name(name).Location(location))));

    public static ISchemaDefinition CreateSchema()
        => SchemaBuilder.New()
            .ModifyOptions(
                o =>
                {
                    o.EnableDefer = true;
                    o.EnableStream = true;
                })
            .AddQueryType<QueryType>()
            .AddMutationType<MutationType>()
            .AddType<AlienType>()
            .AddType<CatOrDogType>()
            .AddType<CatType>()
            .AddType<DogOrHumanType>()
            .AddType<DogType>()
            .AddType<HumanOrAlienType>()
            .AddType<HumanType>()
            .AddType<PetType>()
            .AddType<BeingType>()
            .AddType<ArgumentsType>()
            .AddSubscriptionType<SubscriptionType>()
            .AddType<ComplexInputType>()
            .AddType<Complex2InputType>()
            .AddType<Complex3InputType>()
            .AddType<InvalidScalar>()
            .AddDirectiveType<ComplexDirective>()
            .AddDirectiveType(new CustomDirectiveType("directive"))
            .AddDirectiveType(new CustomDirectiveType("directive1"))
            .AddDirectiveType(new CustomDirectiveType("directive2"))
            .AddDirectiveType("onMutation", DirectiveLocation.Mutation)
            .AddDirectiveType("onQuery", DirectiveLocation.Query)
            .AddDirectiveType("onSubscription", DirectiveLocation.Subscription)
            .AddDirectiveType("onFragmentDefinition", DirectiveLocation.FragmentDefinition)
            .AddDirectiveType("onVariableDefinition", DirectiveLocation.VariableDefinition)
            .AddDirectiveType("directiveA",
                DirectiveLocation.Field
                | DirectiveLocation.FragmentDefinition)
            .AddDirectiveType("directiveB",
                DirectiveLocation.Field
                | DirectiveLocation.FragmentDefinition)
            .AddDirectiveType("directiveC",
                DirectiveLocation.Field
                | DirectiveLocation.FragmentDefinition)
            .AddDirectiveType("repeatable",
                DirectiveLocation.Field
                | DirectiveLocation.FragmentDefinition,
                x => x.Repeatable())
            .Create();
}
