### Max

**Directive**

```graphql
directive @max(by: String!) repeatable on QUERY | MUTATION | SUBSCRIPTION | FIELD
```

:: *@max* search a list for the highest value. If `by` is provided and *@max* is applied to a list of objects of depth
1, it returns the object where the value of the field with name `by` is the highest Nested lists can be flattened with *
@flatten* first. If no value was found, *@max* retruns the first element of the list or {null};

Given the following data:

```json example
{
  "list": [
    {
      "string": "a",
      "int": 1
    },
    {
      "string": "b",
      "int": 2
    },
    {
      "string": "c",
      "int": 3
    }
  ],
  "intList": [ 1, 2, 3]
}
```

The execution of the following query:

```graphql example
{
  list @max(by: "int"){
    string
    int
  }
  intList @max
}
```

will retrun the following result:

```json example
{
  "list": {
    "string": "c",
    "int": 3
  },
  "intList": 3
}
```

**Execution**

{node} is the value node where the directive is applied

RewriteMax(node):

* Let {by} be the value of the argument `by` of the directive
* If {node} is *ObjectValue*
  * Assert: *AG0001*
* Otherwise If {node} is *ScalarValue*
  * Assert: *AG0004*
* Otherwise
  * If {by} is NOT specified
    * Return {RewriteMaxArray(node)}
  * Otherwise
    * Return {RewriteMaxArrayBy(node, by)}

RewriteMaxArray(value):
* If {value} has 0 elements
  * Return {null}
* Let {lastValue} be a {null}
* Let {result} be a the first element of {value}
* For each {element} in {value}
  * If {element} is *ScalarValue*
    * If {IsConvertibleToComparable(element)}
      * Let {convertedValue} be {ConvertToComparable(element)}
      * If {lastValue} is {null} OR {convertedValue} is greater than {lastValue}
        * Set value of {result} to {element}
        * Set value of {lastValue} to {convertedValue}
* Return {result}

RewriteMaxArrayBy(value):
* If {value} has 0 elements
  * Return {null}
* Let {lastValue} be a {null}
* Let {result} be a {null}
* If the first element of {value} is *ObjectValue*
  * Set {result} to the first element of {value}
* For each {element} in {value}
  * If {element} is *ListValue*
    * Assert: *AG0003*
  * Otherwise If {element} is *ScalarValue*
    * Assert: *AG0002*
  * Otherwise If {element} is *ObjectValue*
    * If {Exists(element, by)}
      * Let {fieldValue} be {Get(element, by)}
      * If {IsConvertibleToComparable(fieldValue)}
        * Let {convertedValue} be {ConvertToComparable(fieldValue)}
        * If {result} is {null} OR {lastValue} is {null} OR {convertedValue} is greater than {lastValue}
          * Set value of {result} to {element}
          * Set value of {lastValue} to {convertedValue}
* Return {result}
