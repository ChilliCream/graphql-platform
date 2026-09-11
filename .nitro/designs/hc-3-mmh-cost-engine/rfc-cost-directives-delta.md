# Cost Directives in Execution Schemas

This section defines the cost metadata emitted in a composite execution schema. Public directives contain the folded values used by the gateway. Repeatable `fusion__` directives retain the compatible source usages that contributed to those values.

## Directive Definitions

The public directives are non-repeatable and have no argument defaults:

<!-- prettier-ignore -->
```graphql
directive @cost(weight: String!) on
  | SCALAR
  | OBJECT
  | FIELD_DEFINITION
  | ARGUMENT_DEFINITION
  | ENUM
  | INPUT_FIELD_DEFINITION

directive @listSize(
  assumedSize: Int
  slicingArguments: [String!]
  slicingArgumentDefaultValue: Int
  sizedFields: [String!]
  requireOneSlicingArgument: Boolean
) on FIELD_DEFINITION
```

The internal provenance directives are repeatable. `schema` is a required value from the generated `fusion__Schema` enum. Every other `@fusion__listSize` argument is nullable and has no default.

<!-- prettier-ignore -->
```graphql
directive @fusion__cost(
  schema: fusion__Schema!
  weight: String!
) repeatable on
  | SCALAR
  | OBJECT
  | FIELD_DEFINITION
  | ARGUMENT_DEFINITION
  | ENUM
  | INPUT_FIELD_DEFINITION

directive @fusion__listSize(
  assumedSize: Int
  requireOneSlicingArgument: Boolean
  schema: fusion__Schema!
  sizedFields: [String!]
  slicingArgumentDefaultValue: Int
  slicingArguments: [String!]
) repeatable on FIELD_DEFINITION
```

`@cost` has direct type locations for object, scalar, and enum types. Interface and union types are not direct `@cost` locations.

## Collection and Projection

Composition collects compatible source `@cost` and `@listSize` usages. A source usage without a local definition receives the canonical definition. A local definition is compatible when its directive name and repeatability match, each declared argument has the canonical name and type, and its locations are a subset of the canonical locations. A compatible source definition may omit canonical arguments. Argument defaults and descriptions do not affect compatibility.

The collected source metadata has two projections:

- One repeatable `fusion__` provenance entry for each compatible source that declares the usage. The entry preserves that source's argument literals and spelling.
- One folded public directive used by the gateway. Public numeric weights are normalized, so a source weight of `"1.0"` can produce a public weight of `"1"`.

A public directive is emitted only when at least one serving source has a compatible usage. Its public definition is emitted with it. The public usage precedes its internal provenance entries in the execution schema.

An unannotated serving source does not receive a provenance entry. Its coordinate default still participates in the public `@cost` fold, so the visible `@fusion__cost` entries are not sufficient to reconstruct the public weight without the full set of serving sources.

## Cost Fold

After the emission gate is open, the public `@cost(weight:)` is the numeric maximum of every serving source's effective weight. A compatible declared usage contributes its signed weight. A serving source without a compatible declared usage contributes the coordinate default:

| Coordinate                                                             | Default weight |
| ---------------------------------------------------------------------- | -------------: |
| Object type                                                            |            `1` |
| Scalar or enum type                                                    |            `0` |
| Output field whose named return type is an object, interface, or union |            `1` |
| Output field whose named return type is a scalar or enum               |            `0` |
| Argument or input field whose named type is an input object            |            `1` |
| Any other argument or input field                                      |            `0` |

The named type is determined after unwrapping list and non-null types. An interface or union can determine an output field's default even though it is not a direct `@cost` type location.

### Effective Weight Example

Source A declares a negative weight, while source B serves the same composite-returning field without `@cost`:

```graphql
# Source A
type Query {
  book: Book @cost(weight: "-7")
}

# Source B
type Query {
  book: Book
}
```

The cost-specific execution-schema excerpt is:

```graphql
type Query {
  book: Book @cost(weight: "1") @fusion__cost(schema: A, weight: "-7")
}
```

Source B contributes the output-field default of `1` to the public maximum and has no `@fusion__cost` entry.

## List-Size Fold

Only compatible sources that declare `@listSize` participate in its fold:

