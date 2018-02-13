using System;
using System.Linq;
using Xunit;
using Zeus.Abstractions;

namespace Zeus.Parser.Tests
{
    public class QueryDocumentReaderTests
    {
        [Fact]
        public void SingleOperationWithNoName()
        {
            // arrange
            string query = "{ dogs { name friends { name } } }";

            // act
            QueryDocumentReader reader = new QueryDocumentReader();
            QueryDocument queryDocument = reader.Read(query);

            // assert
            Assert.Equal(1, queryDocument.Operations.Count);

            OperationDefinition operation = queryDocument.Operations.Values.First();
            Assert.Equal(1, operation.SelectionSet.Count);

            Field queryField = operation.SelectionSet.OfType<Field>().First();
            Assert.Equal("dogs", queryField.Name);

            Assert.Equal(2, queryField.SelectionSet.Count);
            Field[] fields = queryField.SelectionSet.OfType<Field>().ToArray();
            Assert.Equal("name", fields.First().Name);
            Assert.Equal("friends", fields.Last().Name);

            Assert.Equal(1, fields.Last().SelectionSet.Count);
            Assert.Equal("name", fields.Last().SelectionSet.OfType<Field>().First().Name);
        }

        [Fact]
        public void SingleOperationWithNoNameAndArguments()
        {
            // arrange
            string query = "query x($a: Int) { dogs(a: $a) { name friends { name } } }";

            // act
            QueryDocumentReader reader = new QueryDocumentReader();
            QueryDocument queryDocument = reader.Read(query);

            // assert
            Assert.Equal(1, queryDocument.Operations.Count);

            OperationDefinition operation = queryDocument.Operations.Values.First();
            Assert.Equal(1, operation.VariableDefinitions.Count);
            Assert.Equal(1, operation.SelectionSet.Count);

            Field queryField = operation.SelectionSet.OfType<Field>().First();
            Assert.Equal("dogs", queryField.Name);
            Assert.Equal(1, queryField.Arguments.Count);
            Assert.IsType<Variable>(queryField.Arguments.First().Value.Value);


            Assert.Equal(2, queryField.SelectionSet.Count);
            Field[] fields = queryField.SelectionSet.OfType<Field>().ToArray();
            Assert.Equal("name", fields.First().Name);
            Assert.Equal("friends", fields.Last().Name);

            Assert.Equal(1, fields.Last().SelectionSet.Count);
            Assert.Equal("name", fields.Last().SelectionSet.OfType<Field>().First().Name);
        }
    }
}