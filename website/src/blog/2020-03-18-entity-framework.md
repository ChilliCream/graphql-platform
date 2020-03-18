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
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoUniversity
{
    public class Student
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
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
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int EnrollmentId { get; set; }
        public int CourseId { get; set; }
        public int StudentId { get; set; }
        public Grade? Grade { get; set; }

        public virtual Course Course { get; set; }
        public virtual Student Student { get; set; }
    }

    public class Course
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CourseId { get; set; }
        public string Title { get; set; }
        public int Credits { get; set; }

        public virtual ICollection<Enrollment> Enrollments { get; set; }
    }
}
```

For our model we do need a `DBContext` against which we can interact with the database. 

```csharp
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity
{
    public class SchoolContext : DbContext
    {
        public DbSet<Student> Students { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<Course> Courses { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlite("Data Source=uni.db");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Student>()
                .HasMany(t => t.Enrollments)
                .WithOne(t => t.Student)
                .HasForeignKey(t => t.StudentId);

            modelBuilder.Entity<Enrollment>()
                .HasIndex(t => new { t.StudentId, t.CourseId })
                .IsUnique();

            modelBuilder.Entity<Course>()
                .HasMany(t => t.Enrollments)
                .WithOne(t => t.Course)
                .HasForeignKey(t => t.CourseId);
        }
    }
}
```

Copy the context as well to the project.

Next, we need to register our `SchoolContext` with the dependency injection. For that lets open our `Startup.cs` and replace the `ConfigureServices` method with the following code.

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddDbContext<SchoolContext>();
}
```

There is one last thing to finish our preparations with the database and to get into GraphQL. We somehow need to create our database. Since we are in this post only exploring how we can query data with entity framework and GraphQL we will also need to seed some data.

Add the following method to the `Startup.cs`:

```csharp
private static void InitializeDatabase(IApplicationBuilder app)
{
    using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
    {
        var context = serviceScope.ServiceProvider.GetRequiredService<SchoolContext>();
        if (context.Database.EnsureCreated())
        {
            var course = new Course { Credits = 10, Title = "Object Oriented Programming 1" };

            context.Enrollments.Add(new Enrollment
            {
                Course = course,
                Student = new Student { FirstMidName = "Rafael", LastName = "Foo", EnrollmentDate = DateTime.UtcNow }
            });
            context.Enrollments.Add(new Enrollment
            {
                Course = course,
                Student = new Student { FirstMidName = "Pascal", LastName = "Bar", EnrollmentDate = DateTime.UtcNow }
            });
            context.Enrollments.Add(new Enrollment
            {
                Course = course,
                Student = new Student { FirstMidName = "Michael", LastName = "Baz", EnrollmentDate = DateTime.UtcNow }
            });
            context.SaveChangesAsync();
        }
    }
}
```

And call this method in the first line of `Configure`. You updated `Configure` method should look like the following:

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    InitializeDatabase(app);

    if (env.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }

    app.UseRouting();

    app.UseEndpoints(endpoints =>
    {
        endpoints.MapGet("/", async context =>
        {
            await context.Response.WriteAsync("Hello World!");
        });
    });
}
```

## GraphQL Schema

Everything in GraphQL resolves around a schema. The schema defines the types that are available and the data that our GraphQL server exposes. In GraphQL we interact with the data through root types. In this post we will only query data which means that we only need the query type.

The query type exposes fields which are called root fields. The root fields define how I can query my data. For our university GraphQL server we want to be able to query the students and then drill deeper into what courses a student is enrolled to.

Lets start with that.

With _Hot Chocolate_ and pure code-first the query root type is represented by a simple class. Public methods or public properties on that type are inferred as fields of my GraphQL type.

The following class:

```csharp
public class Query 
{
    /// <summary>
    /// Gets all students.
    /// </summary>
    public IQueryable<Student> GetStudents() => throw new NotImplementedException();
}
```

Is translated to the following GraphQL type:

```graphql
type Query {
  """
  Gets all students
  """
  students: [Student]
}
```

> _Hot Chocolate_ will apply GraphQL conventions to your types which will remove the verb `Get` from your method or the postfix `async`.

In GraphQL we call the method `GetStudents` a resolver since it resolves for us some data. Resolvers are executed independent from one another and each resolver has dependencies on different resources. Everything that a resolver needs can be injected as a method parameter. Our resolver for instance needs our `ShoolContext`. By using argument injection the execution engine can better optimize how to execute a query.

OK, with this knowledge lets implement our `Query` class.

```csharp
public class Query
{
    /// <summary>
    /// Gets all students.
    /// </summary>
    public IQueryable<Student> GetStudents([Service]SchoolContext schoolContext) =>
        schoolContext.Students;
}
```

Our query class up there would already work. But only for one level. It basically would resolve the students but could not drill deeper. The enrollments would always be empty. In _Hot Chocolate_ we have a concept of field middleware that can alter the execution pipeline of our field resolver.

It is important that middleware order is important since multiple middleware form a field execution pipeline.

In our case we want _Entity Framework_ projections to work so that we can drill into data in our GraphQL query. For this we can add the selection middleware.

```csharp
using System.Linq;
using HotChocolate;
using HotChocolate.Types;