| Argument                      | Public projection                                                                                                                                         |
| ----------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `assumedSize`                 | Maximum non-null value. Omit when every value is null or absent.                                                                                          |
| `slicingArguments`            | Plain deterministic union in first-seen order with ordinal duplicate removal. Null and absent lists are empty.                                            |
| `sizedFields`                 | Plain deterministic union in first-seen order with ordinal duplicate removal. Null and absent lists are empty.                                            |
| `requireOneSlicingArgument`   | Resolve each source value as described below, then use `true` if any value is true, otherwise `false` if any value is false, otherwise omit the argument. |
| `slicingArgumentDefaultValue` | Maximum non-null value. Omit when every value is null or absent.                                                                                          |

The `slicingArguments` and `sizedFields` unions are not filtered against the composite field. A missing slicing-argument name produces no slicing value at runtime. A missing sized-field name produces no inherited child size. In both cases, list-size selection continues through its remaining fallbacks.

For `requireOneSlicingArgument`, composition uses the usage value when it is non-null. An omitted or explicit null value uses that source definition's Boolean default when one is declared. A source with neither a non-null usage value nor a definition default contributes null. The provenance entry copies the original usage and does not materialize the source definition default.

## Slicing Argument Default Value

`slicingArgumentDefaultValue` is a nullable ChilliCream extension with no directive-definition default. It is optional in public definitions, internal provenance definitions, source definitions, and directive usages. A compatible IBM-shaped source definition can omit the argument and compose without a warning.

At runtime, the selected list size is the first applicable value in this order:

1. An inherited size from a parent `sizedFields` entry.
2. The maximum slicing argument that resolves to an integer after coercion, including real schema argument defaults.
3. `slicingArgumentDefaultValue` when no slicing argument resolves to an integer.
4. `assumedSize`.
5. The gateway's default list size.

An undefined slicing variable without a schema argument default and an explicit null value can reach `slicingArgumentDefaultValue`. A real schema argument default is selected at step 2. `slicingArgumentDefaultValue` does not cap a supplied slicing value.

Composition skips absent or null `slicingArgumentDefaultValue` values when calculating the maximum. Mixed sources project the maximum value that is present. When no source provides a value, the public usage omits the argument and runtime selection continues from slicing arguments to `assumedSize`.

## Execution-Schema Contract

When composition collects at least one compatible source usage, the execution schema must contain the canonical public directive definition and the folded public usage. The gateway reads and enforces the public `@cost` and `@listSize` directives. An execution-schema producer or transformer must not strip them.

The repeatable `@fusion__cost` and `@fusion__listSize` entries remain as per-source provenance. They are not the gateway's enforcement input. Their preserved source literals can differ from effective values used by the fold because unannotated sources contribute cost defaults and source definition defaults can supply `requireOneSlicingArgument`.

## Spec-Compatible Source Example

Source A uses a definition that omits `slicingArgumentDefaultValue` and declares the IBM default for `requireOneSlicingArgument`. The usage omits the Boolean argument and names a slicing argument that is not present on the field. Source B serves the field without `@listSize`.

```graphql
# Source A
directive @listSize(
  assumedSize: Int
  slicingArguments: [String!]
  sizedFields: [String!]
  requireOneSlicingArgument: Boolean = true
) on FIELD_DEFINITION

type Query {
  items: [Item] @listSize(assumedSize: 2, slicingArguments: ["missing"])
}

# Source B
type Query {
  items: [Item]
}
```

The cost-specific execution-schema excerpt is:

```graphql
type Query {
  items: [Item]
    @listSize(assumedSize: 2, slicingArguments: ["missing"], requireOneSlicingArgument: true)
    @fusion__listSize(schema: A, assumedSize: 2, slicingArguments: ["missing"])
}
```

The public usage materializes source A's definition default. The provenance entry preserves the omitted Boolean argument. The unmatched `"missing"` name remains in both projections and yields no runtime slicing value before the remaining fallbacks are evaluated.

## Mixed Extension Example

Source A declares `@listSize(assumedSize: 5)`. Source B declares `@listSize(assumedSize: 10, slicingArgumentDefaultValue: 20)`. The cost-specific execution-schema excerpt is:

```graphql
type Query {
  items: [Item]
    @listSize(assumedSize: 10, slicingArgumentDefaultValue: 20)
    @fusion__listSize(schema: A, assumedSize: 5)
    @fusion__listSize(schema: B, assumedSize: 10, slicingArgumentDefaultValue: 20)
}
```

The public `assumedSize` is `10`, and the public `slicingArgumentDefaultValue` is `20`. Source A's provenance entry omits the extension argument.
