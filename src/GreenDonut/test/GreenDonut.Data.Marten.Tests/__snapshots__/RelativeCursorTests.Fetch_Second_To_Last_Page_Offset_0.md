# Fetch_Second_To_Last_Page_Offset_0

## Result 1

```json
{
  "Page": 9,
  "TotalCount": 20,
  "Items": [
    "Quantumis",
    "Radiantum"
  ]
}
```

## SQL 0

```sql
select d.id, d.data from public.mt_doc_relativecursortests_brand as d where (d.data ->> 'Name' < :p0 or (d.data ->> 'Name' = :p1 and d.id < :p2)) order by d.data ->> 'Name' desc, d.id desc LIMIT :p3;
```