namespace ContosoUniversity
{
    public class Query
    {
        /// <summary>
        /// Gets all students.
        /// </summary>
        [UseSelection]
        public IQueryable<Student> GetStudents([Service]SchoolContext schoolContext) =>
            schoolContext.Students;
    }
}
```

Lets paste this file into our project.

I pointed out that in GraphQL everything resolves around a schema. In order to get our GraphQL server up and running we need to create and host a GraphQL schema. In _Hot Chocolate_ we define a schema with the `SchemaBuilder`.

Open the `Startup.cs` again and then let us add a simple schema with our `Query` type.

For that replace the `ConfigureServices` method with the following code.

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddDbContext<SchoolContext>();

    services.AddGraphQL(
        SchemaBuilder.New()
            .AddQueryType<Query>()
            .Create(),
        new QueryExecutionOptions { ForceSerialExecution = true });
}
```

The above code registers a GraphQL schema with the dependency injection container.

```csharp
SchemaBuilder.New()
    .AddQueryType<Query>()
    .Create()
```

The schema builder registers our `Query` class as GraphQL `Query` type.

```csharp
new QueryExecutionOptions { ForceSerialExecution = true }
```

Also we are defining that the execution engine shall force the execution engine to execute serially since `DBContext` is not thread-safe.

> The upcoming version 11 of Hot Chocolate uses `DBContext` pooling to use multiple `DBContext` instances in one request. This allows version 11 to parallelize data fetching.

In order to enable our ASP.NET Core server to process GraphQL requests we need to register the _Hot Chocolate_ GraphQL middleware.

Replace the `Configure` method of your `Startup.cs` with the following code.

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    InitializeDatabase(app);

    if (env.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }

    app.UseRouting();

    app.UseGraphQL();

    app.UseEndpoints(endpoints =>
    {
        endpoints.MapGet("/", async context =>
        {
            await context.Response.WriteAsync("Hello World!");
        });
    });
}
```

`app.UseGraphQL();` registers the GraphQL middleware with the server. Since we did not specify any path the middleware will run on the root of our server. Like with field middleware the order of ASP.NET Core middleware is important.

## Testing a GraphQL Server

In order to now query our GraphQL server we need a GraphQL IDE to formulate queries and explore the schema. If you want a deluxe GraphQL IDE as an application you can get our very own Banana Cakepop which can be downloaded [here]().

But you can also use Playground and host a simple GraphQL IDE as a middleware with your server. If you want to use playground add the following package:

```bash
dotnet add package HotChocolate.AspNetCore.Playground
```

After that we need to register the playground middleware. For that add `app.UsePlayground();` after `app.UseGraphQL()`. By default playground is hosted on `/playground` meaning in our case `http://localhost:5000/playground`.

The `Configure` method should now look like the following:

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    InitializeDatabase(app);

    if (env.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }

    app.UseRouting();

    app.UseGraphQL();
    app.UsePlayground();

    app.UseEndpoints(endpoints =>
    {
        endpoints.MapGet("/", async context =>
        {
            await context.Response.WriteAsync("Hello World!");
        });
    });
}
```

Lets test our GraphQL server.

```bash
dotnet run --urls http://localhost:5000
```

Now open either Banana Cakepop or Playground (http://localhost:5000/playground).

Our Schema should look like the following now.

```graphql
schema {
  query: Query
}

type Query {
  students: [Student]
}

type Student {
  enrollmentDate: DateTime!
  enrollments: [Enrollment]
  firstMidName: String
  id: Int!
  lastName: String
}

type Course {
  courseId: Int!
  credits: Int!
  enrollments: [Enrollment]
  title: String
}

type Enrollment {
  course: Course
  courseId: Int!
  enrollmentId: Int!
  grade: Grade
  student: Student
  studentId: Int!
}

enum Grade {
  A
  B
  C
  D
  F
}

