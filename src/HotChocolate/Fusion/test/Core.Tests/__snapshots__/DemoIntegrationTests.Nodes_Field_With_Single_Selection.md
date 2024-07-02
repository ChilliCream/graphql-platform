# Nodes_Field_With_Single_Selection

## Result

```json
{
  "errors": [
    {
      "message": "Unexpected Execution Error",
      "extensions": {
        "message": "The given key 'id' was not present in the dictionary.",
        "stackTrace": "   at System.Collections.Generic.Dictionary`2.get_Item(TKey key)\n   at HotChocolate.Fusion.Execution.Nodes.ResolveNode.OnExecuteNodesAsync(FusionExecutionContext context, RequestState state, CancellationToken cancellationToken) in /Users/tobiastengler/src/graphql-platform/src/HotChocolate/Fusion/src/Core/Execution/Nodes/ResolveNode.cs:line 58\n   at HotChocolate.Fusion.Execution.Nodes.QueryPlanNode.ExecuteAsync(FusionExecutionContext context, CancellationToken cancellationToken) in /Users/tobiastengler/src/graphql-platform/src/HotChocolate/Fusion/src/Core/Execution/Nodes/QueryPlanNode.cs:line 56\n   at HotChocolate.Fusion.Execution.Nodes.QueryPlanNode.OnExecuteNodesAsync(FusionExecutionContext context, RequestState state, CancellationToken cancellationToken) in /Users/tobiastengler/src/graphql-platform/src/HotChocolate/Fusion/src/Core/Execution/Nodes/QueryPlanNode.cs:line 73\n   at HotChocolate.Fusion.Execution.Nodes.QueryPlanNode.ExecuteAsync(FusionExecutionContext context, CancellationToken cancellationToken) in /Users/tobiastengler/src/graphql-platform/src/HotChocolate/Fusion/src/Core/Execution/Nodes/QueryPlanNode.cs:line 56\n   at HotChocolate.Fusion.Execution.Nodes.QueryPlan.ExecuteAsync(FusionExecutionContext context, CancellationToken cancellationToken) in /Users/tobiastengler/src/graphql-platform/src/HotChocolate/Fusion/src/Core/Execution/Nodes/QueryPlan.cs:line 185"
      }
    }
  ],
  "data": {}
}
```

## Request

```graphql
{
  nodes(ids: [ "UHJvZHVjdDox", "UHJvZHVjdDoy", "UHJvZHVjdDoz" ]) {
    ... on Product {
      id
      name
    }
  }
}
```

## QueryPlan Hash

```text
EAA83C4EF2B241A44B25DD3B0E8B787CBDE49AEC
```

## QueryPlan

```json
{
  "document": "{ nodes(ids: [ \u0022UHJvZHVjdDox\u0022, \u0022UHJvZHVjdDoy\u0022, \u0022UHJvZHVjdDoz\u0022 ]) { ... on Product { id name } } }",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "ResolveNode",
        "selectionId": 0,
        "responseName": "nodes",
        "branches": [
          {
            "type": "Product",
            "node": {
              "type": "Resolve",
              "subgraph": "Products",
              "document": "query fetch_nodes_1 { nodes(ids: [ \u0022UHJvZHVjdDox\u0022, \u0022UHJvZHVjdDoy\u0022, \u0022UHJvZHVjdDoz\u0022 ]) { ... on Product { id name __typename } } }",
              "selectionSetId": 0
            }
          },
          {
            "type": "ProductBookmark",
            "node": {
              "type": "Resolve",
              "subgraph": "Products",
              "document": "query fetch_nodes_2 { nodes(ids: [ \u0022UHJvZHVjdDox\u0022, \u0022UHJvZHVjdDoy\u0022, \u0022UHJvZHVjdDoz\u0022 ]) { ... on ProductBookmark { __typename } } }",
              "selectionSetId": 0
            }
          },
          {
            "type": "ProductConfiguration",
            "node": {
              "type": "Resolve",
              "subgraph": "Products",
              "document": "query fetch_nodes_3 { nodes(ids: [ \u0022UHJvZHVjdDox\u0022, \u0022UHJvZHVjdDoy\u0022, \u0022UHJvZHVjdDoz\u0022 ]) { ... on ProductConfiguration { __typename } } }",
              "selectionSetId": 0
            }
          }
        ]
      },
      {
        "type": "Compose",
        "selectionSetIds": [
          0
        ]
      }
    ]
  }
}
```

