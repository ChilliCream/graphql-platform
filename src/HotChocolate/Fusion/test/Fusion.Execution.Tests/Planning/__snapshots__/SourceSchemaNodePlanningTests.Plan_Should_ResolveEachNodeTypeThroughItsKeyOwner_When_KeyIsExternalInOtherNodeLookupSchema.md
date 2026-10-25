# Plan_Should_ResolveEachNodeTypeThroughItsKeyOwner_When_KeyIsExternalInOtherNodeLookupSchema

## 00-node-id

```yaml
operation:
  - document: |
      query NodeId {
        node(id: "a1") {
          __typename @fusion__requirement
          ... on Account {
            id
          }
          ... on Chat {
            id
          }
        }
      }
    name: NodeId
    hash: 123456789101112
    searchSpace: 1
    expandedNodes: 1
nodes:
  - id: 1
    type: Node
    idValue: "a1"
    responseName: node
    branches:
      - Account: 4
      - Chat: 3
    fallback: 2
  - id: 2
    type: Operation
    operation: |
      query NodeId_123456789101112_2 {
        node(id: "a1") {
          __typename
        }
      }
    dependencies:
      - id: 1
  - id: 3
    type: Operation
    schema: b
    operation: |
      query NodeId_123456789101112_3 {
        node(id: "a1") {
          __typename
          ... on Chat {
            __typename
            id
          }
        }
      }
    dependencies:
      - id: 1
      - id: 2
  - id: 4
    type: Operation
    schema: a
    operation: |
      query NodeId_123456789101112_4 {
        node(id: "a1") {
          __typename
          ... on Account {
            __typename
            id
          }
        }
      }
    dependencies:
      - id: 1
      - id: 2

```

## 01-typenames

```yaml
operation:
  - document: |
      query Typenames {
        account: node(id: "a1") {
          __typename
        }
        chat: node(id: "c1") {
          __typename
        }
      }
    name: Typenames
    hash: 123456789101112
    searchSpace: 1
    expandedNodes: 1
nodes:
  - id: 1
    type: Node
    idValue: "c1"
    responseName: chat
    fallback: 2
  - id: 2
    type: Operation
    operation: |
      query Typenames_123456789101112_2 {
        chat: node(id: "c1") {
          __typename
        }
      }
    dependencies:
      - id: 1
  - id: 3
    type: Node
    idValue: "a1"
    responseName: account
    fallback: 4
  - id: 4
    type: Operation
    operation: |
      query Typenames_123456789101112_4 {
        account: node(id: "a1") {
          __typename
        }
      }
    dependencies:
      - id: 3

```

## 02-member-fields

```yaml
operation:
  - document: |
      query MemberFields {
        account: node(id: "a1") {
          __typename @fusion__requirement
          ... on Account {
            id
            username
          }
        }
        chat: node(id: "c1") {
          __typename @fusion__requirement
          ... on Chat {
            id
            text
          }
        }
      }
    name: MemberFields
    hash: 123456789101112
    searchSpace: 1
    expandedNodes: 1
nodes:
  - id: 1
    type: Node
    idValue: "c1"
    responseName: chat
    branches:
      - Chat: 3
    fallback: 2
  - id: 2
    type: Operation
    operation: |
      query MemberFields_123456789101112_2 {
        chat: node(id: "c1") {
          __typename
        }
      }
    dependencies:
      - id: 1
  - id: 3
    type: Operation
    schema: b
    operation: |
      query MemberFields_123456789101112_3 {
        chat: node(id: "c1") {
          __typename
          ... on Chat {
            __typename
            id
            text
          }
        }
      }
    dependencies:
      - id: 1
      - id: 2
  - id: 4
    type: Node
    idValue: "a1"
    responseName: account
    branches:
      - Account: 6
    fallback: 5
  - id: 5
    type: Operation
    operation: |
      query MemberFields_123456789101112_5 {
        account: node(id: "a1") {
          __typename
        }
      }
    dependencies:
      - id: 4
  - id: 6
    type: Operation
    schema: a
    operation: |
      query MemberFields_123456789101112_6 {
        account: node(id: "a1") {
          __typename
          ... on Account {
            __typename
            id
            username
          }
        }
      }
    dependencies:
      - id: 4
      - id: 5

```

