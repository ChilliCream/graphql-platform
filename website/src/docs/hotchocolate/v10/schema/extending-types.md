---
title: Custom Base Classes
---

Hot Chocolate is built with extensibility in mind and allows you to customize exiting type base classes and the descriptors.

# Introduction

In order to know how to extend the type system it is important to know how we actually initialize our types. Types in Hot Chocolate are initialized in three phases (create, assign name and complete type). Each phase can be extended.

## Create

The type initializer creates the type instance and the type definition. The type definition contains all information to create and initialize a schema type. After the instance creation step is completed the type instance exists and is associated with a native .net type representation. The native .net type can be `object` but can also be something more specific like `string` or any other .net object. In this phase the type will also register all of its dependencies to other type system objects (types and directives) to the type initializer.

## Assign Name

After all types are initialized the type initializer will start assigning the type names to the type instances. The name of a type can be dependant on another type. This capability is often used when other languages would actually opt for generics.

Let\`s say we have a type `EdgeType<T>` where `T` is another schema type. The resulting concrete type shall construct its name by combining the name of the two types. So, `EdgeType<StringType>` will become `EdgeString` and so on.

## Complete Type

The last phase of the type initialization process will complete the types, this means that the type will manifest in its final form and become immutable. In this final phase the object type for instance builds its fields or the enum type for instance creates its values.

# Extending Types

Hot Chocolate allows to extend types by creating extension methods on specific descriptors or by inheriting from a type base class and overriding the initialization process. Both ways provide unique capabilities depending on what you want to do.

## Extending Descriptors

Each descriptor provides a method called `Extend`. `Extend` returns an extension descriptor which allows us to register some logic with the type initialization pipeline.

The extension descriptor provides extension points the three phases described earlier:

- `OnBeforeCreate` will allow us to customize the type definition. It is important to know that this step is not allowed to be dependent on another type object. Also, at this point you will not have access to the type completion context.

- `OnBeforeNaming` allows to provide logic to generate the name of a type. You can declare two kinds of dependencies in this step, either the dependency has to be named first or the dependency is allowed to be in any state.

- `OnBeforeCompletion` allows to provide further logic that modifies the type definition. For instance, we could be dependent on another type in order to generate fields based on the fields of that other type. You can declare two kinds of dependencies in this step, either the dependency has to be completed first or the dependency is allowed to be in any state.

## Extending Type Base Class

## Custom Context
