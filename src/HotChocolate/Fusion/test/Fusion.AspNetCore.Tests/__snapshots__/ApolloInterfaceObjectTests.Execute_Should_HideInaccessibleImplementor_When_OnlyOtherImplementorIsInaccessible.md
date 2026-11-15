# Execute_Should_HideInaccessibleImplementor_When_OnlyOtherImplementorIsInaccessible

## typename

```text
{
  "data": {
    "rows": [
      {
        "account": {
          "__typename": "Personal",
          "id": "a1",
          "balance": 11
        }
      },
      {
        "account": {
          "__typename": "Personal",
          "id": "a2",
          "balance": 22
        }
      },
      {
        "account": null
      }
    ]
  }
}
```

## no-typename

```text
{
  "data": {
    "rows": [
      {
        "account": {
          "id": "a1",
          "balance": 11
        }
      },
      {
        "account": {
          "id": "a2",
          "balance": 22
        }
      },
      {
        "account": {
          "id": "a3",
          "balance": 33
        }
      }
    ]
  }
}
```

## fragment

```text
{
  "data": {
    "rows": [
      {
        "account": {
          "id": "a1",
          "name": "Ann"
        }
      },
      {
        "account": {
          "id": "a2",
          "name": "Abe"
        }
      },
      {
        "account": null
      }
    ]
  }
}
```
