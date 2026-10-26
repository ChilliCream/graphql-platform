# Execute_Should_ResolveEachNodeTypeThroughItsKeyOwner_When_KeyIsExternalInOtherNodeLookupSchema

## 00-node-id

```text
{
  "data": {
    "node": {
      "id": "a1"
    }
  }
}
```

## 01-typenames

```text
{
  "data": {
    "account": {
      "__typename": "Account"
    },
    "chat": {
      "__typename": "Chat"
    }
  }
}
```

## 02-member-fields

```text
{
  "data": {
    "account": {
      "id": "a1",
      "username": "a1-username"
    },
    "chat": {
      "id": "c1",
      "text": "c1-text"
    }
  }
}
```

## 03-other-member

```text
{
  "data": {
    "account": {},
    "chat": {}
  }
}
```

## 04-other-member-typename

```text
{
  "data": {
    "account": {
      "__typename": "Account"
    },
    "chat": {
      "__typename": "Chat"
    }
  }
}
```

## 05-aliased

```text
{
  "data": {
    "account": {
      "id": "a1"
    },
    "chat": {
      "__typename": "Chat"
    }
  }
}
```

## 10-member-split

```text
{
  "data": {
    "node": {
      "id": "a1"
    }
  }
}
```

## 20-node-id-chat

```text
{
  "data": {
    "node": {
      "id": "c1"
    }
  }
}
```
