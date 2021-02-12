# Unions

The following example shows a union type switched with inline fragments. The union type cases are fully exhausted although the backend could change and introduce new cases.

```graphql
query search {
  search(text: "foo") {
    ... on Human {
      homePlanet
    }
    ... on Droid {
      primaryFunction
    }
    ... on Starship {
      name
    }
  }
}
```

The following example shows a union type switched with fragment spreads. The union type cases are fully exhausted although the backend could change and introduce new cases.

```graphql
query search {
  search(text: "foo") {
    ...SomeHuman
    ...SomeDroid
    ...SomeStarship
  }
}

fragment SomeHuman on Human {
  homePlanet
}

fragment SomeDroid on Droid {
  primaryFunction
}

fragment SomeStarship on Starship {
  name
}
```

The following example shows a union type switched with inline fragments. The union type cases are **NOT** fully exhausted.

```graphql
query search {
  search(text: "foo") {
    ... on Human {
      homePlanet
    }
    ... on Droid {
      primaryFunction
    }
  }
}
```

The following case shows a mixed approach where the cases are fully exhausted.

```graphql
query search {
  search(text: "foo") {
    ... on Human {
      homePlanet
    }
    ...SomeDroid
    ...SomeStarship
  }
}

fragment SomeDroid on Droid {
  primaryFunction
}

fragment SomeStarship on Starship {
  name
}
```

We also could have multiple fragments that target interfaces instead of the actual types.

```graphql
query search {
  search(text: "foo") {
    ... on Character {
      name
    }
    ...SomeStarship
  }
}

fragment SomeStarship on Starship {
  name
}
```

Although we did not explicitly filly exhaust all type cases we actually have covered all type cases since all types apart from Starship implement the interface `Character`.

# Interfaces

```graphql
query getHero {
  hero {
    name
    friends {
      nodes {
        name
      }
    }
  }
}
```

```graphql
query getHero {
  hero {
    ...Hero
  }
}

fragment Hero on Character {
  ...HasName
  ...HasFriends
}

fragment Friend on CharacterConnection {
  nodes {
    ...HasName
  }
}

fragment HasName on Character {
  name
}

fragment HasFriends on Character {
  friends {
    ...Friend
  }
}
```

```graphql
query getHero {
  hero {
    ...Hero
  }
}

fragment Hero on Character {
  ...HasName
  ...SomeDroid
  ...SomeHuman
}

fragment SomeDroid on Droid {
  primaryFunction
}

fragment SomeHuman on Human {
  homePlanet
}

fragment HasName on Character {
  name
}
```

```graphql
query getHero {
  hero {
    ...HasName
    ...SomeDroid
    ...SomeHuman
  }
}

fragment SomeDroid on Droid {
  primaryFunction
}

fragment SomeHuman on Human {
  homePlanet
}

fragment HasName on Character {
  name
}
```

```graphql
query getHero {
  hero {
    ...Hero
  }
}

fragment Hero on Character {
  ...HasName
  ...SomeDroid
  ...SomeHuman
}

fragment SomeDroid on Droid {
  ...HasName
  primaryFunction
}

fragment SomeHuman on Human {
  ...HasName
  homePlanet
}

fragment HasName on Character {
  name
}
```

# Objects

```graphql
query search {
  droid(id: "foo") {
    name
  }
}
```

```graphql
query search {
  droid(id: "foo") {
    ...SomeDroid
  }
}

fragment SomeDroid on Droid {
  name
}
```

```graphql
query getBars {
  foo {
    bars {
      baz
    }
  }
}
```
