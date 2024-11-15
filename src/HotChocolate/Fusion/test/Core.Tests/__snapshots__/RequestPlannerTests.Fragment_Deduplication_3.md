# Fragment_Deduplication_3

## UserRequest

```graphql
query searchQuery($query: String!, $filters: [FilterInput!], $sortOrder: ShopSearchSortOrder!, $searchQueryConfig: ShopSearchConfigInput!) {
  shopSearch(query: $query, searchQueryConfig: $searchQueryConfig) {
    ... searchEngineResultsPage
    ... searchEngineResultsPageProducts_1rZpll
    products(first: 48, filters: $filters, sortOrder: $sortOrder) {
      productFilters {
        edges {
          node {
            __typename
            identifier
          }
        }
      }
    }
    id
  }
}

fragment alternativeQuerySuggestions on ShopSearchResult {
  alternativeQuerySuggestions {
    productCount
    productPreviewImageUrls
    queryString
  }
}

fragment galaxusReferral on ShopSearchResult {
  galaxusReferral {
    portalUrl
    productCount
    products {
      id
    }
  }
}

fragment searchEngineBlogTeasersSection on ShopSearchResult {
  blogPages {
    nodes {
      id
    }
  }
}

fragment searchEngineCommunitySection on ShopSearchResult {
  communityItems(first: 3) {
    nodes {
      __typename
      ... on CommunityDiscussion {
        databaseId
      }
      ... on ProductQuestion {
        databaseId
      }
      ... on ProductReview {
        databaseId
      }
      ... on Node {
        __isNode: __typename
        id
      }
    }
  }
}

fragment searchEngineProductsSection on ShopSearchResultProductsConnection {
  edges {
    node {
      id
    }
  }
  productFilters {
    edges {
      node {
        __typename
        ... on CheckboxFilter {
          __typename
          identifier
          title
        }
        ... on RangeFilter {
          __typename
          identifier
          title
        }
      }
    }
  }
  totalCount
  pageInfo {
    hasNextPage
    endCursor
  }
}

fragment searchEngineResultsPage on ShopSearchResult {
  ... searchEngineTitleSection
  ... useSerpTrackingShopSearchResult
  ... searchEngineBlogTeasersSection
  ... searchEngineCommunitySection
  ... zeroResults
  additionalQueryInfo {
    lastProductSearchPass
  }
  alternativeQuerySuggestions {
    __typename
  }
  galaxusReferral {
    __typename
  }
  redirectionUrl
}

fragment searchEngineResultsPageProducts_1rZpll on ShopSearchResult {
  products(first: 48, filters: $filters, sortOrder: $sortOrder) {
    edges {
      node {
        id
        __typename
      }
      cursor
    }
    ... searchEngineProductsSection
    ... vectorProductsSection
    ... useSerpTrackingProducts
    productFilters {
      ... searchFiltersProductFilters
    }
    quickFilters {
      ... searchFiltersQuickFilters
    }
    pageInfo {
      endCursor
      hasNextPage
    }
  }
  id
}

fragment searchEngineTitleSection on ShopSearchResult {
  additionalQueryInfo {
    correctedQuery
    didYouMeanQuery
    lastProductSearchPass
  }
}

fragment searchFiltersProductFilters on ShopSearchResultProductFiltersConnection {
  edges {
    node {
      __typename
      ... on CheckboxFilter {
        __typename
        identifier
        title
        commonOptions {
          count
          optionIdentifier
          title
          tooltip {
            absoluteUrl
            text
          }
        }
        pinnedOptions {
          count
          optionIdentifier
          title
          tooltip {
            absoluteUrl
            text
          }
        }
        tooltip {
          absoluteUrl
          text
        }
      }
      ... on RangeFilter {
        __typename
        identifier
        title
        min
        max
        step
        dataPoints {
          count
          value
        }
        topOutliersMerged
        unitId
        unitName
        tooltip {
          absoluteUrl
          text
        }
      }
    }
  }
}

fragment searchFiltersQuickFilters on ShopSearchResultQuickFiltersConnection {
  edges {
    node {
      disabled
      filterIdentifier
      optionIdentifier
      optionTitle
      filterTitle
    }
  }
}

fragment useSerpTrackingProducts on ShopSearchResultProductsConnection {
  totalCount
}

fragment useSerpTrackingShopSearchResult on ShopSearchResult {
  brands {
    edges {
      node {
        id
      }
    }
  }
  productTypes {
    edges {
      node {
        id
      }
    }
  }
  redirectionUrl
  galaxusReferral {
    __typename
  }
  alternativeQuerySuggestions {
    queryString
  }
  additionalQueryInfo {
    correctedQuery
    didYouMeanQuery
    isRerankedQuery
    lastProductSearchPass
    testGroup
  }
}

fragment vectorProductsSection on ShopSearchResultProductsConnection {
  edges {
    node {
      id
    }
  }
  pageInfo {
    hasNextPage
    endCursor
  }
  totalCount
}

fragment zeroResults on ShopSearchResult {
  ... alternativeQuerySuggestions
  ... galaxusReferral
}
```

## QueryPlan

