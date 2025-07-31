# QueryContext_Simple_Selector_Include_Product_List

## SQL 0

```sql
select  jsonb_build_object('Id', d.id, 'Name', d.data ->> 'Name', 'Products', d.data -> 'Products')  as data from public.mt_doc_brand as d order by d.id LIMIT :p0;
```

## Expression 0

```text
value(Marten.Linq.MartenLinqQueryable`1[GreenDonut.Data.TestContext.Brand]).OrderBy(t => t.Id).Select(t => new Brand() {Id = t.Id, Name = t.Name, Products = t.Products}).Take(3)
```

