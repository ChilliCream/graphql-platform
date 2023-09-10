using System;
using HotChocolate.CodeGeneration.Neo4J.Types;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static HotChocolate.CodeGeneration.Neo4J.Neo4JTypeNames;
using static HotChocolate.CodeGeneration.TypeNames;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace HotChocolate.CodeGeneration.Neo4J
{
    public static class Neo4JSyntaxExtensions
    {
        public static MethodDeclarationSyntax AddNeo4JDatabaseAttribute(
            this MethodDeclarationSyntax methodSyntax,
            string databaseName)
        {
            var attribute =
                Attribute(IdentifierName(Global(UseNeo4JDatabaseAttribute)))
                    .AddArgumentListArguments(
                        AttributeArgument(
                            LiteralExpression(
                                SyntaxKind.NumericLiteralExpression,
                                Literal(databaseName)))
                        .WithNameColon(
                            NameColon(IdentifierName(nameof(databaseName)))));

            return methodSyntax.AddAttributeLists(AttributeList(SingletonSeparatedList(attribute)));
        }

        public static PropertyDeclarationSyntax AddNeo4JRelationshipAttribute(
            this PropertyDeclarationSyntax methodSyntax,
            string name,
            RelationshipDirection direction)
        {
            var attribute =
                Attribute(IdentifierName(Global(Neo4JRelationshipAttribute)))
                    .AddArgumentListArguments(
                        AttributeArgument(
                            LiteralExpression(
                                SyntaxKind.NumericLiteralExpression,
                                Literal(name)))
                        .WithNameColon(
                            NameColon(IdentifierName(nameof(name)))),
                        AttributeArgument(
                             MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName(Global(Neo4JRelationshipDirection)),
                                IdentifierName(MapDirection(direction))))
                        .WithNameColon(
                            NameColon(IdentifierName(nameof(direction)))));

            return methodSyntax.AddAttributeLists(AttributeList(SingletonSeparatedList(attribute)));
        }

        private static string MapDirection(RelationshipDirection direction) =>
            direction switch
            {
                RelationshipDirection.In => "Incoming",
                RelationshipDirection.Out => "Outgoing",
                RelationshipDirection.Both => "None",
                _ => throw new NotSupportedException(
                    $"RelationshipDirection {direction} is not supported.")
            };
    }
}
