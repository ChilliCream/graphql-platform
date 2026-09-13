# BatchResolver_Should_Deliver_Data_When_VariableBatchRequestContainsDeferFragment

## Set 0

```text
{
  "variableIndex": 0,
  "data": {
    "ready": "ready"
  },
  "pending": [
    {
      "id": "3",
      "path": []
    }
  ],
  "incremental": [
    {
      "id": "3",
      "data": {
        "productById": {
          "name": "Product 1"
        }
      }
    }
  ],
  "completed": [
    {
      "id": "3"
    }
  ],
  "hasNext": false
}

```

## Set 1

```text
{
  "variableIndex": 1,
  "data": {
    "ready": "ready"
  },
  "pending": [
    {
      "id": "5",
      "path": []
    }
  ],
  "incremental": [
    {
      "id": "5",
      "data": {
        "productById": {
          "name": "Product 2"
        }
      }
    }
  ],
  "completed": [
    {
      "id": "5"
    }
  ],
  "hasNext": false
}

```