## 03-other-member

```yaml
operation:
  - document: |
      query OtherMember {
        account: node(id: "a1") {
          __typename @fusion__requirement
          ... on Chat {
            id
          }
        }
        chat: node(id: "c1") {
          __typename @fusion__requirement
          ... on Account {
            id
          }
        }
      }
    name: OtherMember
    hash: 123456789101112
    searchSpace: 1
    expandedNodes: 1
nodes:
  - id: 1
    type: Node
    idValue: "c1"
    responseName: chat
    branches:
      - Account: 3
    fallback: 2
  - id: 2
    type: Operation
    operation: |
      query OtherMember_123456789101112_2 {
        chat: node(id: "c1") {
          __typename
        }
      }
    dependencies:
      - id: 1
  - id: 3
    type: Operation
    schema: a
    operation: |
      query OtherMember_123456789101112_3 {
        chat: node(id: "c1") {
          __typename
          ... on Account {
            __typename
            id
          }
        }
      }
    dependencies:
      - id: 1
      - id: 2
  - id: 4
    type: Node
    idValue: "a1"
    responseName: account
    branches:
      - Chat: 6
    fallback: 5
  - id: 5
    type: Operation
    operation: |
      query OtherMember_123456789101112_5 {
        account: node(id: "a1") {
          __typename
        }
      }
    dependencies:
      - id: 4
  - id: 6
    type: Operation
    schema: b
    operation: |
      query OtherMember_123456789101112_6 {
        account: node(id: "a1") {
          __typename
          ... on Chat {
            __typename
            id
          }
        }
      }
    dependencies:
      - id: 4
      - id: 5

```

## 04-other-member-typename

```yaml
operation:
  - document: |
      query OtherMemberTypename {
        account: node(id: "a1") {
          __typename
          ... on Chat {
            id
          }
        }
        chat: node(id: "c1") {
          __typename
          ... on Account {
            id
          }
        }
      }
    name: OtherMemberTypename
    hash: 123456789101112
    searchSpace: 1
    expandedNodes: 1
nodes:
  - id: 1
    type: Node
    idValue: "c1"
    responseName: chat
    branches:
      - Account: 3
    fallback: 2
  - id: 2
    type: Operation
    operation: |
      query OtherMemberTypename_123456789101112_2 {
        chat: node(id: "c1") {
          __typename
        }
      }
    dependencies:
      - id: 1
  - id: 3
    type: Operation
    schema: a
    operation: |
      query OtherMemberTypename_123456789101112_3 {
        chat: node(id: "c1") {
          __typename
          ... on Account {
            __typename
            id
          }
        }
      }
    dependencies:
      - id: 1
      - id: 2
  - id: 4
    type: Node
    idValue: "a1"
    responseName: account
    branches:
      - Chat: 6
    fallback: 5
  - id: 5
    type: Operation
    operation: |
      query OtherMemberTypename_123456789101112_5 {
        account: node(id: "a1") {
          __typename
        }
      }
    dependencies:
      - id: 4
  - id: 6
    type: Operation
    schema: b
    operation: |
      query OtherMemberTypename_123456789101112_6 {
        account: node(id: "a1") {
          __typename
          ... on Chat {
            __typename
            id
          }
        }
      }
    dependencies:
      - id: 4
      - id: 5

```

## 05-aliased

