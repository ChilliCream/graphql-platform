### Uniq
**Directive**

```graphql
directive @uniq(count: Int!) repeatable on QUERY | MUTATION | SUBSCRIPTION | FIELD
```

:: *@uniq* can be applied a list any depth.
*@uniq* returns a duplicate-free version of the list.

Given the following data:

```json example
{
  "stringList": [ "a", "a", "b" ]
}
```

The execution of the following query:

```graphql example
{
  stringList @uniq
}
```

will retrun the following result:

```json example
{
  "stringList": [ "a", "b" ]
}
```

**Execution**

{node} is the value node where the directive is applied

RewriteUniq(node):
* Let {size} be the value of the argument `size` of the directive
* If {node} is *ObjectValue*
  * Assert: *AG0001*
* Otherwise If {node} is *ScalarValue*
  * Assert: *AG0004*
* Otherwise 
  * Return {RewriteUniqArray(node)}

RewriteUniqArray(value):
* Let {result} be a ordered set
* For each {element} in {value}
  * If {IsConvertibleToComparable(element)}
    * Let {convertedValue} be {ConvertToComparable(fieldValue)}
    * If {convertedValue} does NOT exist in {result}
      * Add {convertedValue} to {result}
  * Otherwise 
    * Add {element} to {result}
* Return {result}
