# QueryContext_Simple_Selector_Include_Brand

## SQL 0

```sql
select  jsonb_build_object('Id', d.id, 'Name', d.data ->> 'Name', 'Brand', d.data -> 'Brand')  as data from public.mt_doc_product as d order by d.id LIMIT :p0;
```

## Expression 0

```text
value(Marten.Linq.MartenLinqQueryable`1[GreenDonut.Data.TestContext.Product]).OrderBy(t => t.Id).Select(root => new Product() {Id = root.Id, Name = root.Name, Brand = root.Brand}).Take(3)
```

