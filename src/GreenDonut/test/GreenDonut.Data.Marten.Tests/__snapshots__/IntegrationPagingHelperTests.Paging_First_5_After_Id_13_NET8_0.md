# Paging_First_5_After_Id_13

## SQL 0

```sql
select d.id, d.data from public.mt_doc_brand as d where (d.data ->> 'Name' > :p0 or (d.data ->> 'Name' = :p1 and d.id > :p2)) order by d.data ->> 'Name', d.id LIMIT :p3;
```

## Expression 0

```text
value(Marten.Linq.MartenLinqQueryable`1[GreenDonut.Data.TestContext.Brand]).OrderBy(t => t.Name).ThenBy(t => t.Id).Where(t => ((Compare(t.Name, value(GreenDonut.Data.Expressions.ExpressionHelpers+<>c__DisplayClass6_0`1[System.String]).value) > 0) OrElse ((t.Name == value(GreenDonut.Data.Expressions.ExpressionHelpers+<>c__DisplayClass6_0`1[System.String]).value) AndAlso (t.Id > value(GreenDonut.Data.Expressions.ExpressionHelpers+<>c__DisplayClass6_0`1[System.Int32]).value)))).Take(6)
```

## Result 3

```json
{
  "HasNextPage": true,
  "HasPreviousPage": true,
  "First": 14,
  "FirstCursor": "e31CcmFuZFw6MTM6MTQ=",
  "Last": 18,
  "LastCursor": "e31CcmFuZFw6MTc6MTg="
}
```

## Result 4

```json
[
  {
    "Id": 14,
    "Name": "Brand:13",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country13"
      }
    }
  },
  {
    "Id": 15,
    "Name": "Brand:14",
    "DisplayName": "BrandDisplay14",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country14"
      }
    }
  },
  {
    "Id": 16,
    "Name": "Brand:15",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country15"
      }
    }
  },
  {
    "Id": 17,
    "Name": "Brand:16",
    "DisplayName": "BrandDisplay16",
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country16"
      }
    }
  },
  {
    "Id": 18,
    "Name": "Brand:17",
    "DisplayName": null,
    "AlwaysNull": null,
    "Products": [],
    "BrandDetails": {
      "Country": {
        "Name": "Country17"
      }
    }
  }
]
```

