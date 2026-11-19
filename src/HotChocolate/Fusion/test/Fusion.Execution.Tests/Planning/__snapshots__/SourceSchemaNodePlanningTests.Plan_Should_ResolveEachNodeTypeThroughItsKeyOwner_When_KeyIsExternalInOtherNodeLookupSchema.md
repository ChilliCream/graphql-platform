# Plan_Should_ResolveEachNodeTypeThroughItsKeyOwner_When_KeyIsExternalInOtherNodeLookupSchema

## 00-node-id

```yaml
id: 93ac22335d228a1802435f153604882361f9743ed34902763a66c6338ad3506f
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
    kind: Query
    id: "123456789101112"
    hash: "123456789101112"
    shortHash: "123456789101112"
searchSpace: 1
expandedNodes: 1
nodes:
  - id: 1
    type: Node
    idValue: "\"a1\""
    responseName: node
    branches:
      Account: 4
      Chat: 3
    fallback: 2
  - id: 2
    type: Operation
    operation:
      name: NodeId_123456789101112_2
      kind: Query
      document: |
        query NodeId_123456789101112_2 {
          node(id: "a1") {
            __typename
          }
        }
      hash: ff12eea2ccec186fcf9faf78fd70293897e6b8fccfd9cf5e6f6d2a8eefc8c54b
      shortHash: ff12eea2
      xxHash: 13686051397605762067
    resultSelectionSet: >-
      { node }
    dependencies:
      - id: 1
  - id: 3
    type: Operation
    schema: b
    operation:
      name: NodeId_123456789101112_3
      kind: Query
      document: |
        query NodeId_123456789101112_3 {
          node(id: "a1") {
            __typename
            ... on Chat {
              __typename
              id
            }
          }
        }
      hash: fdd6917f70a67ad144e4d827c92dfc7b01c1b86e46eb45ecba9d924636ccf399
      shortHash: fdd6917f
      xxHash: 17935253322469928772
    lookupTypeName: Chat
    resultSelectionSet: >-
      { node { __typename ... on Chat { __typename id } } }
    dependencies:
      - id: 1
      - id: 2
  - id: 4
    type: Operation
    schema: a
    operation:
      name: NodeId_123456789101112_4
      kind: Query
      document: |
        query NodeId_123456789101112_4 {
          node(id: "a1") {
            __typename
            ... on Account {
              __typename
              id
            }
          }
        }
      hash: ec28bf7dd6f3d5b4db0f9c5a1f8f08394c1f25eed1d09162905777b94c8a5828
      shortHash: ec28bf7d
      xxHash: 3701120947326809045
    lookupTypeName: Account
    resultSelectionSet: >-
      { node { __typename ... on Account { __typename id } } }
    dependencies:
      - id: 1
      - id: 2

```

## 01-typenames

```yaml
id: 4c52c63ce1013e2ce256fcf6a7a92e62f7546c460cbf0bb9f0b802d27797241e
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
    kind: Query
    id: "123456789101112"
    hash: "123456789101112"
    shortHash: "123456789101112"
searchSpace: 1
expandedNodes: 1
nodes:
  - id: 1
    type: Node
    idValue: "\"c1\""
    responseName: chat
    branches: {}
    fallback: 2
  - id: 2
    type: Operation
    operation:
      name: Typenames_123456789101112_2
      kind: Query
      document: |
        query Typenames_123456789101112_2 {
          chat: node(id: "c1") {
            __typename
          }
        }
      hash: 171842790f76796db9be21e5558dced37aa0c2c5efc8823ea81c9e8a9bdb6aa8
      shortHash: "17184279"
      xxHash: 14969061012869820288
    resultSelectionSet: >-
      { chat }
    dependencies:
      - id: 1
  - id: 3
    type: Node
    idValue: "\"a1\""
    responseName: account
    branches: {}
    fallback: 4
  - id: 4
    type: Operation
    operation:
      name: Typenames_123456789101112_4
      kind: Query
      document: |
        query Typenames_123456789101112_4 {
          account: node(id: "a1") {
            __typename
          }
        }
      hash: a57ad02b89d560a64447c82cba1c98c692b5056c7dbe04a5449f526fb79107c5
      shortHash: a57ad02b
      xxHash: 11441786107340575107
    resultSelectionSet: >-
      { account }
    dependencies:
      - id: 3

```

