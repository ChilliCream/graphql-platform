# Plan_Should_OrderRootsByFirstIncludedOccurrence_When_ResponseNameRepeatsUnderConditions

## Plan

```yaml
id: 4d4a42bab6f4d5d7aa85f2c26bb3801235189a97a3ad26d60b4a05864b5b7efc
operation:
  - document: |
      mutation Conditioned($counter: ID!, $a: Boolean!, $b: Boolean!) {
        first: bump(counter: $counter, by: 1) @include(if: $a)
        second: add(counter: $counter, by: 10) @include(if: $b)
        first: bump(counter: $counter, by: 1) @skip(if: $a) @include(if: $b)
        last: bump(counter: $counter, by: 100)
      }
    name: Conditioned
    kind: Mutation
    id: "123456789101112"
    hash: "123456789101112"
    shortHash: "123456789101112"
searchSpace: 1
expandedNodes: 1
nodes:
  - id: 1
    type: Operation
    schema: catalog
    operation:
      name: Conditioned_123456789101112_1
      kind: Mutation
      document: |
        mutation Conditioned_123456789101112_1($counter: ID!) {
          first: bump(counter: $counter, by: 1)
        }
      hash: cfb4b66256af3e1e4a07c07bd2d0102437be45443b70e2fc4286e91ba3574353
      shortHash: cfb4b662
      xxHash: 3933895797487385760
    resultSelectionSet: >-
      { first }
    conditions:
      - variable: $a
        passingValue: true
    forwardedVariables:
      - counter
  - id: 2
    type: Operation
    schema: details
    operation:
      name: Conditioned_123456789101112_2
      kind: Mutation
      document: |
        mutation Conditioned_123456789101112_2($counter: ID!) {
          second: add(counter: $counter, by: 10)
        }
      hash: 95f56a29d7b32f1ec2183439c7c68b23a3d9117928d06b04098808105ebaa9b4
      shortHash: 95f56a29
      xxHash: 9918861080952254044
    resultSelectionSet: >-
      { second }
    conditions:
      - variable: $b
        passingValue: true
    forwardedVariables:
      - counter
  - id: 3
    type: Operation
    schema: catalog
    operation:
      name: Conditioned_123456789101112_3
      kind: Mutation
      document: |
        mutation Conditioned_123456789101112_3($counter: ID!) {
          first: bump(counter: $counter, by: 1)
        }
      hash: 18f50f37519613e8d05f400219f49f78d53880ef74ec34fc89e606f589f1cf3b
      shortHash: 18f50f37
      xxHash: 9218465121231643046
    resultSelectionSet: >-
      { first }
    conditions:
      - variable: $a
        passingValue: false
      - variable: $b
        passingValue: true
    forwardedVariables:
      - counter
  - id: 4
    type: Operation
    schema: catalog
    operation:
      name: Conditioned_123456789101112_4
      kind: Mutation
      document: |
        mutation Conditioned_123456789101112_4($counter: ID!) {
          last: bump(counter: $counter, by: 100)
        }
      hash: 0a055e22eb1ba0b71dcf3187a14ebbfd82ebc8f8ec9b268c8c0d45a056c96801
      shortHash: 0a055e22
      xxHash: 8001275110627868804
    resultSelectionSet: >-
      { last }
    forwardedVariables:
      - counter

```

## Root order when $a: true, $b: true

```text
first (1), second (2), last (4)
```

## Root order when $a: true, $b: false

```text
first (1), last (4)
```

## Root order when $a: false, $b: true

```text
second (2), first (3), last (4)
```

## Root order when $a: false, $b: false

```text
last (4)
```
