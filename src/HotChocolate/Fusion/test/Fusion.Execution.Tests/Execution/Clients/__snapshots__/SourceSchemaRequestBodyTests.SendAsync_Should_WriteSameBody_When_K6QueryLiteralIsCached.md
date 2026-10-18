# SendAsync_Should_WriteSameBody_When_K6QueryLiteralIsCached

## Node 1, TestQuery_123456789101112_1 (85 bytes of operation text)

```json
{"query":"query TestQuery_123456789101112_1 { topProducts(first: 5) { name price upc weight } }"}
```

## Node 2, TestQuery_123456789101112_2 (64 bytes of operation text)

```json
{"query":"query TestQuery_123456789101112_2 { users { id username name } }"}
```

## Node 14, TestQuery_123456789101112_3 (193 bytes of operation text)

```json
{"query":"query TestQuery_123456789101112_3($__fusion_1_id: ID!) { user(id: $__fusion_1_id) { reviews { id body product { upc reviews { id body author { id reviews { id body product { upc } } } } } } } }","variables":{"__fusion_1_id":"1"}}
```

## Node 14, TestQuery_123456789101112_9 (163 bytes of operation text)

```json
{"query":"query TestQuery_123456789101112_9($__fusion_9_upc: ID!) { product(upc: $__fusion_9_upc) { reviews { id body author { id reviews { id body product { upc } } } } } }","variables":{"__fusion_9_upc":"1"}}
```

## Node 15, TestQuery_123456789101112_4 (111 bytes of operation text)

```json
{"query":"query TestQuery_123456789101112_4($__fusion_2_upc: ID!) { product(upc: $__fusion_2_upc) { name price weight } }","variables":{"__fusion_2_upc":"1"}}
```

## Node 16, TestQuery_123456789101112_5 (230 bytes of operation text)

```json
{"query":"query TestQuery_123456789101112_5($__fusion_3_upc: ID!, $__fusion_4_price: Long!, $__fusion_4_weight: Long!) { productByUpc(upc: $__fusion_3_upc) { inStock shippingEstimate(weight: $__fusion_4_weight, price: $__fusion_4_price) } }","variables":{"__fusion_3_upc":"1","__fusion_4_price":"1","__fusion_4_weight":"1"}}
```

## Node 17, TestQuery_123456789101112_6 (101 bytes of operation text)

```json
{"query":"query TestQuery_123456789101112_6($__fusion_5_id: ID!) { user(id: $__fusion_5_id) { username name } }","variables":{"__fusion_5_id":"1"}}
```
