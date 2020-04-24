# Filter & Operations Kinds

Filtering can be broken down into different kinds of filters that then have different operations.
The filter kind is bound to the type. A string is fundamentally something different than an array or an object.
Each filter kind has different operations that can be applied to it. Some operations are unique to a filter and some operations are shared across multiple filters.
e.g. A string filter has string specific operations like `Contains` or `EndsWith` but still shares the operations `Equals` and `NotEquals` with the boolean filter.

## Filter Kinds

Hot Chocolate knows following filter kinds

| Kind       | Operations                                                                                                                                                          |
| ---------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| String     | Equals, In, EndsWith, StartsWith, Contains, NotEquals, NotIn, NotEndsWith, NotStartsWith, NotContains                                                                |
| Bool       | Equals, NotEquals                                                                                                                                                    |
| Object     | Equals                                                                                                                                                               |
| Array      | Some, Any, All, None                                                                                                                                                 |
| Comparable | Equals, In, GreaterThan, GreaterThanOrEqual, LowerThan, LowerThanOrEqual, NotEquals, NotIn, NotGreaterThan, NotGreaterThanOrEqual, NotLowerThan, NotLowerThanOrEqual |

## Operations Kinds

Hot Chocolate knows following operation kinds

| Kind                   | Operations                                                                                           |
| ---------------------- | ----------------------------------------------------------------------------------------------------- |
| Equals                 | Compares the equality of input value and property value                                               |
| NotEquals              | negation of Equals                                                                                    |
| In                     | Checks if the property value is contained in a given list of input values                             |
| NotIn                  | negation of In                                                                                        |
| GreaterThan            | checks if the input value is greater than the property value                                          |
| NotGreaterThan         | negation of GreaterThan                                                                               |
| GreaterThanOrEquals    | checks if the input value is greater than or equal to the property value                              |
| NotGreaterThanOrEquals | negation of GreaterThanOrEquals                                                                       |
| LowerThan              | checks if the input value is lower than the property value                                            |
| NotLowerThan           | negation of LowerThan                                                                                 |
| LowerThanOrEquals      | checks if the input value is lower than or equal to the property value                                |
| NotLowerThanOrEquals   | negation of LowerThanOrEquals                                                                         |
| EndsWith               | checks if the property value ends with the input value                                                |
| NotEndsWith            | negation of EndsWith                                                                                  |
| StartsWith             | checks if the property value starts with the input value                                              |
| NotStartsWith          | negation of StartsWith                                                                                |
| Contains               | checks if the input value is contained in the property value                                          |
| NotContains            | negation of Contains                                                                                  |
| Some                   | checks if at least one element in the collection exists                                               |
| Some                   | checks if at least one element of the property value meets the condition provided by the input value  |
| None                   | checks if no element of the property value meets the condition provided by the input value            |
| All                    | checks if all least one element of the property value meets the condition provided by the input value |