"The `DateTime` scalar represents an ISO-8601 compliant date time type."
scalar DateTime

"The `Int` scalar type represents non-fractional signed whole numeric values. Int can represent values between -(2^31) and 2^31 - 1."
scalar Int

"The `String` scalar type represents textual data, represented as UTF-8 character sequences. The String type is most often used by GraphQL to represent free-form human-readable text."
scalar String
```

While we just added one field that exposes the `Student` entity to _Hot Chocolate_, _Hot Chocolate_ explored what data is reachable from that entity. In conjunction with the `UseSelection` middleware we can now query all that data and drill into it.

Let us start with a simple query.

```graphql
query {
  students {
    firstMidName
  }
}
```

The above query resolves correctly the data from our database and we get the following result:

```json
{
  "data": {
    "students": [
      {
        "firstMidName": "Pascal"
      }
    ]
  }
}
```

What is interesting is that the GraphQL engine rewrite the incoming GraphQL request to an expression tree that only fetched the data from the database that was needed. So, the SQL query for our GraphQL query looked like the following.

```sql
SELECT "s"."FirstMidName" FROM "Students" AS "s"
```

So let us drill into the data a little more.

```graphql
query {
  students {
    firstMidName
    enrollments {
      course {
        title
      }
    }
  }
}
```

The above query returns:

```json
{
  "data": {
    "students": [
      {
        "firstMidName": "Pascal",
        "enrollments": [
          {
            "course": {
              "title": "Object Oriented Programming 1"
            }
          }
        ]
      }
    ]
  }
}
```

In order to fetch the data the GraphQL query is rewritten to the following SQL:

```sql
SELECT "s"."FirstMidName", "s"."Id", "t"."Title", "t"."EnrollmentId", "t"."CourseId"
    FROM "Students" AS "s"
    LEFT JOIN (
        SELECT "c"."Title", "e"."EnrollmentId", "c"."CourseId", "e"."StudentId"
        FROM "Enrollments" AS "e"
        INNER JOIN "Courses" AS "c" ON "e"."CourseId" = "c"."CourseId"
    ) AS "t" ON "s"."Id" = "t"."StudentId"
    ORDER BY "s"."Id", "t"."EnrollmentId", "t"."CourseId"
```

## Filtering

Without a quite any code we already have a working GraphQL server that returns all the students and we can drill into the data. We really just added entity framework and exposed a single root field.

Let us go further with this. We actually can do more here and _Hot Chocolate_ provides you with a filter and sorting middleware to really give you the power to query your data with complex expressions.

First let us add the following packages.

```bash
dotnet add package HotChocolate.Types.Filters
dotnet add package HotChocolate.Types.Sorting
```

With these new packages in place let us rewrite our query type in order to enable proper filters.

```csharp
using System.Linq;
using HotChocolate;
using HotChocolate.Types;

namespace ContosoUniversity
{
    public class Query
    {
        [UseSelection]
        [UseFiltering]
        [UseSorting]
        public IQueryable<Student> GetStudents([Service]SchoolContext context) =>
            context.Students;
    }
}
```

The above query type has now two new attributes `UseFiltering` and `UseSorting`. Let me again state that order is important.

With that upgraded `Query` type let us run restart our server.

```bash
dotnet run --urls http://localhost:5000
```

Now let us inspect our schema again. When we look at the `students` field we can see that there are new arguments `where` and `orderBy`.

We now can define a query like the following.

```graphql
query {
  students(where: { OR: [{ lastName: "Bar" }, { lastName: "Baz" }] }) {
    firstMidName
    lastName
    enrollments {
      course {
        title
      }
    }
  }
}
```

Which will return the following result:

```json
{
  "data": {
    "students": [
      {
        "firstMidName": "Pascal",
        "lastName": "Bar",
        "enrollments": [
          {
            "course": {
              "title": "Object Oriented Programming 1"
            }
          }
        ]
      },
      {
        "firstMidName": "Michael",
        "lastName": "Baz",
        "enrollments": [
          {
            "course": {
              "title": "Object Oriented Programming 1"
            }
          }
        ]
      }
    ]
  }
}
```

Again, we are rewriting the whole GraphQL query into one expression tree that translates into the following SQL.

```sql
SELECT "s"."FirstMidName", "s"."LastName", "s"."Id", "t"."Title", "t"."EnrollmentId", "t"."CourseId"
    FROM "Students" AS "s"
    LEFT JOIN (
        SELECT "c"."Title", "e"."EnrollmentId", "c"."CourseId", "e"."StudentId"
        FROM "Enrollments" AS "e"
        INNER JOIN "Courses" AS "c" ON "e"."CourseId" = "c"."CourseId"
    ) AS "t" ON "s"."Id" = "t"."StudentId"
    WHERE ("s"."LastName" = 'Bar') OR ("s"."LastName" = 'Baz')
    ORDER BY "s"."Id", "t"."EnrollmentId", "t"."CourseId"