```yaml
operation:
  - document: |
      query Aliased {
        account: node(id: "a1") {
          __typename @fusion__requirement
          ... on Account {
            id
          }
          ... on Chat {
            id
          }
        }
        chat: node(id: "c1") {
          __typename
          ... on Account {
            id
          }
        }
      }
    name: Aliased
    hash: 123456789101112
    searchSpace: 1
    expandedNodes: 1
nodes:
  - id: 1
    type: Node
    idValue: "c1"
    responseName: chat
    branches:
      - Account: 3
    fallback: 2
  - id: 2
    type: Operation
    operation: |
      query Aliased_123456789101112_2 {
        chat: node(id: "c1") {
          __typename
        }
      }
    dependencies:
      - id: 1
  - id: 3
    type: Operation
    schema: a
    operation: |
      query Aliased_123456789101112_3 {
        chat: node(id: "c1") {
          __typename
          ... on Account {
            __typename
            id
          }
        }
      }
    dependencies:
      - id: 1
      - id: 2
  - id: 4
    type: Node
    idValue: "a1"
    responseName: account
    branches:
      - Account: 7
      - Chat: 6
    fallback: 5
  - id: 5
    type: Operation
    operation: |
      query Aliased_123456789101112_5 {
        account: node(id: "a1") {
          __typename
        }
      }
    dependencies:
      - id: 4
  - id: 6
    type: Operation
    schema: b
    operation: |
      query Aliased_123456789101112_6 {
        account: node(id: "a1") {
          __typename
          ... on Chat {
            __typename
            id
            id
          }
        }
      }
    dependencies:
      - id: 4
      - id: 5
  - id: 7
    type: Operation
    schema: a
    operation: |
      query Aliased_123456789101112_7 {
        account: node(id: "a1") {
          __typename
          ... on Account {
            __typename
            id
          }
        }
      }
    dependencies:
      - id: 4
      - id: 5

```

## 10-member-split

```yaml
operation:
  - document: |
      query MemberSplit {
        node(id: "a1") {
          __typename @fusion__requirement
          ... on Account {
            id
          }
          ... on Chat {
            id
          }
        }
      }
    name: MemberSplit
    hash: 123456789101112
    searchSpace: 1
    expandedNodes: 1
nodes:
  - id: 1
    type: Node
    idValue: "a1"
    responseName: node
    branches:
      - Account: 4
      - Chat: 3
    fallback: 2
  - id: 2
    type: Operation
    operation: |
      query MemberSplit_123456789101112_2 {
        node(id: "a1") {
          __typename
        }
      }
    dependencies:
      - id: 1
  - id: 3
    type: Operation
    schema: b
    operation: |
      query MemberSplit_123456789101112_3 {
        node(id: "a1") {
          __typename
          ... on Chat {
            __typename
            id
          }
        }
      }
    dependencies:
      - id: 1
      - id: 2
  - id: 4
    type: Operation
    schema: a
    operation: |
      query MemberSplit_123456789101112_4 {
        node(id: "a1") {
          __typename
          ... on Account {
            __typename
            id
          }
        }
      }
    dependencies:
      - id: 1
      - id: 2

```

## 20-node-id-chat

```yaml
operation:
  - document: |
      query NodeIdChat {
        node(id: "c1") {
          __typename @fusion__requirement
          ... on Account {
            id
          }
          ... on Chat {
            id
          }
        }
      }
    name: NodeIdChat
    hash: 123456789101112
    searchSpace: 1
    expandedNodes: 1
nodes:
  - id: 1
    type: Node
    idValue: "c1"
    responseName: node
    branches:
      - Account: 4
      - Chat: 3
    fallback: 2
  - id: 2
    type: Operation
    operation: |
      query NodeIdChat_123456789101112_2 {
        node(id: "c1") {
          __typename
        }
      }
    dependencies:
      - id: 1
  - id: 3
    type: Operation
    schema: b
    operation: |
      query NodeIdChat_123456789101112_3 {
        node(id: "c1") {
          __typename
          ... on Chat {
            __typename
            id
          }
        }
      }
    dependencies:
      - id: 1
      - id: 2
  - id: 4
    type: Operation
    schema: a
    operation: |
      query NodeIdChat_123456789101112_4 {
        node(id: "c1") {
          __typename
          ... on Account {
            __typename
            id
          }
        }
      }
    dependencies:
      - id: 1
      - id: 2

```
