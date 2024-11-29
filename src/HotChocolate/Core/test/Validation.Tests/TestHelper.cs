using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Language;
using HotChocolate.Validation.Options;

namespace HotChocolate.Validation;

public static class TestHelper
{
    public static void ExpectValid(
        Action<IValidationBuilder> configure,
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
        ISchema schema,
        Action<IValidationBuilder> configure,
        string sourceText,
        IEnumerable<KeyValuePair<string, object?>>? contextData = null)
    {
        // arrange
        var serviceCollection = new ServiceCollection();

        var builder = serviceCollection
            .AddValidation()
            .ConfigureValidation(c => c.RulesModifiers.Add((_, r) => r.Rules.Clear()));
        configure(builder);

        IServiceProvider services = serviceCollection.BuildServiceProvider();
        var rule =
            services.GetRequiredService<IValidationConfiguration>()
                .GetRules(Schema.DefaultName).First();

        var context = ValidationUtils.CreateContext(schema);
        var query = Utf8GraphQLParser.Parse(sourceText);
        context.Prepare(query);

        context.ContextData = new Dictionary<string, object?>();

        if (contextData is not null)
        {
            foreach (var (key, value) in contextData)
            {
                context.ContextData[key] = value;
            }
        }

        // act
        rule.Validate(context, query);

        // assert
        Assert.False(context.UnexpectedErrorsDetected);
        Assert.Empty(context.Errors);
    }

    public static void ExpectErrors(
        Action<IValidationBuilder> configure,
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
        ISchema schema,
        Action<IValidationBuilder> configure,
        string sourceText,
        IEnumerable<KeyValuePair<string, object>>? contextData = null,
        params Action<IError>[] elementInspectors)
    {
        // arrange
        var serviceCollection = new ServiceCollection();

        var builder = serviceCollection
            .AddValidation()
            .ConfigureValidation(c => c.RulesModifiers.Add((_, r) => r.Rules.Clear()));
        configure(builder);

        IServiceProvider services = serviceCollection.BuildServiceProvider();
        var rule =
            services.GetRequiredService<IValidationConfiguration>()
                .GetRules(Schema.DefaultName).First();

        var context = ValidationUtils.CreateContext(schema);
        context.MaxAllowedErrors = int.MaxValue;

        var query = Utf8GraphQLParser.Parse(sourceText);
        context.Prepare(query);

        context.ContextData = new Dictionary<string, object?>();

        if (contextData is not null)
        {
            foreach (var (key, value) in contextData)
            {
                context.ContextData[key] = value;
            }
        }

        // act
        rule.Validate(context, query);

        // assert
        Assert.NotEmpty(context.Errors);

        if (elementInspectors.Length > 0)
        {
            Assert.Collection(context.Errors, elementInspectors);
        }

        context.Errors.MatchSnapshot();
    }
}
