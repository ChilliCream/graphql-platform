# Fetch_Fourth_To_Last_Page_Offset_Negative_2

## Result 1

```json
{
  "Page": 7,
  "TotalCount": 20,
  "Items": [
    "Momentumix",
    "Nebularis"
  ]
}
```

## SQL 0

```sql
select d.id, d.data from public.mt_doc_relativecursortests_brand as d where (d.data ->> 'Name' < :p0 or (d.data ->> 'Name' = :p1 and d.id < :p2)) order by d.data ->> 'Name' desc, d.id desc OFFSET :p3 LIMIT :p4;
```

