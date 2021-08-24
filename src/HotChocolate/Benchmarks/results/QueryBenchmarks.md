```ini

BenchmarkDotNet=v0.12.1, OS=macOS 11.4 (20F71) [Darwin 20.5.0]
Intel Core i9-10910 CPU 3.60GHz, 1 CPU, 20 logical and 10 physical cores
.NET Core SDK=5.0.301
  [Host]     : .NET Core 5.0.7 (CoreCLR 5.0.721.25508, CoreFX 5.0.721.25508), X64 RyuJIT
  DefaultJob : .NET Core 5.0.7 (CoreCLR 5.0.721.25508, CoreFX 5.0.721.25508), X64 RyuJIT


```

| Method                                |     Mean |     Error |    StdDev |   Median | Rank |   Gen 0 |  Gen 1 | Gen 2 | Allocated |
| ------------------------------------- | -------: | --------: | --------: | -------: | ---: | ------: | -----: | ----: | --------: |
| Sessions_TitleAndAbstract             | 2.201 ms | 0.0440 ms | 0.0837 ms | 2.177 ms |    1 | 19.5313 | 3.9063 |     - | 230.26 KB |
| Sessions_TitleAndAbstractAndTrackName | 2.764 ms | 0.0524 ms | 0.0603 ms | 2.747 ms |    2 | 11.7188 |      - |     - | 126.26 KB |

**Sessions_TitleAndAbstract**

```graphql
{
  sessions(first: 50) {
    nodes {
      title
      abstract
    }
  }
}
```

**Sessions_TitleAndAbstractAndTrackName**

```graphql
{
  sessions {
    nodes {
      title
      abstract
      track {
        name
      }
    }
  }
}
```