```

But we can go further and even allow more. Lets say we want to allow the consumer to search for specific grades in our students enrolment list then we could upgrade the students entity like the following.

```csharp
public class Student
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public string LastName { get; set; }
    public string FirstMidName { get; set; }
    public DateTime EnrollmentDate { get; set; }

    [UseFiltering]
    public virtual ICollection<Enrollment> Enrollments { get; set; }
}
```

Id not need to apply `UseSelections` again. `UseSelections` really only has to be applied where the data is fetched. In this case I do only want to support filtering but no sorting on enrollments. I could again add both but decided to only use filtering here.

Let us restart our server and modify the query further.

```bash
dotnet run --urls http://localhost:5000
```

For the next query we will get all students with the last name `Bar` that are enrolled in course 1.

```graphql
query {
  students(where: { lastName: "Bar" }) {
    firstMidName
    lastName
    enrollments(where: { courseId: 1 }) {
      courseId
      course {
        title
      }
    }
  }
}
```

The following query translates again to a single SQL.

```sql
SELECT "s"."FirstMidName", "s"."LastName", "s"."Id", "t"."CourseId", "t"."Title", "t"."EnrollmentId", "t"."CourseId0"
    FROM "Students" AS "s"
    LEFT JOIN (
        SELECT "e"."CourseId", "c"."Title", "e"."EnrollmentId", "c"."CourseId" AS "CourseId0", "e"."StudentId"
        FROM "Enrollments" AS "e"
        INNER JOIN "Courses" AS "c" ON "e"."CourseId" = "c"."CourseId"
        WHERE "e"."CourseId" = 1
    ) AS "t" ON "s"."Id" = "t"."StudentId"
    WHERE "s"."LastName" = 'Bar'
    ORDER BY "s"."Id", "t"."EnrollmentId", "t"."CourseId0"
```

## Paging

With filtering and sorting we infer without almost no code complex filters from your code and allow you to query your data with complex expressions while drilling into the data graph. But we still might get to many data. What if we select all the students from a real university database.

This is where our paging middleware comes in. The paging middleware implements the relay cursor pagination pattern.

> Since we cannot do a skip while with entity framework we actually use a indexed based pagination underneath. For convenience we are wrapping this as really cursor pagination. With mongoDB and other database provider we are supporting real cursor base pagination.

Like with filtering, sorting and selection we just annotate the paging middleware and it just works. Again, middleware order is important so we need to put the paging attribute on the top.

```csharp
using System.Linq;
using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Relay;

namespace ContosoUniversity
{
    public class Query
    {
        [UsePaging]
        [UseSelection]
        [UseFiltering]
        [UseSorting]
        public IQueryable<Student> GetStudents([Service]SchoolContext context) =>
            context.Students;
    }
}
```

Since paging adds metadata for pagination like a `totalCount` or a `pageInfo` the actual result structure of the data changes. 

BTW, head over to our _pure code-first_ [Star Wars example](https://github.com/ChilliCream/hotchocolate-examples/tree/master/PureCodeFirst).

If you want to get into contact with us head over to our [slack channel](https://join.slack.com/t/hotchocolategraphql/shared_invite/enQtNTA4NjA0ODYwOTQ0LTViMzA2MTM4OWYwYjIxYzViYmM0YmZhYjdiNzBjOTg2ZmU1YmMwNDZiYjUyZWZlMzNiMTk1OWUxNWZhMzQwY2Q) and join our community.

| [HotChocolate Slack Channel](https://join.slack.com/t/hotchocolategraphql/shared_invite/enQtNTA4NjA0ODYwOTQ0LTViMzA2MTM4OWYwYjIxYzViYmM0YmZhYjdiNzBjOTg2ZmU1YmMwNDZiYjUyZWZlMzNiMTk1OWUxNWZhMzQwY2Q) | [Hot Chocolate Documentation](https://hotchocolate.io) | [Hot Chocolate on GitHub](https://github.com/ChilliCream/hotchocolate) |
| ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------ | ---------------------------------------------------------------------- |


[hot chocolate]: https://hotchocolate.io
[hot chocolate source code]: https://github.com/ChilliCream/hotchocolate
