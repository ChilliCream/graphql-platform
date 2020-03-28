using System;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
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
    }
}