## 02-member-fields

```yaml
id: ea096c8254bd83469052c3ddbe9e867a4bf188679905829696ed7e5fde8407fe
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
    kind: Query
    id: "123456789101112"
    hash: "123456789101112"
    shortHash: "123456789101112"
searchSpace: 1
expandedNodes: 1
nodes:
  - id: 1
    type: Node
    idValue: "\"c1\""
    responseName: chat
    branches:
      Chat: 3
    fallback: 2
  - id: 2
    type: Operation
    operation:
      name: MemberFields_123456789101112_2
      kind: Query
      document: |
        query MemberFields_123456789101112_2 {
          chat: node(id: "c1") {
            __typename
          }
        }
      hash: 72d6881f894a50bf1f5db0cffad6f216ba80d1d4d2e086eae7825cbdd47efa1d
      shortHash: 72d6881f
      xxHash: 14979236905963922032
    resultSelectionSet: >-
      { chat }
    dependencies:
      - id: 1
  - id: 3
    type: Operation
    schema: b
    operation:
      name: MemberFields_123456789101112_3
      kind: Query
      document: |
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
      hash: 7787dce862e472ecf1cd92fd889df0dc2e5395b5ed7dcc1d01077a243dd03946
      shortHash: 7787dce8
      xxHash: 2573389171758143979
    lookupTypeName: Chat
    resultSelectionSet: >-
      { chat { __typename ... on Chat { __typename id text } } }
    dependencies:
      - id: 1
      - id: 2
  - id: 4
    type: Node
    idValue: "\"a1\""
    responseName: account
    branches:
      Account: 6
    fallback: 5
  - id: 5
    type: Operation
    operation:
      name: MemberFields_123456789101112_5
      kind: Query
      document: |
        query MemberFields_123456789101112_5 {
          account: node(id: "a1") {
            __typename
          }
        }
      hash: 3e87ca5f8826e8fad578f784442d73a60c167d98b2df5c6af3bcff8fcb34c613
      shortHash: 3e87ca5f
      xxHash: 11206294550053770883
    resultSelectionSet: >-
      { account }
    dependencies:
      - id: 4
  - id: 6
    type: Operation
    schema: a
    operation:
      name: MemberFields_123456789101112_6
      kind: Query
      document: |
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
      hash: ab116ae1ecad705aa525098b92d69665b06b8d65cc29f3bfd938e4d75595a888
      shortHash: ab116ae1
      xxHash: 16473784511276119736
    lookupTypeName: Account
    resultSelectionSet: >-
      { account { __typename ... on Account { __typename id username } } }
    dependencies:
      - id: 4
      - id: 5

```

## 03-other-member

