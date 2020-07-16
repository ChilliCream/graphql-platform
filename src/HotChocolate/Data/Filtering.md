# Filtering

On issue with the design of filtering was that it was not clear how we define handlers for operations. How van the user define a handler for the string equal filter method? 

We should consider following cases:
## Case 1:  
```csharp
public class Foo { 
    public string Bar { get; set; } 
    public int Baz { get; set; } 
}    
```
```graphql
type FooFilterType {
    bar: StringOperationType
    baz: IntOperationType 
}

```



```csharp
public class Foo { 
    public string Bar { get; set; } 
}    
```

```graphql
{
    foo {
        bar {
            username: {
                like: "Foo"

            }

        }
    }
    baz {
        bar {
            username
        }
    }
}
```

```graphql
{
    baz {
        bar {
            username
        }
    }
}
```
