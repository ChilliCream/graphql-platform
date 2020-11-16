---
title: Parser
---

The Hot Chocolate parser is a port from the `graphql-js` reference implementation. We are constantly updating the lexer and parser to keep up with new spec features in order to keep it the fastest and most feature complete GraphQL parser in .NET.

# Getting Started

If you want to build GraphQL tooling for .NET or your own type system and query-engine feel free to built on top of the Hot Chocolate parser.

In order to use the parser, install the following package:

```bash
dotnet add package HotChocolate.Language
```

In order to parse a GraphQL schema or query use it like the following:

```csharp
Parser parser = new Parser();
DocumentNode document = parser.Parse("{ x { y } }");
```

We have created some visitor classes in order to make it easy to traverse the parsed syntax nodes.

# Important Base Classes

Here are some important base classes:

## SyntaxVisitor

The `SyntaxVisitor` provides the basic visitation methods without any functionality to traverse the tree.

## SyntaxWalkerBase

The `SyntaxWalkerBase` built upon the `SyntaxVisitor` and adds basic functionality like `VisitMany` for traversing syntax nodes.

## SchemaSyntaxWalker

The `SchemaSyntaxWalker` built upon the `SyntaxWalkerBase` and adds functionality to automatically traverse type system syntax nodes. This syntax walker ignores query syntax nodes.

In order to visit a specific type definition syntax node override the related visitation node and add your code. If you want the syntax walker to keep traversing after your code has been executed invoke the original method implementation of the visitation-method after or before your code.

```csharp
protected override void VisitDirectiveDefinition(
    DirectiveDefinitionNode node,
    YourContextType context)
{
    _visited.Add(nameof(VisitDirectiveDefinition));
    base.VisitDirectiveDefinition(node);
}
```

## SchemaSerializer

The `SchemaSerializer` is built upon the `SchemaSyntaxWalker` and serializes specific type definition syntax nodes to a GraphQL SDL. So, it is basically doing the reverse of the parser. With this you are able to modify a syntax graph and than serializing it back to a GraphQL string.

## QuerySyntaxWalker

The `QuerySyntaxWalker` built upon the `SyntaxWalkerBase` and adds functionality to automatically traverse query syntax nodes. This syntax walker ignores type system syntax nodes.

## QuerySerializer

The `QuerySerializer` is built upon the `QuerySyntaxWalker` and serializes query syntax nodes to a GraphQL query string. With this you are able to modify a syntax graph and than serialize it back to a GraphQL string.

We are also providing a set of rewriter base classes that basically represent a visitor that produces a new graph by visiting the various nodes.

# What's Coming Next

We have started work on our high-performance parser that will boost stitching performance as well as normal execution of queries.
