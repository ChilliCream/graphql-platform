using System;
using System.Collections.Generic;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Validation
{
    public abstract class DocumentValidatorVisitorTestBase
    {
        public DocumentValidatorVisitorTestBase(Action<IServiceCollection> configure)
        {
            var serviceCollection = new ServiceCollection();
            configure(serviceCollection);

            IServiceProvider services = serviceCollection.BuildServiceProvider();
            Rule = services.GetRequiredService<IDocumentValidatorRule>();
        }

        protected IDocumentValidatorRule Rule { get; }

        [Fact]
        public void ContextIsNull()
        {
            // arrange
            DocumentNode query = Utf8GraphQLParser.Parse(@"{ foo }");

            // act
            Action a = () => Rule.Validate(null, query);

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

        public void ExpectValid(string sourceText)
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
            DocumentNode query = Utf8GraphQLParser.Parse(sourceText);
            context.Prepare(query);

            // act
            Rule.Validate(context, query);

            // assert
            Assert.Empty(context.Errors);
        }

        public void ExpectFailed(string sourceText, params Action<IError>[] elementInspectors)
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
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
