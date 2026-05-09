---
title: Visitors
---

Visitors are traversal APIs for Hot Chocolate's internal GraphQL models. Use them when you need to inspect a parsed document, transform syntax, add a validation rule, report on a schema, or build provider infrastructure around resolved selections.

Most resolver code does not require a custom visitor. For tasks local to a field or schema configuration, prefer resolver arguments, `IResolverContext`, middleware, descriptor APIs, or built-in provider extension points.

## Choosing the Right Visitor

The table below helps you select the appropriate visitor for your scenario:

| Structure                             | API                                                                              | Typical Use                                              | Prefer Another API When                                                  |
| ------------------------------------- | -------------------------------------------------------------------------------- | -------------------------------------------------------- | ------------------------------------------------------------------------ |
| Parsed GraphQL document or SDL        | `SyntaxWalker<TContext>`, `SyntaxVisitor<TContext>`, `SyntaxVisitor.Create(...)` | Collect fields, directives, names, or other syntax facts | You need schema type information during validation                       |
| Parsed document with validation state | `DocumentValidatorVisitor`, `TypeDocumentValidatorVisitor`                       | Reject an operation before execution                     | The result depends on request user, tenant, or time and cannot be cached |
| Syntax tree transform                 | `SyntaxRewriter<TContext>`, `SyntaxRewriter.Create(...)`                         | Rename or remove syntax nodes in tooling                 | You only need to collect information                                     |
| Built schema model                    | `SchemaDefinitionVisitor<TContext>`                                              | Count types, inspect fields, enforce schema conventions  | You need parsed SDL locations or AST nodes                               |
| Mutable schema model                  | `MutableSchemaDefinitionVisitor<TContext>`                                       | Work with `HotChocolate.Types.Mutable` models            | You have a built `ISchemaDefinition`                                     |
| Resolved execution selections         | `SelectionVisitor<TContext>`, `ProjectionVisitor<TContext>`                      | Projection and data provider internals                   | You are writing application resolver logic                               |

The key distinction is the model being traversed. A `FieldNode` from the language AST is different from an `IOutputFieldDefinition` from the schema, and both differ from an execution `Selection`.

## Syntax Visitors for AST Analysis

Syntax visitors are used for parsed GraphQL documents and SDL. The main namespaces are:

```csharp
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
```

`SyntaxVisitor<TContext>` is the base visitor. Its default action is `Skip`, so it does not walk children unless you return `Continue` or specify another default action. `SyntaxWalker<TContext>` derives from it and defaults to `Continue`, making it a better starting point for full-tree analysis.

A visitor instance defines behavior, while a context instance carries per-visit state.

```csharp
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

var document = Utf8GraphQLParser.Parse(
    """
    query GetProduct {
      product(id: 1) {
        name
        price
      }
    }
    """);

var context = new FieldCollectorContext();
new FieldCollector().Visit(document, context);

Console.WriteLine(string.Join(", ", context.FieldNames));

public sealed class FieldCollectorContext
{
    public List<string> FieldNames { get; } = [];
}

public sealed class FieldCollector : SyntaxWalker<FieldCollectorContext>
{
    protected override ISyntaxVisitorAction Enter(
        FieldNode node,
        FieldCollectorContext context)
    {
        context.FieldNames.Add(node.Name.Value);
        return Continue;
    }
}
```

You can start from any `ISyntaxNode`, not only `DocumentNode`. Starting lower in the tree limits which parents, siblings, directives, or arguments are accessible.

For one-off scans, use a delegate visitor:

```csharp
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

var document = Utf8GraphQLParser.Parse(
    """
    query GetProduct {
      product(id: 1) @defer {
        name
      }
    }
    """);

var hasDefer = false;

var visitor = SyntaxVisitor.Create(
    enter: node =>
    {
        if (node is DirectiveNode { Name.Value: "defer" })
        {
            hasDefer = true;
            return SyntaxVisitor.Break;
        }

        return SyntaxVisitor.Continue;
    },
    options: new SyntaxVisitorOptions { VisitDirectives = true });

visitor.Visit(document, null);
```

