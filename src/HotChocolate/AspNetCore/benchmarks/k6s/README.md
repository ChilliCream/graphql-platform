# k6 Load Tests for Catalog GraphQL API

This directory contains k6 load tests for the Catalog GraphQL API.

## Quick Start

The easiest way to run the tests is using the automated script:

```bash
./run-tests.sh
```

This script will:

- Check if k6 is installed and offer to install it if missing
- Verify the GraphQL server is running
- Run all load tests sequentially
- Provide a summary of results

## Prerequisites

1. Install k6: https://k6.io/docs/getting-started/installation/ (or use `./run-tests.sh` to auto-install)
2. Start the Catalog.AppHost Aspire application

## Available Tests

### 1. Single Fetch Test (`single-fetch.js`)

Tests a simple query that fetches 50 products with only their names.

**Query:**
```graphql
{
  products(first: 50) {
    nodes {
      name
    }
  }
}
```

**Run:**
```bash
k6 run single-fetch.js
```

### 2. DataLoader Test (`dataloader.js`)

Tests a query that fetches 50 products with their names and brand information, demonstrating DataLoader batching behavior.

**Query:**
```graphql
{
  products(first: 50) {
    nodes {
      name
      brand {
        name
      }
    }
  }
}
```

**Run:**
```bash
k6 run dataloader.js
```

## Test Configuration

Both tests use the following configuration:

- **Ramp-up:** 30 seconds to reach 20 concurrent users
- **Steady state:** 1 minute at 20 concurrent users
- **Ramp-down:** 10 seconds to 0 users

### Performance Thresholds

- 95th percentile response time < 500ms
- Error rate < 1%

## Customizing Tests

You can modify the test parameters by editing the `options` object in each test file:

```javascript
export const options = {
  stages: [
    { duration: '30s', target: 20 },  // Modify duration and target users
    { duration: '1m', target: 20 },
    { duration: '10s', target: 0 },
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'], // Adjust performance thresholds
    http_req_failed: ['rate<0.01'],
  },
};
```

## Understanding Results

k6 will output various metrics including:

- **http_req_duration:** Response time percentiles (p50, p95, p99)
- **http_reqs:** Total number of requests and requests per second
- **http_req_failed:** Percentage of failed requests
- **checks:** Percentage of passed validation checks

## Comparing Results

Run both tests sequentially to compare the performance characteristics:

```bash
k6 run single-fetch.js
k6 run dataloader.js
```

The DataLoader test should demonstrate the efficiency of batched database queries when fetching related entities.
