---
title: "Scalars"
---

Hi,

We're currently working on the version 11 documentation. Probably right now at this very moment. However, this is an open-source project, and we need any help we can get! You can jump in at any time and help us improve the documentation for hundreds or even thousands of developers!

In case you might need help, check out our [Slack channel](https://join.slack.com/t/hotchocolategraphql/shared_invite/enQtNTA4NjA0ODYwOTQ0LTViMzA2MTM4OWYwYjIxYzViYmM0YmZhYjdiNzBjOTg2ZmU1YmMwNDZiYjUyZWZlMzNiMTk1OWUxNWZhMzQwY2Q) and get immediate help from the core contributors or the community itself.

Sorry for any inconvenience, and thank you for being patient!

The ChilliCream Team

<br><br><br>

# Additional Scalars
HotChocolate provides additional scalars for more specific usecases. 

To use these scalars you have to add the package `HotChocolate.Types.Scalars`

```csharp
dotnet add package HotChocolate.Types.Scalars
```

These scalars cannot be mapped by HotChocolate to a field. 
You need to specify them manually.

**Annotation Based**
```csharp
public class User 
{
    [GraphQLType(typeof(NonEmptyStringType))]
    public string UserName { get; set; }
}
```

**Code First**
```csharp
public class UserType : ObjectType<User> 
{
    protected override void Configure(
        IObjectTypeDescriptor<User> descriptor)
    {
        descriptor.Field(x => x.UserName).Type<NonEmptyStringType>();
    }
}
```

**Schema First**
```sql
type User {
  userName: NonEmptyString
}
```

You will also have to add the Scalar to the schema: 
```csharp
services
  .AddGraphQLServer()
  // ....
  .AddType<NonEmptyStringType>()
```

## NonEmptyString
```sdl
"""
The NonNullString scalar type represents non empty textual data, represented as UTF‐8 character sequences with at least one character
"""
scalar NonEmptyString
```