`SyntaxVisitorOptions` controls which optional children are traversed:

| Option              | Traverses               |
| ------------------- | ----------------------- |
| `VisitNames`        | `NameNode` children     |
| `VisitDescriptions` | SDL description strings |
| `VisitDirectives`   | Directive nodes         |
| `VisitArguments`    | Argument nodes          |

All options default to `false`. If a visitor does not see directives, arguments, descriptions, or names, check the matching option and the node where traversal starts.

## Enter, Leave, and Actions

For each visited syntax node, Hot Chocolate runs the following sequence:

1. `OnBeforeEnter(node, parent, context)`
2. `Enter(node, context)`
3. `OnAfterEnter(node, parent, context, action)`
4. `VisitChildren(node, context)` when the enter action is `Continue`
5. `OnBeforeLeave`, `Leave`, and `OnAfterLeave` when the enter action is `Continue` or `SkipAndLeave`

Actions implement `ISyntaxVisitorAction` and use `SyntaxVisitorActionKind` internally.

| Action         | Visits Children | Runs Leave Hooks for Current Node | Effect                                              |
| -------------- | --------------- | --------------------------------- | --------------------------------------------------- |
| `Continue`     | Yes             | Yes                               | Walk the current node and its children              |
| `Skip`         | No              | No                                | Skip children and continue with the next sibling    |
| `SkipAndLeave` | No              | Yes                               | Skip children, then run current-node leave hooks    |
| `Break`        | No              | No                                | Stop traversal early and propagate the break result |

Use `Skip` to avoid irrelevant subtrees. Use `Break` when the answer is known and further traversal is unnecessary. If you push state while entering a node, ensure the action path also pops it. Return `SkipAndLeave` when cleanup belongs in `Leave`.

## Carrying State in the Context

Visitor instances are often reusable. Store traversal data in `TContext`, not in instance fields that could leak across visits or requests.

Good context data includes:

- Collections for findings
- Stacks for path, type, field, or selection state
- Dictionaries for memoized facts
- Counters and limits for expensive scans
- Cancellation flags if the caller controls cancellation

Provider handlers can be singletons, so the same rule applies. Place request or traversal state on the visitor context.

## Navigating Ancestors and Schema Coordinates

Use `SyntaxVisitor.CreateWithNavigator<TContext>` when a syntax visitor needs access to ancestors. The context must implement `INavigatorContext`; `NavigatorContext` is the built-in implementation.

The navigator manages pushing and popping syntax nodes and provides helpers such as `GetAncestor<T>()` and `CreateCoordinate()`.

```csharp
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

var schema = Utf8GraphQLParser.Parse(
    """
    type Product {
      name(locale: String): String
    }
    """);

var coordinates = new List<string>();

var visitor = SyntaxVisitor.CreateWithNavigator<NavigatorContext>(
    enter: (node, context) =>
    {
        if (node is FieldDefinitionNode or InputValueDefinitionNode)
        {
            coordinates.Add(context.Navigator.CreateCoordinate().ToString());
        }

        return SyntaxVisitor.Continue;
    },
    options: new SyntaxVisitorOptions { VisitArguments = true });

visitor.Visit(schema, new NavigatorContext());
```

Schema coordinates describe SDL and type-system paths, such as `Product.name` or `Product.name(locale:)`. They are not executable response paths.

## Rewriting Syntax Trees

Use `SyntaxRewriter<TContext>` when you need to produce a replacement tree. Syntax nodes are immutable, so a rewrite returns original nodes for unchanged branches and new nodes for changed branches.

```csharp
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

var schema = Utf8GraphQLParser.Parse(
    """
    type Product {
      oldName: String
      sku: String
    }
    """);

var rewriter = SyntaxRewriter.CreateWithNavigator(
    rewrite: (node, context) =>
    {
        if (node is FieldDefinitionNode { Name.Value: "oldName" } field
            && context.Navigator.GetAncestor<ObjectTypeDefinitionNode>()?.Name.Value == "Product")
        {
            return field.WithName(field.Name.WithValue("name"));
        }

        return node;
    });

var rewritten = (DocumentNode?)rewriter.Rewrite(schema, new NavigatorContext());
var sdl = rewritten?.Print();
```

