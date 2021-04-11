---
title: "Custom Attributes"
---

Hot Chocolate allows to define a schema in various ways. When defining schemas with pure .NET types and custom attributes we need a way to access advanced features like custom field middleware that we have at our disposal with schema types.

```csharp
public class QueryType : ObjectType<Query>
{
    protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
    {
        descriptor.Field(t => t.Strings).UsePaging<StringType>();
    }
}
```

This is where descriptor attributes come in. Descriptor attributes allow us to package descriptor configurations into an attribute that can be used to decorate our .NET types. Descriptor attributes act like an interceptor into the configuration of the inferred schema type.

# Built-In Attributes

We have prepared the following set of built-in descriptor attributes.

> ⚠️ **Note:** As middleware comprises the stages of a sequential _pipeline_, the ordering is important. The correct order to use is `UsePaging`, `UseFiltering`, `UseSorting`.

## UsePagingAttribute

The `UsePagingAttribute` allows us to use the paging middleware by annotating it to a property or method.

```csharp
public class Query
{
    [UsePaging]
    public IQueryable<Foo> GetFoos()
    {
        ...
    }
}
```

## UseFilteringAttribute

The `UseFilteringAttribute` allows us to apply the filtering middleware to a property or method.

```csharp
public class Query
{
    [UseFiltering]
    public IQueryable<Foo> GetFoos()
    {
        ...
    }
}
```

> ⚠️ **Note**: Be sure to install the `HotChocolate.Types.Filters` NuGet package.

## UseSortingAttribute

The `UseSortingAttribute` allows us to apply the sorting middleware to a property or method.

```csharp
public class Query
{
    [UseSorting]
    public IQueryable<Foo> GetFoos()
    {
        ...
    }
}
```

> ⚠️ **Note**: Be sure to install the `HotChocolate.Types.Sorting` NuGet package.

## AuthorizeAttribute

The `AuthorizeAttribute` allows to apply the authorize directives to a class, struct, interface, property or method. The attribute will only be applied if the inferred type is an object type.

```csharp
public class Query
{
    [Authorize(Policy = "MyPolicy")]
    public IQueryable<Foo> GetFoos()
    {
        ...
    }
}
```

# Attribute Chaining

Attributes can by default be chained, meaning that the attributes are applied in order from the top one to the bottom one.

The following code ...

```csharp
public class Query
{
    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Foo> GetFoos()
    {
        ...
    }
}
```

... would translate to:

```csharp
public class QueryType
    : ObjectType<Query>
{
    protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
    {
        descriptor.Field(t => t.Foos)
            .UsePaging<ObjectType<Foo>>()
            .UseFiltering()
            .UseSorting();
    }
}
```

# Custom Descriptor Attributes

It is super simple to create custom descriptor attributes and package complex functionality in simple to use attributes.

```csharp
public class SomeMiddlewareAttribute
    : ObjectFieldDescriptorAttribute
{
    public override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo member)
    {
        descriptor.Use(next => context => ...);
    }
}
```

Within the `OnConfigure` method you can do what you actually would do in the `Configure` method of a type.

But you also get some context information about where the configuration was applied to, like you get the member to which the attribute was applied to and you get the descriptor context.

We have one descriptor base class for each first-class descriptor type.

- EnumTypeDescriptorAttribute
- EnumValueDescriptorAttribute
- InputObjectTypeDescriptorAttribute
- InputFieldDescriptorAttribute
- InterfaceTypeDescriptorAttribute
- InterfaceFieldDescriptorAttribute
- ObjectTypeDescriptorAttribute
- ObjectFieldDescriptorAttribute
- UnionTypeDescriptorAttribute
- ArgumentDescriptorAttribute

All of these attribute base classes have already the allowed attribute targets applied. That means that we pre-configured the `ObjectFieldDescriptorAttribute` for instance to be only valid on methods and properties.

If you want to build more complex attributes that can be applied to multiple targets like an interface type and an object type at the same time then you can use our `DescriptorAttribute` base class. This base class is not pre-configured and lets you probe for configuration types.

```csharp
[AttributeUsage(
    AttributeTargets.Property | AttributeTargets.Method,
    Inherited = true,
    AllowMultiple = true)]
public sealed class MyCustomAttribute : DescriptorAttribute
{
    protected override void TryConfigure(
        IDescriptorContext context,
        IDescriptor descriptor,
        ICustomAttributeProvider element)
    {
        if(element is MemberInfo member)
        {
            switch(descriptor)
            {
                case IInterfaceFieldDescriptor interfaceField:
                    // do something ...
                    break;

                case IObjectFieldDescriptor interfaceField:
                    // do something ...
                    break;
            }
        }
    }
}
```

It is simple to use these attributes. Just annotating a type or a property with an attribute will add the packaged functionality. The types can be used in conjunction with schema types or without.
