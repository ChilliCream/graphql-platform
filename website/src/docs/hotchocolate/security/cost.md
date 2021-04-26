---
title: Query complexity
---

Query complexity is a useful tool to make your API secure. The query complexity assigns by default every field a complexity of `1`. The complexity of all fields in one of the operations of a GraphQL request is not allowed to be greater than the maximum permitted operation complexity.

# Static Request Analysis

This sounds fairly simple at first, but the more you think about this, the more you wonder if that is so. Does every field have the same complexity?

In a data graph, not every field is the same. We have fields that fetch data that are more expensive than fields that just complete already resolved data.

```graphql
type Query {
  books(take: Int = 10): [Book]
}

type Book {
  title
  author: Author
}

type Author {
  name
}
```

In the above example executing the `books` field on the `Query` type might go to the database and fetch the `Book`. This means that the cost of the `books` field is probably higher than the cost of the `title` field. The cost of the title field might be the impact on the memory and to the transport. For `title`, we can do with the default cost. But for `books`, we might want to go with a higher cost of `10` since we are getting a list here.

Moreover, we have the field `author` on the book, which might go to the database to fetch the `Author` object. Since we are only fetching a single item here, we might want to apply a cost of `5` to this field.

```graphql
type Query {
  books(take: Int = 10): [Book] @cost(complexity: 10)
}

type Book {
  title
  author: Author @cost(complexity: 5)
}

type Author {
  name
}
```

If we run the following query against our data graph, we will come up with the cost of `11`.

```graphql
query {
  books {
    title
  }
}
```

When drilling in further, a cost of `17` occurs.

```graphql
query {
  books {
    title
    author {
      name
    }
  }
}
```

This kind of analysis is entirely static and can just be done by inspecting the query syntax tree. The impact on the overall execution performance is very low. But with this static approach, we do have a very rough idea of the performance. Is it correct to apply always a cost of `10` even though we might get one or one hundred books back?

# Full Request Analysis

The hot chocolate complexity analysis can also take arguments into account when analyzing complexity.

If we look at our data graph, we can see that books actually have an argument that defines how many books are returned. The `take` argument, in this case, specifies the maximum books that the field will return.

When measuring the fields`impact, we can take the argument`take`into account as a multiplier of our cost. This means we might want to lower the cost to`5` since now we get a more fine-grained cost calculation by multiplying the complexity of the field with the take argument.

```graphql
type Query {
  books(take: Int = 10): [Book] @cost(complexity: 5, multipliers:[take])
}

type Book {
  title
  author: Author @cost(complexity: 5)
}

type Author {
  name
}
```

With the multiplier in place, we now get a cost of `60` for the request since the multiplier is applied to the books field and the child fields' cost.

Cost calculation: `(5 * 10) + (1 * 10)`

```graphql
query {
  books {
    title
  }
}
```

When drilling in further, the cost will go up to `110`.

Cost calculation: `(5 * 10) + ((1 + 5) * 10)`

```graphql
query {
  books {
    title
    author {
      name
    }
  }
}
```

```csharp
services
    .AddGraphQL()
    .ModifyRequestOptions(o =>
    {
        o.Enable = true;
        o.Complexity.MaximumAllowed = 1500;
    });
```

# Default Complexity Rules

Hot Chocolate will automatically apply multipliers to fields that enable pagination. Moreover, explicit resolvers and resolvers compiled from async resolvers are by default weighted with `5` to mark them as having more impact than fields that do not fetch data.

These defaults can be configured.

```csharp
services
    .AddGraphQL()
    .ModifyRequestOptions(o =>
    {
        o.Complexity.ApplyDefaults = true;
        o.Complexity.DefaultComplexity = 1;
        o.Complexity.DefaultDataResolverComplexity = 5;
    });
```

# Advanced

Often we not only want to make sure that a consumer of our API does not do too complex queries, but we also want to make sure that the consumer does not issue too many complex queries in a given time window. For this reason, the complexity analysis will store the query complexity on the request context data.

The context data key can be configured like the following:

```csharp
services
    .AddGraphQL()
    .ModifyRequestOptions(o =>
    {
        o.Complexity.ContextDataKey = "MyContextDataKey";
    });
```

With this, it is possible to add a request middleware and aggregate the complexity over time on something like _Redis_ and fail a request if the allowed complexity was used up.

## Custom Complexity Calculation

The default complexity calculation is fairly basic and can be customized to fit your needs.

```csharp
services
    .AddGraphQL()
    .ModifyRequestOptions(o =>
    {
        o.Complexity.Calculation = context =>
        {
            if (context.Multipliers.Count == 0)
            {
                return context.Complexity + context.ChildComplexity;
            }

            var cost = context.Complexity + context.ChildComplexity;
            bool needsDefaultMultiplier = true;

            foreach (MultiplierPathString multiplier in context.Multipliers)
            {
                if (context.TryGetArgumentValue(multiplier, out int value))
                {
                    cost *= value;
                    needsDefaultMultiplier = false;
                }
            }

            if(needsDefaultMultiplier && context.DefaultMultiplier.HasValue)
            {
                cost *= context.DefaultMultiplier.Value;
            }

            return cost;
        });
    });
```

**Complexity Context**

| Member              | Description                                                           |
| ------------------- | --------------------------------------------------------------------- |
| Field               | The `IOutputField` for which the complexity is calculated.            |
| Selection           | The field selection node in the query syntax tree.                    |
| Complexity          | The field`s base complexity.                                          |
| ChildComplexity     | The calculated complexity of all child fields.                        |
| Multipliers         | The multiplier argument names.                                        |
| Multipliers         | The default multiplier value when no multiplier argument has a value. |
| FieldDepth          | The field depth in the query.                                         |
| NodeDepth           | The syntax node depth in the query syntax tree.                       |
| TryGetArgumentValue | Helper to get the coerced argument value of a multiplier.             |
