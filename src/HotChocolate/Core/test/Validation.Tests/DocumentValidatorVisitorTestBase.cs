using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.StarWars;

namespace HotChocolate.Validation;

public abstract class DocumentValidatorVisitorTestBase
{
    protected DocumentValidatorVisitorTestBase(Action<DocumentValidatorBuilder> configure)
    {
        var builder = DocumentValidatorBuilder.New();
        configure(builder);
        Rule = builder.Build().Rules[0];

        StarWars = SchemaBuilder.New()
            .AddStarWarsTypes()
            .Create();
    }

    protected IDocumentValidatorRule Rule { get; }

    protected ISchemaDefinition StarWars { get; }

    /// <summary>
    /// Parser options that enable the experimental fragment spread arguments syntax.
    /// </summary>
    protected static ParserOptions FragmentArgumentParserOptions { get; } =
        new(new ParserOptionsExperimental(allowFragmentArguments: true));

    protected void ExpectValid(
        [StringSyntax("graphql")] string sourceText)
        => ExpectValid(null, sourceText, ParserOptions.Default);

    protected void ExpectValid(
        [StringSyntax("graphql")] string sourceText,
        ParserOptions parserOptions)
        => ExpectValid(null, sourceText, parserOptions);

    protected void ExpectValid(
        ISchemaDefinition? schema,
        [StringSyntax("graphql")] string sourceText)
        => ExpectValid(schema, sourceText, ParserOptions.Default);

    protected void ExpectValid(
        ISchemaDefinition? schema,
        [StringSyntax("graphql")] string sourceText,
        ParserOptions parserOptions)
    {
        // arrange
        var document = Utf8GraphQLParser.Parse(sourceText, parserOptions);
        var context = ValidationUtils.CreateContext(document, schema);

        // act
        Rule.Validate(context, document);

        // assert
        Assert.False(context.UnexpectedErrorsDetected);
        Assert.Empty(context.Errors);
    }

    protected void ExpectErrors(
        [StringSyntax("graphql")] string sourceText,
        params Action<IError>[] elementInspectors)
        => ExpectErrors(null, sourceText, ParserOptions.Default, elementInspectors);

    protected void ExpectErrors(
        [StringSyntax("graphql")] string sourceText,
        ParserOptions parserOptions,
        params Action<IError>[] elementInspectors)
        => ExpectErrors(null, sourceText, parserOptions, elementInspectors);

    protected void ExpectErrors(
        ISchemaDefinition? schema,
        [StringSyntax("graphql")] string sourceText,
        params Action<IError>[] elementInspectors)
        => ExpectErrors(schema, sourceText, ParserOptions.Default, elementInspectors);

    protected void ExpectErrors(
        ISchemaDefinition? schema,
        [StringSyntax("graphql")] string sourceText,
        ParserOptions parserOptions,
        params Action<IError>[] elementInspectors)
    {
        // arrange
        var document = Utf8GraphQLParser.Parse(sourceText, parserOptions);
        var context = ValidationUtils.CreateContext(
            document,
            schema,
            maxAllowedErrors: int.MaxValue);

        // act
        Rule.Validate(context, document);

        // assert
        Assert.NotEmpty(context.Errors);

        if (elementInspectors.Length > 0)
        {
            Assert.Collection(context.Errors, elementInspectors);
        }

        var snapshot = Snapshot.Create();

        foreach (var error in context.Errors)
        {
            snapshot.Add(error);
        }

        snapshot.Match();
    }
}
