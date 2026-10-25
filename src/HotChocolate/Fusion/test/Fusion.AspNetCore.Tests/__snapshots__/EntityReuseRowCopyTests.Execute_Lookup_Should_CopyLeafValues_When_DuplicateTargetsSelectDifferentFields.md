# Execute_Lookup_Should_CopyLeafValues_When_DuplicateTargetsSelectDifferentFields

```json
{
  "WithReuse": "accounts-api: 1, products-api: 1, reviews-api: 1",
  "WithoutReuse": "accounts-api: 1, products-api: 1, reviews-api: 1",
  "Data": "{\"me\":{\"reviews\":[{\"product\":{\"upc\":\"1\",\"name\":\"Table\",\"price\":899,\"weight\":100}},{\"product\":{\"upc\":\"1\",\"name\":\"Table\",\"price\":899,\"weight\":100}}]},\"users\":[{\"id\":\"1\",\"reviews\":[{\"product\":{\"upc\":\"1\",\"name\":\"Table\"}},{\"product\":{\"upc\":\"1\",\"name\":\"Table\"}}]},{\"id\":\"2\",\"reviews\":[{\"product\":{\"upc\":\"1\",\"name\":\"Table\"}},{\"product\":{\"upc\":\"1\",\"name\":\"Table\"}}]},{\"id\":\"3\",\"reviews\":[{\"product\":{\"upc\":\"1\",\"name\":\"Table\"}},{\"product\":{\"upc\":\"1\",\"name\":\"Table\"}}]},{\"id\":\"4\",\"reviews\":[{\"product\":{\"upc\":\"1\",\"name\":\"Table\"}},{\"product\":{\"upc\":\"1\",\"name\":\"Table\"}}]},{\"id\":\"5\",\"reviews\":[{\"product\":{\"upc\":\"1\",\"name\":\"Table\"}},{\"product\":{\"upc\":\"1\",\"name\":\"Table\"}}]},{\"id\":\"6\",\"reviews\":[{\"product\":{\"upc\":\"1\",\"name\":\"Table\"}},{\"product\":{\"upc\":\"1\",\"name\":\"Table\"}}]}]}"
}
```
