using System;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;
using HotChocolate.Validation.Options;
using System.Linq;

namespace HotChocolate.Validation
{
    public static class TestHelper
    {
        public static void ExpectValid(
            Action<IValidationBuilder> configure,
            string sourceText)
        {
            ExpectValid(
                ValidationUtils.CreateSchema(),
                configure,
                sourceText);
        }

        public static void ExpectValid(
            ISchema schema,
            Action<IValidationBuilder> configure,
            string sourceText)
        {
            // arrange
            var serviceCollection = new ServiceCollection();

            IValidationBuilder builder = serviceCollection
                .AddValidation()
                .ConfigureValidation(c => c.Modifiers.Add(o => o.Rules.Clear()));
            configure(builder);

            IServiceProvider services = serviceCollection.BuildServiceProvider();
            var rule = services.GetRequiredService<IValidationConfiguration>()
                .GetRules(Schema.DefaultName).First();

            IDocumentValidatorContext context = ValidationUtils.CreateContext(schema);
            DocumentNode query = Utf8GraphQLParser.Parse(sourceText);
            context.Prepare(query);

            // act
            rule.Validate(context, query);

            // assert
            Assert.False(context.UnexpectedErrorsDetected);
            Assert.Empty(context.Errors);
        }

        public static void ExpectErrors(
            Action<IValidationBuilder> configure,
            string sourceText,
            params Action<IError>[] elementInspectors)
        {
            ExpectErrors(
                ValidationUtils.CreateSchema(),
                configure,
                sourceText,
                elementInspectors);
        }

        public static void ExpectErrors(
            ISchema schema,
            Action<IValidationBuilder> configure,
            string sourceText,
            params Action<IError>[] elementInspectors)
        {
            // arrange
            var serviceCollection = new ServiceCollection();

            IValidationBuilder builder = serviceCollection
                .AddValidation()
                .ConfigureValidation(c => c.Modifiers.Add(o => o.Rules.Clear()));
            configure(builder);

            IServiceProvider services = serviceCollection.BuildServiceProvider();
            var rule = services.GetRequiredService<IValidationConfiguration>()
                .GetRules(Schema.DefaultName).First();

            IDocumentValidatorContext context = ValidationUtils.CreateContext(schema);
            DocumentNode query = Utf8GraphQLParser.Parse(sourceText);
            context.Prepare(query);

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
}
