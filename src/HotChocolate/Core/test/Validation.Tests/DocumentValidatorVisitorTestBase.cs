using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Language;
using HotChocolate.StarWars;
using HotChocolate.Validation.Options;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Validation
{
    public abstract class DocumentValidatorVisitorTestBase
    {
        public DocumentValidatorVisitorTestBase(Action<IValidationBuilder> configure)
        {
            var serviceCollection = new ServiceCollection();

            IValidationBuilder builder = serviceCollection
                .AddValidation()
                .ConfigureValidation(c => c.Modifiers.Add(o => o.Rules.Clear()));
            configure(builder);

            IServiceProvider services = serviceCollection.BuildServiceProvider();
            Rule = services.GetRequiredService<IValidationConfiguration>()
                .GetRules(Schema.DefaultName).First();
            StarWars = SchemaBuilder.New().AddStarWarsTypes().Create();
        }

        protected IDocumentValidatorRule Rule { get; }

        protected ISchema StarWars { get; }

        [Fact]
        public void ContextIsNull()
        {
            // arrange
            DocumentNode query = Utf8GraphQLParser.Parse(@"{ foo }");

            // act
            Action a = () => Rule.Validate(null!, query);

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        public void QueryIsNull()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();

            // act
            Action a = () => Rule.Validate(context, null);

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }

        protected void ExpectValid(string sourceText) => ExpectValid(null, sourceText);

        protected void ExpectValid(ISchema schema, string sourceText)
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext(schema);
            DocumentNode query = Utf8GraphQLParser.Parse(sourceText);
            context.Prepare(query);

            // act
            Rule.Validate(context, query);

            // assert
            Assert.False(context.UnexpectedErrorsDetected);
            Assert.Empty(context.Errors);
        }

        protected void ExpectErrors(string sourceText, params Action<IError>[] elementInspectors) =>
            ExpectErrors(null, sourceText, elementInspectors);

        protected void ExpectErrors(
            ISchema schema,
            string sourceText,
            params Action<IError>[] elementInspectors)
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext(schema);
            DocumentNode query = Utf8GraphQLParser.Parse(sourceText);
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
}
