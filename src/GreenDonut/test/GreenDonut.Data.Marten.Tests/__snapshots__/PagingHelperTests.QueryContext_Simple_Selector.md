# QueryContext_Simple_Selector

## SQL 0

```sql
select  jsonb_build_object('Id', d.id, 'Name', d.data ->> 'Name')  as data from public.mt_doc_product as d order by d.id LIMIT :p0;
```

## Expression 0

```text
value(Marten.Linq.MartenLinqQueryable`1[GreenDonut.Data.TestContext.Product]).OrderBy(t => t.Id).Select(t => new Product() {Id = t.Id, Name = t.Name}).Take(3)
```

