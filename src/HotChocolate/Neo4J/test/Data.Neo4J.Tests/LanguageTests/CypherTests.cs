using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data.Neo4J.Language
{
    public class CypherTests
    {
        private static Node _bikeNode = Cypher.Node("Bike").Named("b");
        private static Node _userNode = Cypher.Node("User").Named("u");

        public class ReadingAndReturn
        {
            [Fact]
            public void UnrelatedNodes()
            {
                StatementBuilder statement = Cypher.Match(_bikeNode, _userNode, Cypher.Node("U").Named("o"))
                    .Return(_bikeNode, _userNode);
                statement.Build().MatchSnapshot();
            }

            [Fact]
            public void ReturnAsterisk()
            {
                StatementBuilder statement = Cypher.Match(_bikeNode, _userNode, Cypher.Node("U").Named("o"))
                    .Return(Cypher.Asterisk);
                statement.Build().MatchSnapshot();
            }

            [Fact]
            public void AliasedExpressionInReturn()
            {
                StatementBuilder statement = Cypher.Match(_bikeNode)
                    .Return(_bikeNode.As("bike"));
                statement.Build().MatchSnapshot();
            }

            [Fact]
            public void SimpleRelationshipSingleType()
            {
                StatementBuilder statement = Cypher.Match(_userNode.RelationshipTo(_bikeNode, "OWNS"))
                    .Return(_bikeNode, _userNode);
                statement.Build().MatchSnapshot();
            }

            [Fact]
            public void SimpleRelationshipMultipleTypes()
            {
                StatementBuilder statement = Cypher.Match(_userNode.RelationshipTo(_bikeNode, "OWNS", "RIDES"))
                    .Return(_bikeNode, _userNode);
                statement.Build().MatchSnapshot();
            }

            [Fact]
            public void SimpleRelationshipSingleTypeWithProperties()
            {
                StatementBuilder statement = Cypher.Match(
                        _userNode.RelationshipTo(_bikeNode, "OWNS")
                            .WithProperties(Cypher.MapOf("boughtOn", Cypher.LiteralOf("2021-03-02"))))
                    .Return(_bikeNode, _userNode);
                statement.Build().MatchSnapshot();
            }

            [Fact]
            public void SimpleRelationshipSingleTypeWithMinimumLength()
            {
                StatementBuilder statement = Cypher.Match(
                        _userNode.RelationshipTo(_bikeNode, "OWNS").Minimum(3))
                    .Return(_bikeNode, _userNode);
                statement.Build().MatchSnapshot();
            }

            [Fact]
            public void SimpleRelationshipSingleTypeWithMaximumLength()
            {
                StatementBuilder statement = Cypher.Match(
                        _userNode.RelationshipTo(_bikeNode, "OWNS").Maximum(5))
                    .Return(_bikeNode, _userNode);
                statement.Build().MatchSnapshot();
            }

            [Fact]
            public void SimpleRelationshipSingleTypeWithLength()
            {
                StatementBuilder statement = Cypher.Match(
                        _userNode.RelationshipTo(_bikeNode, "OWNS").Length(3,5))
                    .Return(_bikeNode, _userNode);
                statement.Build().MatchSnapshot();
            }

            [Fact]
            public void SimpleRelationshipSingleTypeWithLengthAndProperties()
            {
                StatementBuilder statement = Cypher.Match(
                        _userNode.RelationshipTo(_bikeNode, "OWNS").Named("b1")
                            .Length(3,5)
                            .WithProperties(Cypher.MapOf("boughtOn", Cypher.LiteralOf("2021-03-02"))))
                    .Return(_bikeNode, _userNode);
                statement.Build().MatchSnapshot();
            }

            [Fact]
            public void ChainedRelationshipSingle()
            {
                Node tripNode = Cypher.Node("Trip").Named("t");

                StatementBuilder statement = Cypher
                    .Match(_userNode
                        .RelationshipTo(_bikeNode, "OWNS").Named("r1")
                        .RelationshipTo(tripNode, "USED_ON").Named("r2")
                    )
                    .Return(_bikeNode, _userNode);
                statement.Build().MatchSnapshot();
            }

            [Fact]
            public void ChainedRelationshipMultiple()
            {
                Node tripNode = Cypher.Node("Trip").Named("t");

                StatementBuilder statement = Cypher
                    .Match(_userNode
                        .RelationshipTo(_bikeNode, "OWNS").Named("r1")
                        .RelationshipTo(tripNode, "USED_ON").Named("r2")
                        .RelationshipFrom(_userNode, "WAS_ON").Named("x")
                        .RelationshipBetween(Cypher.Node("SOMETHING")).Named("y")
                    )
                    .Return(_bikeNode, _userNode);
                statement.Build().MatchSnapshot();
            }
        }

        public class ReadingWithWhereAndReturn
        {
            [Fact]
            public void PropertyIsNull()
            {
                StatementBuilder statement = Cypher
                    .Match(_userNode)
                    .Where(_userNode.Property("email").IsNull())
                    .Return(_userNode);
                statement.Build().MatchSnapshot();
            }

            [Fact]
            public void AndCondition()
            {
                StatementBuilder statement = Cypher
                    .Match(_userNode)
                    .Where(_userNode.Property("email").IsEqualTo(Cypher.LiteralOf("user@gmail.com"))
                        .And(_userNode.Property("address").IsNull()))
                    .Return(_userNode);
                statement.Build().MatchSnapshot();
            }

            [Fact]
            public void OrCondition()
            {
                StatementBuilder statement = Cypher
                    .Match(_userNode)
                    .Where(_userNode.Property("email").IsEqualTo(Cypher.LiteralOf("user@gmail.com"))
                        .Or(_userNode.Property("address").IsNull()))
                    .Return(_userNode);
                statement.Build().MatchSnapshot();
            }

            [Fact]
            public void PropertyIsEqual()
            {
                StatementBuilder statement = Cypher
                    .Match(_userNode)
                    .Where(_userNode.Property("email").IsEqualTo(Cypher.LiteralOf("user@gmail.com")))
                    .Return(_userNode);
                statement.Build().MatchSnapshot();
            }
        }

        public class ReadingAndReturnWithProjections
        {
            [Fact]
            public void NodeWithSingleFieldProjection()
            {
                StatementBuilder statement = Cypher
                    .Match(_userNode)
                    .Return(_userNode.Project("email"));
                statement.Build().MatchSnapshot();
            }
        }

        public class MatchNodes
        {
            [Fact]
            public void MatchNamedNode()
            {
                Node movie = Cypher.Node("Movie").Named("m");
                StatementBuilder statement = Cypher.Match(movie).Return(movie);
                statement.Build().MatchSnapshot();
            }

            [Fact]
            public void MatchNamedNodeWithProperties()
            {
                Node movie = Cypher.Node("Movie")
                    .Named("m")
                    .WithProperties(
                        "title", Cypher.LiteralOf("The Matrix"),
                        "yearReleased", Cypher.LiteralOf(1999),
                        "released", Cypher.LiteralOf(true),
                        "rating", Cypher.LiteralOf(8.7)
                    );

                StatementBuilder statement = Cypher.Match(movie);
                statement.Build().MatchSnapshot();
            }

            [Fact]
            public void MatchTwoNamedNode()
            {
                Node movie = Node.Create("Movie").Named("m");
                Node bike = Node.Create("Bike").Named("b");

                StatementBuilder statement = Cypher.Match(movie, bike);
                statement.Build().MatchSnapshot();
            }
        }

        public class Create
        {
            [Fact]
            public void CreateNode()
            {
                StatementBuilder statement = Cypher
                    .Create(_userNode);
                statement.Build().MatchSnapshot();
            }

            [Fact]
            public void CreateNodeWithProperty()
            {
                StatementBuilder statement = Cypher
                    .Create(_userNode.WithProperties("email", Cypher.Null()));
                statement.Build().MatchSnapshot();
            }

            [Fact]
            public void CreateNodeWithProperties()
            {
                StatementBuilder statement = Cypher
                    .Create(_userNode.WithProperties("email", Cypher.Null(), "name", Cypher.LiteralOf("Peter Jackson")));
                statement.Build().MatchSnapshot();
            }
        }
    }
}