Returning `null` removes a node only when the parent can omit that child, such as in many list positions. Removing a required child can throw `SyntaxNodeCannotBeNullException`. After rewriting SDL or an operation document, use Hot Chocolate printing or formatting APIs rather than string concatenation.

## Custom Validation Visitors

Validation visitors run before execution and receive a `DocumentValidatorContext`. Derive from:

- `DocumentValidatorVisitor` for syntax-oriented rules
- `TypeDocumentValidatorVisitor` when the rule needs current schema type, field, argument, or variable state

Register validation visitors on the request executor builder:

```csharp
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Validation;
using Microsoft.Extensions.DependencyInjection;

services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddValidationVisitor<NoDebugFieldVisitor>(isCacheable: true);

public sealed class NoDebugFieldVisitor : TypeDocumentValidatorVisitor
{
    protected override ISyntaxVisitorAction Enter(
        FieldNode node,
        DocumentValidatorContext context)
    {
        if (node.Name.Value.Equals("debug", StringComparison.Ordinal))
        {
            context.ReportError(
                ErrorBuilder.New()
                    .SetMessage("The field `debug` is not allowed in operations.")
                    .SetCode("FIELD_NOT_ALLOWED")
                    .Build());

            return Skip;
        }

        return Continue;
    }
}
```

Use `AddValidationVisitor<T>(isCacheable: true)` when the result depends only on the schema and document. Set `isCacheable: false` or use a full `IDocumentValidatorRule` through `AddValidationRule<T>()` if the rule depends on request user, tenant, time, services, or request-specific context data.

Validation traversal is not a plain tree walk. `DocumentValidatorVisitor` follows fragment spreads through the fragment context, and validation limits cap fragment visits. Account for repeated fragment entry, avoid unbounded work, and report bounded, specific errors.

## Schema Visitors

`SchemaDefinitionVisitor<TContext>` traverses the built schema model from `HotChocolate.Types`. It is not an enter/leave visitor and does not return visitor actions. Override methods for the parts you need and call base methods when you want the default child traversal.

```csharp
using HotChocolate.Types;

public sealed class SchemaReport
{
    public int ObjectTypeCount { get; set; }
    public int OutputFieldCount { get; set; }
}

public sealed class SchemaReportVisitor : SchemaDefinitionVisitor<SchemaReport>
{
    public override void VisitObjectType(
        IObjectTypeDefinition type,
        SchemaReport context)
    {
        context.ObjectTypeCount++;
        base.VisitObjectType(type, context);
    }

    public override void VisitOutputField(
        IOutputFieldDefinition field,
        SchemaReport context)
    {
        context.OutputFieldCount++;
        base.VisitOutputField(field, context);
    }
}

// new SchemaReportVisitor().VisitSchema(schema, report);
```

The default collection traversal skips introspection types and introspection fields. Use `MutableSchemaDefinitionVisitor<TContext>` only for `HotChocolate.Types.Mutable` models. Do not mix mutable schema visitors with built `ISchemaDefinition` traversal.

## Selection and Projection Visitors

`SelectionVisitor<TContext>` and `ProjectionVisitor<TContext>` are found in `HotChocolate.Data.Projections` and are mainly for data provider authors. They traverse resolved execution selections, not parsed syntax nodes.

Important types include:

- `HotChocolate.Execution.Processing.Selection`
- `ISelectionVisitorContext`
- `IProjectionVisitorContext`
- `ISelectionVisitorAction`

Selection traversal can account for `@skip` and `@include` flags and possible object types for abstract selections. `ProjectionVisitor<TContext>` starts at `context.ResolverContext.Selection` and delegates to projection handlers. For application code, prefer resolver APIs and documented projection, filtering, or sorting extension points.

## Performance and Safety