```yaml
id: f2be8808de7726b781f6d0f78a71deee674111a6ed0d4c202c3061f32f50f861
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
    kind: Query
    id: "123456789101112"
    hash: "123456789101112"
    shortHash: "123456789101112"
searchSpace: 1
expandedNodes: 1
nodes:
  - id: 1
    type: Node
    idValue: "\"c1\""
    responseName: chat
    branches:
      Account: 3
    fallback: 2
  - id: 2
    type: Operation
    operation:
      name: OtherMember_123456789101112_2
      kind: Query
      document: |
        query OtherMember_123456789101112_2 {
          chat: node(id: "c1") {
            __typename
          }
        }
      hash: f1d145fecba0ecb893d0b84d8f3846da65a795730f337c6e53ab59b586788891
      shortHash: f1d145fe
      xxHash: 17976964679665090677
    resultSelectionSet: >-
      { chat }
    dependencies:
      - id: 1
  - id: 3
    type: Operation
    schema: a
    operation:
      name: OtherMember_123456789101112_3
      kind: Query
      document: |
        query OtherMember_123456789101112_3 {
          chat: node(id: "c1") {
            __typename
            ... on Account {
              __typename
              id
            }
          }
        }
      hash: e3381187ad77fd2af5bc0e201411e5fb6caf9cf43bd45f0343b0670ba0120d78
      shortHash: e3381187
      xxHash: 8943031124790777516
    lookupTypeName: Account
    resultSelectionSet: >-
      { chat { __typename ... on Account { __typename id } } }
    dependencies:
      - id: 1
      - id: 2
  - id: 4
    type: Node
    idValue: "\"a1\""
    responseName: account
    branches:
      Chat: 6
    fallback: 5
  - id: 5
    type: Operation
    operation:
      name: OtherMember_123456789101112_5
      kind: Query
      document: |
        query OtherMember_123456789101112_5 {
          account: node(id: "a1") {
            __typename
          }
        }
      hash: 8ab7a73ee4e46b4125935e5b11162660dc4dbccb09fc6ba38fa0e27b0abb9792
      shortHash: 8ab7a73e
      xxHash: 9690657461033426816
    resultSelectionSet: >-
      { account }
    dependencies:
      - id: 4
  - id: 6
    type: Operation
    schema: b
    operation:
      name: OtherMember_123456789101112_6
      kind: Query
      document: |
        query OtherMember_123456789101112_6 {
          account: node(id: "a1") {
            __typename
            ... on Chat {
              __typename
              id
            }
          }
        }
      hash: a5d62727f4b1ea097eb1d12222237b8df0fed9e4b23cf08a11b7d170f3e0b3f8
      shortHash: a5d62727
      xxHash: 115793209665356803
    lookupTypeName: Chat
    resultSelectionSet: >-
      { account { __typename ... on Chat { __typename id } } }
    dependencies:
      - id: 4
      - id: 5

```

## 04-other-member-typename

```yaml
id: c6aae4bcc71e0a94f6f849b1977595ffcc7777bfaf7eefeae6edfbdf92265387
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
    kind: Query
    id: "123456789101112"
    hash: "123456789101112"
    shortHash: "123456789101112"
searchSpace: 1
expandedNodes: 1
nodes:
  - id: 1
    type: Node
    idValue: "\"c1\""
    responseName: chat
    branches:
      Account: 3
    fallback: 2
  - id: 2
    type: Operation
    operation:
      name: OtherMemberTypename_123456789101112_2
      kind: Query
      document: |
        query OtherMemberTypename_123456789101112_2 {
          chat: node(id: "c1") {
            __typename
          }
        }
      hash: e5ac7ec4dd5db67452535273422793fa9b79cc7cfcf91644a429a47930cdd2a8
      shortHash: e5ac7ec4
      xxHash: 13527486274138917501
    resultSelectionSet: >-
      { chat }
    dependencies:
      - id: 1
  - id: 3
    type: Operation
    schema: a
    operation:
      name: OtherMemberTypename_123456789101112_3
      kind: Query
      document: |
        query OtherMemberTypename_123456789101112_3 {
          chat: node(id: "c1") {
            __typename
            ... on Account {
              __typename
              id
            }
          }
        }
      hash: a530263c7cc22dfec4180d06606f7e76304d08e3bb06ea6783959736f974a6c9
      shortHash: a530263c
      xxHash: 4451965045277873217
    lookupTypeName: Account
    resultSelectionSet: >-
      { chat { __typename ... on Account { __typename id } } }
    dependencies:
      - id: 1
      - id: 2
  - id: 4
    type: Node
    idValue: "\"a1\""
    responseName: account
    branches:
      Chat: 6
    fallback: 5
  - id: 5
    type: Operation
    operation:
      name: OtherMemberTypename_123456789101112_5
      kind: Query
      document: |
        query OtherMemberTypename_123456789101112_5 {
          account: node(id: "a1") {
            __typename
          }
        }
      hash: e60bfd00637a93172a6758bc5bc39d1ca6c0d963ed2ca846b7bbe0c19a39c3f5
      shortHash: e60bfd00
      xxHash: 17917100611495745691
    resultSelectionSet: >-
      { account }
    dependencies:
      - id: 4
  - id: 6
    type: Operation
    schema: b
    operation:
      name: OtherMemberTypename_123456789101112_6
      kind: Query
      document: |
        query OtherMemberTypename_123456789101112_6 {
          account: node(id: "a1") {
            __typename
            ... on Chat {
              __typename
              id
            }
          }
        }
      hash: 607066d917002acb5561605be94cb8b7554989eacaa29a9e90f2802315880645
      shortHash: 607066d9
      xxHash: 2773804431611637312
    lookupTypeName: Chat
    resultSelectionSet: >-
      { account { __typename ... on Chat { __typename id } } }
    dependencies:
      - id: 4
      - id: 5

```

