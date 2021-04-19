---
title: "Pagination"
---

import { ExampleTabs } from "../../../components/mdx/example-tabs"

> This document covers a lot of theory around pagination. If you just want to learn how to implement Relay-style cursor pagination, head over [here](/docs/hotchocolate/fetching-data/pagination/#relay-style-cursor-pagination).

Pagination is one of the most common problems that we have to solve when implementing our backend. Often, sets of data are too large to pass them directly to the consumer of our service.

Pagination solves this problem by giving the consumer the ability to fetch a set in chunks.

There are various ways we could implement pagination in our server, but there are mainly two concepts we find in most GraphQL servers: _Offset_ and _Cursor_ pagination.

# Offset Pagination

_Offset-based_ pagination is found in many server implementations whether the backend is implemented in SOAP, REST or GraphQL.

The simplest way to implement _Offset-based_ pagination on one of our fields is to add an `offset` and a `limit` argument.

```csharp
public class Query
{
    public IEnumerable<User> GetUsers(int? offset, int? limit,
        [Service] IUserRepository repository)
    {
        IEnumerable<User> users = repository.GetUsers();

        if(offset.HasValue)
        {
            users = users.Skip(offset.Value);
        }

        if(limit.HasValue)
        {
            users = users.Take(limit.Value);
        }

        return users;
    }
}
```

Most of the time we are not working with an `IEnumerable` though, but rather with a database of sorts. Luckily for us, most databases support queries similiar to the following.

```sql
SELECT * FROM Users LIMIT 10 OFFSET 5
```

But whilst _Offset-based_ pagination is simple to implement and works relatively well, there are also some problems:

- Using `LIMIT` and `OFFSET` on the database-side does not scale well for large datasets. Most databases work with an index instead of numbered rows. This means the database always has to count _offset + limit_ rows, before discarding the offset and only returning the requested number of rows.

- If new entries are written to or removed from our database at high frequency, the _offset_ becomes unreliable, potentially skipping or returning duplicate entries.

Luckily we can solve these issues pretty easily by switching from an `offset` to a `cursor`. Continue reading to find out how this works.

# Cursor Pagination

TODO

# Relay-style cursor pagination
