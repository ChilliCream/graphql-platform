# Paging_First_5_Before_Id_96

## SQL 0

```sql
select d.id, d.data from public.mt_doc_brand as d where (d.data ->> 'Name' < :p0 or (d.data ->> 'Name' = :p1 and d.id < :p2)) order by d.data ->> 'Name' desc, d.id desc LIMIT :p3;
```

## Expression 0

```text
value(Marten.Linq.MartenLinqQueryable`1[GreenDonut.Data.TestContext.Brand]).OrderByDescending(t => t.Name).ThenByDescending(t => t.Id).Where(t => ((Compare(t.Name, value(GreenDonut.Data.Expressions.ExpressionHelpers+<>c__DisplayClass6_0`1[System.String]).value) < 0) OrElse ((t.Name == value(GreenDonut.Data.Expressions.ExpressionHelpers+<>c__DisplayClass6_0`1[System.String]).value) AndAlso (t.Id < value(GreenDonut.Data.Expressions.ExpressionHelpers+<>c__DisplayClass6_0`1[System.Int32]).value)))).Take(6)
```

## Result 3

```json
{
  "HasNextPage": true,
  "HasPreviousPage": true,
  "First": 92,
  "FirstCursor": "e31CcmFuZFw6OTE6OTI=",
  "Last": 96,
  "LastCursor": "e31CcmFuZFw6OTU6OTY="
}
```

## Result 4

```json
[
  {
    "Id": 92,
    "Name": "Brand:91",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country91"
      }
    }
  },
  {
    "Id": 93,
    "Name": "Brand:92",
    "DisplayName": "BrandDisplay92",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country92"
      }
    }
  },
  {
    "Id": 94,
    "Name": "Brand:93",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country93"
      }
    }
  },
  {
    "Id": 95,
    "Name": "Brand:94",
    "DisplayName": "BrandDisplay94",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country94"
      }
    }
  },
  {
    "Id": 96,
    "Name": "Brand:95",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country95"
      }
    }
  }
]
```

