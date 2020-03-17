---
path: "/blog/2019/12/26/hot-chocolate-10.3.0"
date: "2020-03-18"
title: "Get started with Hot Chocolate and Entity Framework"
author: Michael Staib
authorURL: https://github.com/michaelstaib
authorImageURL: https://avatars1.githubusercontent.com/u/9714350?s=100&v=4
---

![Hot Chocolate](/img/blog/hotchocolate-banner.svg)

In this post I will walk you through how to build a GraphQL Server using _Hot Chocolate_ and _Entity Framework_.

_Entity Framework_ is an OR-mapper from Microsoft that implements the unit-of-work pattern. This basically means that with _Entity Framework_ we work against a `DBContext` and once in a while commit the changes aggregated in the context to the database by invoking `SaveChanges` on the context. With _Entity Framework_ we can write database queries with _Linq_ and do not have deal with _SQL_ directly which many developers prefer.

<!--truncate-->

## Introduction

This tutorial uses the Contoso University sample application used by Microsoft to demonstrate the usage of _Entity Framework_ with ASP.NET Core. The sample application is a simple GraphQL server for the university website. With it, you can query and update student, course, and instructor information.

Before we get started let us setup our server project.

```bash
mkdir ContosoUniversity
dotnet new web
```

Next wee need to add _Entity Framework_ to our project.

```bash
dotnet add package Microsoft.EntityFrameworkCore
```

Last but not least we are adding the SQLLite _Entity Framework_ provided in order to have a lightweight database.

```bash
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
```

For our data we have three models representing the student, the enrollments and the courses. 

The student has some basic data about the person like the first name or the last name and the date when the student first enrolled into the university.

The enrollment entity represents the enrollment of a student to a specific course. The enrollment not only represents the relationship but also holds the Grade that a student achieved in that course. 

Last but not leas we have the course to which many students can be enroll to.

Lets copy our models into the project.

```csharp
using System;
using System.Collections.Generic;

namespace ContosoUniversity
{
    public class Student
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int ID { get; set; }
        public string LastName { get; set; }
        public string FirstMidName { get; set; }
        public DateTime EnrollmentDate { get; set; }

        public virtual ICollection<Enrollment> Enrollments { get; set; }
    }

    public enum Grade
    {
        A, B, C, D, F
    }

    public class Enrollment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int EnrollmentID { get; set; }
        public int CourseID { get; set; }
        public int StudentID { get; set; }
        public Grade? Grade { get; set; }

        public virtual Course Course { get; set; }
        public virtual Student Student { get; set; }
    }

    public class Course
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int CourseID { get; set; }
        public string Title { get; set; }
        public int Credits { get; set; }

        public virtual ICollection<Enrollment> Enrollments { get; set; }
    }
}
```

For our model we do need a `DBContext` 

```csharp


namespace ContosoUniversity
{
    public class SchoolContext : DbContext
    {
        public DbSet<Student> Students { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<Course> Courses { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            
        }
    }
}
```

## d

While this makes _Entity Framework_ nice to use it also introduces some issues with it for GraphQL. With GraphQL the default execution strategy is to parallelize the execution of field resolvers. This means that we potentially access the same scoped `DBContext` with two different threads.

If two or more threads start messing around with the state aggregated on our `DBContext` we start to get into trouble very quickly. The `DBContext` in this case will throw an exception that it is not allowed to access the context with multiple threads.

In _Hot Chocolate_ we can force the execution engine to execute all resolvers serially in order to prevent errors like that with _Entity Framework_.

> Version 11 of Hot Chocolate uses `DBContext` pooling to use multiple `DBContext` instances in one request.

In order to do that we can set a the `ForceSerialExecution` option for the query execution options to `true`.

```csharp
services
    .AddGraphQL(sp =>
        SchemaBuilder.New()
            ...
            .Create(),
        new QueryExecutionOptions { ForceSerialExecution = true });
```

## Projections

With the serial execution feature you basically can just use entity framework to pull in data without worrying to run into thread exceptions.

But when we started talked about integrating _Entity Framework_ we immediately started talking about rewriting the whole query graph into one native query on top of _Entity Framework_. With 10.4 we are introducing the first step on this road with our new projections.

The new _Hot Chocolate_ projections allows us to annotate on the root field that we want to use the selections middleware and _Hot Chocolate_ will then take the query graph from the annotation and rewrite it into a native query.

```csharp
[UseSelection]
public IQueryable<Person> GetPeople(
    [Service]ChatDbContext dbContext) =>
    dbContext.People;
```

Whenever I know write a GraphQL query like:

```graphql
{
  people {
    name
  }
}
```

It translates into:

```SQL
SELECT [Name] FROM [People]
```

But we did not stop here. We already have those nice middleware that you can use for filtering and like always you can combine these. So, lets take our initial example and improve upon this:

```csharp
[UseSelection]
[UseFiltering]
[UseSorting]
public IQueryable<Person> GetPeople(
    [Service]ChatDbContext dbContext) =>
    dbContext.People;
```

Whenever I know write a GraphQL query like:

```graphql
{
  people(where: { name: "foo" }) {
    name
  }
}
```

It translates into:

```SQL
SELECT [Name] FROM [People] WHERE [Name] = 'foo'
```

The selection middleware is not only effecting level on which we annotated it but will take the whole sub-graph into account. This means that if our `Person` for instance has a collection of addresses then we can just dig in.

```graphql
{
  people(where: { name: "foo" }) {
    name
    addresses {
      street
    }
  }
}
```

There were two main issues that made using _Entity Framework_ with _Hot Chocolate_ difficult. The first issue is that the `DBContext` is not thread-safe. _Entity Framework_ implements the unit-of-work pattern and basically to work against a context that holts in memory instances of your entities. You can change those entities in memory and when you have done all you needed to do you invoke `SaveChangesAsync` and all is good.

With ASP.NET Core the `DBContext` is added to the dependency injection as scoped reference by default or in newer version you now can pool them. You basically get one instance per request, you do what you need to do with the context and the

With GraphQL the default execution algorithm for queries executes fields potentially in parallel.

BTW, head over to our _pure code-first_ [Star Wars example](https://github.com/ChilliCream/hotchocolate-examples/tree/master/PureCodeFirst).

If you want to get into contact with us head over to our [slack channel](https://join.slack.com/t/hotchocolategraphql/shared_invite/enQtNTA4NjA0ODYwOTQ0LTViMzA2MTM4OWYwYjIxYzViYmM0YmZhYjdiNzBjOTg2ZmU1YmMwNDZiYjUyZWZlMzNiMTk1OWUxNWZhMzQwY2Q) and join our community.

| [HotChocolate Slack Channel](https://join.slack.com/t/hotchocolategraphql/shared_invite/enQtNTA4NjA0ODYwOTQ0LTViMzA2MTM4OWYwYjIxYzViYmM0YmZhYjdiNzBjOTg2ZmU1YmMwNDZiYjUyZWZlMzNiMTk1OWUxNWZhMzQwY2Q) | [Hot Chocolate Documentation](https://hotchocolate.io) | [Hot Chocolate on GitHub](https://github.com/ChilliCream/hotchocolate) |
| ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------ | ---------------------------------------------------------------------- |


[hot chocolate]: https://hotchocolate.io
[hot chocolate source code]: https://github.com/ChilliCream/hotchocolate