```json
{
  "document": "query searchQuery($query: String!, $filters: [FilterInput!], $sortOrder: ShopSearchSortOrder!, $searchQueryConfig: ShopSearchConfigInput!) { shopSearch(query: $query, searchQueryConfig: $searchQueryConfig) { ... searchEngineResultsPage ... searchEngineResultsPageProducts_1rZpll products(first: 48, filters: $filters, sortOrder: $sortOrder) { productFilters { edges { node { __typename identifier } } } } id } } fragment alternativeQuerySuggestions on ShopSearchResult { alternativeQuerySuggestions { productCount productPreviewImageUrls queryString } } fragment galaxusReferral on ShopSearchResult { galaxusReferral { portalUrl productCount products { id } } } fragment searchEngineBlogTeasersSection on ShopSearchResult { blogPages { nodes { id } } } fragment searchEngineCommunitySection on ShopSearchResult { communityItems(first: 3) { nodes { __typename ... on CommunityDiscussion { databaseId } ... on ProductQuestion { databaseId } ... on ProductReview { databaseId } ... on Node { __isNode: __typename id } } } } fragment searchEngineProductsSection on ShopSearchResultProductsConnection { edges { node { id } } productFilters { edges { node { __typename ... on CheckboxFilter { __typename identifier title } ... on RangeFilter { __typename identifier title } } } } totalCount pageInfo { hasNextPage endCursor } } fragment searchEngineResultsPage on ShopSearchResult { ... searchEngineTitleSection ... useSerpTrackingShopSearchResult ... searchEngineBlogTeasersSection ... searchEngineCommunitySection ... zeroResults additionalQueryInfo { lastProductSearchPass } alternativeQuerySuggestions { __typename } galaxusReferral { __typename } redirectionUrl } fragment searchEngineResultsPageProducts_1rZpll on ShopSearchResult { products(first: 48, filters: $filters, sortOrder: $sortOrder) { edges { node { id __typename } cursor } ... searchEngineProductsSection ... vectorProductsSection ... useSerpTrackingProducts productFilters { ... searchFiltersProductFilters } quickFilters { ... searchFiltersQuickFilters } pageInfo { endCursor hasNextPage } } id } fragment searchEngineTitleSection on ShopSearchResult { additionalQueryInfo { correctedQuery didYouMeanQuery lastProductSearchPass } } fragment searchFiltersProductFilters on ShopSearchResultProductFiltersConnection { edges { node { __typename ... on CheckboxFilter { __typename identifier title commonOptions { count optionIdentifier title tooltip { absoluteUrl text } } pinnedOptions { count optionIdentifier title tooltip { absoluteUrl text } } tooltip { absoluteUrl text } } ... on RangeFilter { __typename identifier title min max step dataPoints { count value } topOutliersMerged unitId unitName tooltip { absoluteUrl text } } } } } fragment searchFiltersQuickFilters on ShopSearchResultQuickFiltersConnection { edges { node { disabled filterIdentifier optionIdentifier optionTitle filterTitle } } } fragment useSerpTrackingProducts on ShopSearchResultProductsConnection { totalCount } fragment useSerpTrackingShopSearchResult on ShopSearchResult { brands { edges { node { id } } } productTypes { edges { node { id } } } redirectionUrl galaxusReferral { __typename } alternativeQuerySuggestions { queryString } additionalQueryInfo { correctedQuery didYouMeanQuery isRerankedQuery lastProductSearchPass testGroup } } fragment vectorProductsSection on ShopSearchResultProductsConnection { edges { node { id } } pageInfo { hasNextPage endCursor } totalCount } fragment zeroResults on ShopSearchResult { ... alternativeQuerySuggestions ... galaxusReferral }",
  "operation": "searchQuery",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Subgraph_1",
        "document": "query searchQuery_1($filters: [FilterInput!], $sortOrder: ShopSearchSortOrder!, $query: String!, $searchQueryConfig: ShopSearchConfigInput) { shopSearch(query: $query, searchQueryConfig: $searchQueryConfig) { additionalQueryInfo { correctedQuery didYouMeanQuery lastProductSearchPass isRerankedQuery testGroup } brands { edges { node { id } } } productTypes { edges { node { id } } } redirectionUrl galaxusReferral { __typename portalUrl productCount products { id } } alternativeQuerySuggestions { queryString productCount productPreviewImageUrls __typename } blogPages { nodes { id } } communityItems(first: 3) { nodes { __typename ... on CommunityDiscussion { __typename databaseId __isNode: __typename id } ... on ProductQuestion { __typename databaseId __isNode: __typename id } ... on ProductReview { __typename databaseId __isNode: __typename id } } } products(first: 48, filters: $filters, sortOrder: $sortOrder) { edges { node { id __typename } cursor } productFilters { edges { node { __typename ... on RangeFilter { __typename identifier title min max step dataPoints { count value } topOutliersMerged unitId unitName tooltip { absoluteUrl text } } ... on CheckboxFilter { __typename identifier title commonOptions { count optionIdentifier title tooltip { absoluteUrl text } } pinnedOptions { count optionIdentifier title tooltip { absoluteUrl text } } tooltip { absoluteUrl text } } } } } totalCount pageInfo { hasNextPage endCursor } quickFilters { edges { node { disabled filterIdentifier optionIdentifier optionTitle filterTitle } } } } id } }",
        "selectionSetId": 0,
        "forwardedVariables": [
          {
            "variable": "filters"
          },
          {
            "variable": "sortOrder"
          },
          {
            "variable": "query"
          },
          {
            "variable": "searchQueryConfig"
          }
        ]
      },
      {
        "type": "Compose",
        "selectionSetIds": [
          0
        ]
      }
    ]
  }
}
```

