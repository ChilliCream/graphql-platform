using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Language;
using HotChocolate.Validation.Options;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Validation
{
    public static class TestHelper
    {
        public static void ExpectValid(
            Action<IValidationBuilder> configure,
            string sourceText,
            IEnumerable<KeyValuePair<string, object>> contextData = null)
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
            IEnumerable<KeyValuePair<string, object>> contextData = null)
        {
            // arrange
            var serviceCollection = new ServiceCollection();

            IValidationBuilder builder = serviceCollection
                .AddValidation()
                .ConfigureValidation(c => c.Modifiers.Add(o => o.Rules.Clear()));
            configure(builder);

            IServiceProvider services = serviceCollection.BuildServiceProvider();
            IDocumentValidatorRule rule =
                services.GetRequiredService<IValidationConfiguration>()
                    .GetRules(Schema.DefaultName).First();

            DocumentValidatorContext context = ValidationUtils.CreateContext(schema);
            DocumentNode query = Utf8GraphQLParser.Parse(sourceText);
            context.Prepare(query);

            context.ContextData = new Dictionary<string, object>();

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
            IEnumerable<KeyValuePair<string, object>> contextData = null,
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
            IEnumerable<KeyValuePair<string, object>> contextData = null,
            params Action<IError>[] elementInspectors)
        {
            // arrange
            var serviceCollection = new ServiceCollection();

            IValidationBuilder builder = serviceCollection
                .AddValidation()
                .ConfigureValidation(c => c.Modifiers.Add(o => o.Rules.Clear()));
            configure(builder);

            IServiceProvider services = serviceCollection.BuildServiceProvider();
            IDocumentValidatorRule rule =
                services.GetRequiredService<IValidationConfiguration>()
                    .GetRules(Schema.DefaultName).First();

            DocumentValidatorContext context = ValidationUtils.CreateContext(schema);
            context.MaxAllowedErrors = int.MaxValue;

            DocumentNode query = Utf8GraphQLParser.Parse(sourceText);
            context.Prepare(query);

            context.ContextData = new Dictionary<string, object>();

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
}
