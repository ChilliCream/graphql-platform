# SendAsync_Should_WriteSameBody_When_K6QueryLiteralIsCached

## Node 1, TestQuery_123456789101112_1 (105 bytes of operation text)

```json
{"query":"query TestQuery_123456789101112_1 {\n  topProducts(first: 5) {\n    name\n    price\n    upc\n    weight\n  }\n}"}
```

## Node 2, TestQuery_123456789101112_2 (80 bytes of operation text)

```json
{"query":"query TestQuery_123456789101112_2 {\n  users {\n    id\n    username\n    name\n  }\n}"}
```

## Node 14, TestQuery_123456789101112_3 (401 bytes of operation text)

```json
{"query":"query TestQuery_123456789101112_3($__fusion_1_id: ID!) {\n  user(id: $__fusion_1_id) {\n    reviews {\n      id\n      body\n      product {\n        upc\n        reviews {\n          id\n          body\n          author {\n            id\n            reviews {\n              id\n              body\n              product {\n                upc\n              }\n            }\n          }\n        }\n      }\n    }\n  }\n}","variables":{"__fusion_1_id":"1"}}
```

## Node 14, TestQuery_123456789101112_9 (275 bytes of operation text)

```json
{"query":"query TestQuery_123456789101112_9($__fusion_9_upc: ID!) {\n  product(upc: $__fusion_9_upc) {\n    reviews {\n      id\n      body\n      author {\n        id\n        reviews {\n          id\n          body\n          product {\n            upc\n          }\n        }\n      }\n    }\n  }\n}","variables":{"__fusion_9_upc":"1"}}
```

## Node 15, TestQuery_123456789101112_4 (127 bytes of operation text)

```json
{"query":"query TestQuery_123456789101112_4($__fusion_2_upc: ID!) {\n  product(upc: $__fusion_2_upc) {\n    name\n    price\n    weight\n  }\n}","variables":{"__fusion_2_upc":"1"}}
```

## Node 16, TestQuery_123456789101112_5 (248 bytes of operation text)

```json
{"query":"query TestQuery_123456789101112_5(\n  $__fusion_3_upc: ID!\n  $__fusion_4_price: Long!\n  $__fusion_4_weight: Long!\n) {\n  productByUpc(upc: $__fusion_3_upc) {\n    inStock\n    shippingEstimate(weight: $__fusion_4_weight, price: $__fusion_4_price)\n  }\n}","variables":{"__fusion_3_upc":"1","__fusion_4_price":"1","__fusion_4_weight":"1"}}
```

## Node 17, TestQuery_123456789101112_6 (113 bytes of operation text)

```json
{"query":"query TestQuery_123456789101112_6($__fusion_5_id: ID!) {\n  user(id: $__fusion_5_id) {\n    username\n    name\n  }\n}","variables":{"__fusion_5_id":"1"}}
```