## 05-aliased

```yaml
id: 6b66d8e7f91181412424c97e8f5f8751006725cf9d42122408b9df426abc4289
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
    kind: Query
    id: "123456789101112"
    hash: "123456789101112"
    shortHash: "123456789101112"
searchSpace: 1
expandedNodes: 1
nodes:
  - id: 1
    type: Node
    idValue: "\"c1\""
    responseName: chat
    branches:
      Account: 3
    fallback: 2
  - id: 2
    type: Operation
    operation:
      name: Aliased_123456789101112_2
      kind: Query
      document: |
        query Aliased_123456789101112_2 {
          chat: node(id: "c1") {
            __typename
          }
        }
      hash: da3e8149ae1a93c0c21e7455d27f349572fbcc0ee2c9d36a40d794156854f77f
      shortHash: da3e8149
      xxHash: 4037468415727643527
    resultSelectionSet: >-
      { chat }
    dependencies:
      - id: 1
  - id: 3
    type: Operation
    schema: a
    operation:
      name: Aliased_123456789101112_3
      kind: Query
      document: |
        query Aliased_123456789101112_3 {
          chat: node(id: "c1") {
            __typename
            ... on Account {
              __typename
              id
            }
          }
        }
      hash: 114599e213640cc315851b9fbbf7798875b394f938731fb1cfb9b9a8adf8039b
      shortHash: "114599e2"
      xxHash: 15394870183805517774
    lookupTypeName: Account
    resultSelectionSet: >-
      { chat { __typename ... on Account { __typename id } } }
    dependencies:
      - id: 1
      - id: 2
  - id: 4
    type: Node
    idValue: "\"a1\""
    responseName: account
    branches:
      Account: 7
      Chat: 6
    fallback: 5
  - id: 5
    type: Operation
    operation:
      name: Aliased_123456789101112_5
      kind: Query
      document: |
        query Aliased_123456789101112_5 {
          account: node(id: "a1") {
            __typename
          }
        }
      hash: 6991ce74a847e6a2f2960092a84d6f23c26fa9bfd2cfac108d09cf1fa52d5036
      shortHash: 6991ce74
      xxHash: 11253180084889138527
    resultSelectionSet: >-
      { account }
    dependencies:
      - id: 4
  - id: 6
    type: Operation
    schema: b
    operation:
      name: Aliased_123456789101112_6
      kind: Query
      document: |
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
      hash: 605e3d5f91a8e7fbf684d36d79e13378fe3956f2da5a9244f9394b74807784d8
      shortHash: 605e3d5f
      xxHash: 158011127903755112
    lookupTypeName: Chat
    resultSelectionSet: >-
      { account { __typename ... on Chat { __typename id id } } }
    dependencies:
      - id: 4
      - id: 5
  - id: 7
    type: Operation
    schema: a
    operation:
      name: Aliased_123456789101112_7
      kind: Query
      document: |
        query Aliased_123456789101112_7 {
          account: node(id: "a1") {
            __typename
            ... on Account {
              __typename
              id
            }
          }
        }
      hash: 5cccb60b7fd505bd29de95fc43cb0f78679ee877d6e2aa6162998d85d1e5bf5a
      shortHash: 5cccb60b
      xxHash: 9664455848649204769
    lookupTypeName: Account
    resultSelectionSet: >-
      { account { __typename ... on Account { __typename id } } }
    dependencies:
      - id: 4
      - id: 5

```

## 10-member-split

