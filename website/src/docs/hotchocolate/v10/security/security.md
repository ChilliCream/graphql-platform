---
title: Security
---

The user of a GraphQL services is given enormous capabilities by crafting his or her queries and defining what data he or she really needs.

This stands in contrast to REST or SOAP have fixed operations that can be tested and the performance impact can be predicted more easily.

This is one of the main features of GraphQL but also poses one of the main challenges for the backend developer since it makes the backend less predictable performance wise.

Hot Chocolate provides you with some basic strategies to make your backend more predictable and protect against queries that have a to high complexity and thus would pose a headache for your backend.

# Pagination Amount

The first and most simple way to protect your api is to define how many items a page can have when you are using pagination. We added for this the scalar type `PaginationAmount`.

```csharp
SchemaBuilder.New()
  .AddType(new PaginationAmountType(50))
  ...
  .Create();
```

After doing this, you'll want to "bind back" `IntType` as the default `int` representation by doing:

```csharp
  .BindClrType<int, IntType>
```

# Execution Timeout

The first strategy and the simplest one is using a timeout to protect your backend against large queries. Basically, if a query exceeds the allowed amount of execution time it will be aborted and a GraphQL error is returned.

_By default a query is limited to 30 seconds._

# Query Depth

Many GraphQL schemas expose cyclic graphs allowing for recursive queries like the following:

```graphql
{
  me {
    friends {
      friends {
        friends {
          friends {
            friends {
              friends {
                friends {
                  friends {
                    friends {
                      #...
                    }
                  }
                }
              }
            }
          }
        }
      }
    }
  }
}
```

Sure, GraphQL queries are finite and there is now way to craft a query that would crawl through your graph forever but you could write or generate a very big query that drills very deep in your graph.

In order to limit the depth of queries you can enable a maximum execution depth and by doing this protect you query against this kind of queries.

It is important to know that the query will be validated before any execution is happening. So, in contrast to the execution timeout which will actually start executing a query the execution depth of a query is validated beforehand.

The query will be rejected when any of the provided operations exceeds the allowed query depth.

# Query Complexity

Query complexity is a very complex and useful tool to make your API secure. The query complexity assigns by default every field a complexity of `1`. The complexity of all fields in one of the operations of a query document is not allowed to be greater than `MaxOperationComplexity` defined in the `QueryExecutionOptions`.

This sounds fairly simple at first, but if you think more about this one, then you start wondering that not every field has an equal complexity. So, you could add a higher complexity to fields that actually pull data, or to list fields and so on.

So, you should really think about what the complexity value of a field is.

The complexity of a field is annotated with the cost directive.

```sdl
type Foo {
  bar: [Bar] @cost(complexity: 5)
}
```

If you want to go even further in computing your complexity you can include multiplier properties. Multiplier, properties are properties that have an impact on how complex the field data loading task is.

Take a paging field for instance in which you can pass the amount of items that you want to load, than you could define that field as a multiplier of your complexity:

```sdl
type Foo {
  bar(take:Int): [Bar] @cost(complexity: 5 multipliers:[take])
}
```

Multipliers are only recognized when you set `UseComplexityMultipliers` in your execution options to `true`. If you opt-in to multipliers the complexity cannot any more be calculated by the validation rules at the beginning of the execution pipeline but has to be calculated after the variables have been coerced since multiplier field arguments could have been provided as variables. This means that we already did some processing and used some more time on the validation.

In both cases, with or without multipliers you can go even further with this one and provide your own multiplier calculation function that for example takes into account the depth of the field. By default we take the complexity as field complexity, or multiply the complexity by the multiplier fields but we give you a lot of context into the complexity calculation function and you could for example multiply the complexity by the depth of the field and so on.

On the `QueryExecutionBuilder` you can call the extension function `AddComplexityCalculation` in order to add you own custom execution function.

```csharp
public delegate int ComplexityCalculation(ComplexityContext context);

public readonly struct ComplexityContext
{
    internal ComplexityContext(
        IOutputField fieldDefinition,
        FieldNode fieldSelection,
        ICollection<IOutputField> path,
        IVariableCollection variables,
        CostDirective cost)
    {
        FieldDefinition = fieldDefinition;
        FieldSelection = fieldSelection;
        Path = path;
        Variables = variables;
        Cost = cost;
    }

    public IOutputField FieldDefinition { get; }
    public FieldNode FieldSelection { get; }
    public ICollection<IOutputField> Path { get; }
    public IVariableCollection Variables { get; }
    public CostDirective Cost { get; }
}
```

All of the execution options are listed [here](/docs/hotchocolate/v10/execution-engine/execution-options/).