- Use the narrowest visitor model that contains the state you need
- Return `Skip` for subtrees that cannot affect the result
- Return `Break` after a successful search
- Enable only the needed `SyntaxVisitorOptions` flags
- Keep request-specific state out of reusable visitor or handler instances
- Prefer direct loops in hot visitor methods and avoid unnecessary allocations
- Bound custom validation work and error counts
- Respect parser, validation, and fragment-visit limits
- Prefer schema-time checks over repeated per-request scans when the rule is about schema shape

## Testing Visitors

Test visitors with small documents that clearly demonstrate traversal behavior.

```csharp
using HotChocolate.Language;

var document = Utf8GraphQLParser.Parse(
    """
    query {
      product { name }
    }
    """);

var context = new FieldCollectorContext();
new FieldCollector().Visit(document, context);

Assert.Equal(["product", "name"], context.FieldNames);
```

For rewriters, assert the printed document or use snapshot testing. Include a case that proves unchanged branches remain valid and a case for invalid removals if your rewrite can return `null`.

For validation visitors, test both the reported error and a valid operation. If the rule is cacheable, avoid request-specific data in the assertion setup.

## Troubleshooting

| Problem                                                              | Check                                                                                                                |
| -------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------- |
| The visitor never sees arguments, directives, names, or descriptions | Enable the matching `SyntaxVisitorOptions` flag and start traversal above the owner node                             |
| `Leave` is not called                                                | `Enter` returned `Skip` or `Break`; use `SkipAndLeave` when current-node cleanup belongs in `Leave`                  |
| A delegate visitor stops at the root                                 | `SyntaxVisitor.Create` defaults to `Skip`; return `Continue` or set `defaultAction`                                  |
| Fragment validation repeats work                                     | Validation follows fragment spreads; use fragment context, short-circuit repeated facts, and respect fragment limits |
| State leaks between requests                                         | Move state to `TContext`, create a context per visit, or clear reusable collections                                  |
| A rewriter throws after returning `null`                             | The parent required that child; return a valid replacement or remove only optional/list children                     |
| Syntax nodes have no current schema field or type                    | Use `TypeDocumentValidatorVisitor` during validation or selection APIs after operation planning                      |
| A schema visitor does not see introspection fields                   | Default built-schema traversal skips introspection types and fields                                                  |

## API Quick Reference

**Syntax traversal:**

- `ISyntaxVisitor<TContext>`
- `SyntaxVisitor<TContext>`
- `SyntaxWalker<TContext>`
- `SyntaxVisitor.Create(...)`
- `SyntaxVisitor.CreateWithNavigator(...)`
- `SyntaxVisitorOptions`
- `ISyntaxVisitorAction`
- `SyntaxVisitorActionKind`
- `NavigatorContext`, `INavigatorContext`, `ISyntaxNavigator`

**Syntax rewriting:**

- `ISyntaxRewriter<TContext>`
- `SyntaxRewriter<TContext>`
- `SyntaxRewriter.Create(...)`
- `SyntaxRewriter.CreateWithNavigator(...)`
- `SyntaxNodeCannotBeNullException`

**Validation:**

- `DocumentValidatorVisitor`
- `TypeDocumentValidatorVisitor`
- `DocumentValidatorContext`
- `IDocumentValidatorRule`
- `DocumentValidatorRule`
- `AddValidationVisitor<T>()`
- `AddValidationRule<T>()`

**Schema and provider traversal:**

- `SchemaDefinitionVisitor<TContext>`
- `MutableSchemaDefinitionVisitor<TContext>`
- `SelectionVisitor<TContext>`
- `ProjectionVisitor<TContext>`
- `ISelectionVisitorContext`
- `IProjectionVisitorContext`
- `ISelectionVisitorAction`

## Next Steps

- [Language and AST](/docs/hotchocolate/v16/build/execution-internals/language-and-ast) for syntax node shapes, parsing, and printing
- [Request limits](/docs/hotchocolate/v16/build/security/execution-depth-and-limits) for validation and fragment-visit limits
- [Cost analysis](/docs/hotchocolate/v16/build/security/cost-analysis) for a built-in validation-time analysis feature
- [Extending filtering](/docs/hotchocolate/v16/build/filtering-sorting-projections/extending-filtering) for provider handler patterns and visitor context state