```yaml
id: 4be5690c2978e4c5e37453dd41a9cb818cab567751c4b886970d66f1e9c3a502
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
    kind: Query
    id: "123456789101112"
    hash: "123456789101112"
    shortHash: "123456789101112"
searchSpace: 1
expandedNodes: 1
nodes:
  - id: 1
    type: Node
    idValue: "\"a1\""
    responseName: node
    branches:
      Account: 4
      Chat: 3
    fallback: 2
  - id: 2
    type: Operation
    operation:
      name: MemberSplit_123456789101112_2
      kind: Query
      document: |
        query MemberSplit_123456789101112_2 {
          node(id: "a1") {
            __typename
          }
        }
      hash: 0867424437fd80e841386c47ccd521005dc02281af64820c58469fc972a44e86
      shortHash: "08674244"
      xxHash: 2897941906775815673
    resultSelectionSet: >-
      { node }
    dependencies:
      - id: 1
  - id: 3
    type: Operation
    schema: b
    operation:
      name: MemberSplit_123456789101112_3
      kind: Query
      document: |
        query MemberSplit_123456789101112_3 {
          node(id: "a1") {
            __typename
            ... on Chat {
              __typename
              id
            }
          }
        }
      hash: d4f0532db3b601331d5b99e0d0db62747acd0e2ed9b44744ec03e40d817b0ceb
      shortHash: d4f0532d
      xxHash: 13302649507895849263
    lookupTypeName: Chat
    resultSelectionSet: >-
      { node { __typename ... on Chat { __typename id } } }
    dependencies:
      - id: 1
      - id: 2
  - id: 4
    type: Operation
    schema: a
    operation:
      name: MemberSplit_123456789101112_4
      kind: Query
      document: |
        query MemberSplit_123456789101112_4 {
          node(id: "a1") {
            __typename
            ... on Account {
              __typename
              id
            }
          }
        }
      hash: 83688634c4bdd2f3947a9150910cdb44820257f4f86627fbe7ab2d6532a7a719
      shortHash: "83688634"
      xxHash: 16833257180747640345
    lookupTypeName: Account
    resultSelectionSet: >-
      { node { __typename ... on Account { __typename id } } }
    dependencies:
      - id: 1
      - id: 2

```

## 20-node-id-chat

```yaml
id: 643829da89e94c99933f369ecf8343deefd5e482b329bf45726be939f80e3b6d
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
    kind: Query
    id: "123456789101112"
    hash: "123456789101112"
    shortHash: "123456789101112"
searchSpace: 1
expandedNodes: 1
nodes:
  - id: 1
    type: Node
    idValue: "\"c1\""
    responseName: node
    branches:
      Account: 4
      Chat: 3
    fallback: 2
  - id: 2
    type: Operation
    operation:
      name: NodeIdChat_123456789101112_2
      kind: Query
      document: |
        query NodeIdChat_123456789101112_2 {
          node(id: "c1") {
            __typename
          }
        }
      hash: 59074aa68ff309b4ba687c692e817d50780135bdc93bec6a3563a575f6444247
      shortHash: 59074aa6
      xxHash: 12637845175516169580
    resultSelectionSet: >-
      { node }
    dependencies:
      - id: 1
  - id: 3
    type: Operation
    schema: b
    operation:
      name: NodeIdChat_123456789101112_3
      kind: Query
      document: |
        query NodeIdChat_123456789101112_3 {
          node(id: "c1") {
            __typename
            ... on Chat {
              __typename
              id
            }
          }
        }
      hash: 0a3e49dbf9937f44e8aa0fe86af84cc15367a58f83380576399dbc3528161f88
      shortHash: 0a3e49db
      xxHash: 16168447767309487088
    lookupTypeName: Chat
    resultSelectionSet: >-
      { node { __typename ... on Chat { __typename id } } }
    dependencies:
      - id: 1
      - id: 2
  - id: 4
    type: Operation
    schema: a
    operation:
      name: NodeIdChat_123456789101112_4
      kind: Query
      document: |
        query NodeIdChat_123456789101112_4 {
          node(id: "c1") {
            __typename
            ... on Account {
              __typename
              id
            }
          }
        }
      hash: c4e7e46ac88e8d871e8ae4b8b68a93167c723cc015354ce1ed45e729ac77300e
      shortHash: c4e7e46a
      xxHash: 10974424164463405783
    lookupTypeName: Account
    resultSelectionSet: >-
      { node { __typename ... on Account { __typename id } } }
    dependencies:
      - id: 1
      - id: 2

```
