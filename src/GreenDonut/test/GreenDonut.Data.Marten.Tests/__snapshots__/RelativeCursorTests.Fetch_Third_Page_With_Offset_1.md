# Fetch_Third_Page_With_Offset_1

## Result 1

```json
{
  "Page": 3,
  "TotalCount": 20,
  "Items": [
    "Evolvance",
    "Futurova"
  ]
}
```

## SQL 0

```sql
select d.id, d.data from public.mt_doc_relativecursortests_brand as d where (d.data ->> 'Name' > :p0 or (d.data ->> 'Name' = :p1 and d.id > :p2)) order by d.data ->> 'Name', d.id OFFSET :p3 LIMIT :p4;
```

