# Paging_WithChildCollectionProjectionExpression_First_5

## SQL 0

```sql
select  jsonb_build_object('Id', d.id, 'Name', d.data ->> 'Name', 'Products', d.data -> 'Products')  as data from public.mt_doc_brand as d LIMIT :p0;
```

## Expression 0

```text
value(Marten.Linq.MartenLinqQueryable`1[GreenDonut.Data.TestContext.Brand]).Select(brand => new BrandWithProductsDto() {Id = brand.Id, Name = brand.Name, Products = brand.Products.AsQueryable().Select(ProductDto.Projection).ToList()}).OrderBy(t => t.Name).ThenBy(t => t.Id).Take(6)
```

## Result 3

```json
{
  "HasNextPage": true,
  "HasPreviousPage": false,
  "First": 1,
  "FirstCursor": "e31CcmFuZFw6MDox",
  "Last": 5,
  "LastCursor": "e31CcmFuZFw6NDo1"
}
```

## Result 4

```json
[
  {
    "Id": 1,
    "Name": "Brand:0",
    "Products": []
  },
  {
    "Id": 2,
    "Name": "Brand:1",
    "Products": []
  },
  {
    "Id": 3,
    "Name": "Brand:2",
    "Products": []
  },
  {
    "Id": 4,
    "Name": "Brand:3",
    "Products": []
  },
  {
    "Id": 5,
    "Name": "Brand:4",
    "Products": []
  }
]
```

