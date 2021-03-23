---
title: Enum Type
---

Enum types consists of a set of named constants called the enumerator list.

```sdl
enum Day {
  SATURDAY
  SUNDAY
  MONDAY
  TUESDAY
  WEDNESDAY
  THURSDAY
  FRIDAY
}
```

Hot Chocolate can bind enumerations to a various set of types. The simplest thing is to bind GraphQL enum types to .NET enum types.

```csharp
public class DayType : EnumType<Day> { }
```

When you bind GraphQL enum types to .NET enum types then Hot Chocolate will infer everything for you. But you can still overwrite what we have inferred by overriding the `Configure` method.

```csharp
public class DayType : EnumType<Day>
{
    protected override void Configure(IEnumTypeDescriptor<Day> descriptor)
    {
        descriptor.Value(Day.Monday)
            .Name("MY_CUSTOM_NAME")
            .Description("This is the description of this value")
            .Directive(new MyCustomDirective());
    }
}
```

You can also bind the enumeration type to any other .NET type.

```csharp
public class DayType : EnumType<string>
{
    protected override void Configure(IEnumTypeDescriptor<string> descriptor)
    {
        descriptor.Value("monday")
            .Name("MY_CUSTOM_NAME")
            .Description("This is the description of this value")
            .Directive(new MyCustomDirective());
    }
}
```
