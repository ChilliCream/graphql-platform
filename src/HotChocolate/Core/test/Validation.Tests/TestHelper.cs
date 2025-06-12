using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Validation.Options;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Validation;

public static class TestHelper
{
    public static void ExpectValid(
        Action<DocumentValidatorBuilder> configure,
        string sourceText,
        IEnumerable<KeyValuePair<string, object?>>? contextData = null)
    {
        ExpectValid(
            ValidationUtils.CreateSchema(),
            configure,
            sourceText,
            contextData);
    }

    public static void ExpectValid(
        ISchemaDefinition schema,
        Action<DocumentValidatorBuilder> configure,
        string sourceText,
        IEnumerable<KeyValuePair<string, object?>>? contextData = null)
    {
        // arrange
        var builder = DocumentValidatorBuilder.New();
        configure(builder);
        var validator = builder.Build();
        var rule = validator.Rules[0];

        var document = Utf8GraphQLParser.Parse(sourceText);
        var context = ValidationUtils.CreateContext(document, schema);

        if (contextData is not null)
        {
            foreach (var (key, value) in contextData)
            {
                context.ContextData[key] = value;
            }
        }

        // act
        rule.Validate(context, document);

        // assert
        Assert.False(context.UnexpectedErrorsDetected);
        Assert.Empty(context.Errors);
    }

    public static void ExpectValid(
        ISchemaDefinition schema,
        Action<DocumentValidatorBuilder> configure,
        string sourceText,
        Action<DocumentValidatorContext> configureContext)
    {
        // arrange
        var builder = DocumentValidatorBuilder.New();
        configure(builder);
        var validator = builder.Build();
        var rule = validator.Rules[0];

        var document = Utf8GraphQLParser.Parse(sourceText);
        var context = ValidationUtils.CreateContext(document, schema);

        configureContext(context);

        // act
        rule.Validate(context, document);

        // assert
        Assert.False(context.UnexpectedErrorsDetected);
        Assert.Empty(context.Errors);
    }

    public static void ExpectErrors(
        Action<DocumentValidatorBuilder> configure,
        string sourceText,
        IEnumerable<KeyValuePair<string, object>>? contextData = null,
        params Action<IError>[] elementInspectors)
    {
        ExpectErrors(
            ValidationUtils.CreateSchema(),
            configure,
            sourceText,
            contextData,
            elementInspectors);
    }

    public static void ExpectErrors(
        ISchemaDefinition schema,
        Action<DocumentValidatorBuilder> configure,
        string sourceText,
        IEnumerable<KeyValuePair<string, object>>? contextData = null,
        params Action<IError>[] elementInspectors)
    {
        // arrange
        var builder = DocumentValidatorBuilder.New();
        configure(builder);
        var validator = builder.Build();
        var rule = validator.Rules[0];

        var document = Utf8GraphQLParser.Parse(sourceText);
        var context = ValidationUtils.CreateContext(document, schema);

        if (contextData is not null)
        {
            foreach (var (key, value) in contextData)
            {
                context.ContextData[key] = value;
            }
        }

        // act
        rule.Validate(context, document);

        // assert
        Assert.NotEmpty(context.Errors);

        if (elementInspectors.Length > 0)
        {
            Assert.Collection(context.Errors, elementInspectors);
        }

        context.Errors.MatchSnapshot();
    }

    public static void ExpectErrors(
        ISchemaDefinition schema,
        Action<DocumentValidatorBuilder> configure,
        string sourceText,
        Action<DocumentValidatorContext> configureContext,
        params Action<IError>[] elementInspectors)
    {
        // arrange
        var builder = DocumentValidatorBuilder.New();
        configure(builder);
        var validator = builder.Build();
        var rule = validator.Rules[0];

        var document = Utf8GraphQLParser.Parse(sourceText);
        var context = ValidationUtils.CreateContext(document, schema);
        configureContext(context);

        // act
        rule.Validate(context, document);

        // assert
        Assert.NotEmpty(context.Errors);

        if (elementInspectors.Length > 0)
        {
            Assert.Collection(context.Errors, elementInspectors);
        }

        context.Errors.MatchSnapshot();
    }
}
