using System;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Validation;
using Xunit;

namespace HotChocolate.DependencyInjection
{
    public class RequestExecutorBuilderExtensionsValidationTests
    {
        [Fact]
        public void AddValidationVisitor_1_Builder_Is_Null()
        {
            void Fail() => RequestExecutorBuilderExtensions
                .AddValidationVisitor<MockVisitor>(null!);

            Assert.Throws<ArgumentNullException>(Fail);
        }

        [Fact]
        public void AddValidationVisitor_2_Builder_Is_Null()
        {
            void Fail() => RequestExecutorBuilderExtensions
                .AddValidationVisitor<MockVisitor>(
                    null!,
                    (_, _) => throw new NotImplementedException());

            Assert.Throws<ArgumentNullException>(Fail);
        }

        [Fact]
        public void AddValidationVisitor_2_Factory_Is_Null()
        {
            void Fail() => new ServiceCollection()
                .AddGraphQL()
                .AddValidationVisitor<MockVisitor>(null!);

            Assert.Throws<ArgumentNullException>(Fail);
        }

        [Fact]
        public void AddValidationRuler_1_Builder_Is_Null()
        {
            void Fail() => RequestExecutorBuilderExtensions
                .AddValidationRule<MockRule>(null!);

            Assert.Throws<ArgumentNullException>(Fail);
        }

        [Fact]
        public void AddValidationRule_2_Builder_Is_Null()
        {
            void Fail() => RequestExecutorBuilderExtensions
                .AddValidationRule<MockRule>(
                    null!,
                    (_, _) => throw new NotImplementedException());

            Assert.Throws<ArgumentNullException>(Fail);
        }

        [Fact]
        public void AddValidationRule_2_Factory_Is_Null()
        {
            void Fail() => new ServiceCollection()
                .AddGraphQL()
                .AddValidationRule<MockRule>(null!);

            Assert.Throws<ArgumentNullException>(Fail);
        }

        public class MockVisitor : DocumentValidatorVisitor
        {
        }

        public class MockRule : IDocumentValidatorRule
        {
            public void Validate(IDocumentValidatorContext context, DocumentNode document)
            {
                throw new NotImplementedException();
            }
        }
    }
}
