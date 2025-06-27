# Fetch_Second_Page

## Result 1

```json
{
  "Page": 2,
  "TotalCount": 20,
  "Items": [
    "Celestara",
    "Dynamova"
  ]
}
```

## SQL 0

```sql
select d.id, d.data from public.mt_doc_relativecursortests_brand as d where (d.data ->> 'Name' > :p0 or (d.data ->> 'Name' = :p1 and d.id > :p2)) order by d.data ->> 'Name', d.id LIMIT :p3;
```

