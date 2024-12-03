using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Language;
using HotChocolate.StarWars;
using HotChocolate.Validation.Options;

namespace HotChocolate.Validation;

public abstract class DocumentValidatorVisitorTestBase
{
    protected DocumentValidatorVisitorTestBase(Action<IValidationBuilder> configure)
    {
        var serviceCollection = new ServiceCollection();

        var builder = serviceCollection
            .AddValidation()
            .ConfigureValidation(c => c.RulesModifiers.Add((_, r) => r.Rules.Clear()))
            .ModifyValidationOptions(o => o.MaxAllowedErrors = int.MaxValue);
        configure(builder);

        IServiceProvider services = serviceCollection.BuildServiceProvider();

        Rule = services
            .GetRequiredService<IValidationConfiguration>()
            .GetRules(Schema.DefaultName).First();

        StarWars = SchemaBuilder.New()
            .AddStarWarsTypes()
            .ModifyOptions(o => o.EnableOneOf = true)
            .Create();
    }

    protected IDocumentValidatorRule Rule { get; }

    protected ISchema StarWars { get; }

    [Fact]
    public void ContextIsNull()
    {
        // arrange
        var query = Utf8GraphQLParser.Parse(@"{ foo }");

        // act
        var a = () => Rule.Validate(null!, query);

        // assert
        Assert.Throws<ArgumentNullException>(a);
    }

    [Fact]
    public void QueryIsNull()
    {
        // arrange
        IDocumentValidatorContext context = ValidationUtils.CreateContext();

        // act
        var a = () => Rule.Validate(context, null!);

        // assert
        Assert.Throws<ArgumentNullException>(a);
    }

    protected void ExpectValid(string sourceText) => ExpectValid(null, sourceText);

    protected void ExpectValid(ISchema? schema, string sourceText)
    {
        // arrange
        IDocumentValidatorContext context = ValidationUtils.CreateContext(schema);
        var query = Utf8GraphQLParser.Parse(sourceText);
        context.Prepare(query);

        // act
        Rule.Validate(context, query);

        // assert
        Assert.False(context.UnexpectedErrorsDetected);
        Assert.Empty(context.Errors);
    }

    protected void ExpectErrors(string sourceText, params Action<IError>[] elementInspectors)
        => ExpectErrors(null, sourceText, elementInspectors);

    protected void ExpectErrors(
        ISchema? schema,
        string sourceText,
        params Action<IError>[] elementInspectors)
    {
        // arrange
        var context = ValidationUtils.CreateContext(schema);
        context.MaxAllowedErrors = int.MaxValue;
        var query = Utf8GraphQLParser.Parse(sourceText);
        context.Prepare(query);

        // act
        Rule.Validate(context, query);

        // assert
        Assert.NotEmpty(context.Errors);

        if (elementInspectors.Length > 0)
        {
            Assert.Collection(context.Errors, elementInspectors);
        }

        context.Errors.MatchSnapshot();
    }
}
