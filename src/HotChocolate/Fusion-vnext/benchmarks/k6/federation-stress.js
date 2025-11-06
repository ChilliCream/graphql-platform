import http from 'k6/http';
import { check } from 'k6';

const GRAPHQL_URL = 'http://localhost:5220/graphql';

// Complex nested query testing federation across all source schemas
// Based on: https://github.com/graphql-hive/graphql-gateways-benchmark
const query = `
  fragment User on User {
    id
    username
    name
  }

  fragment Review on Review {
    id
    body
  }

  fragment Product on Product {
    inStock
    name
    price
    shippingEstimate
    upc
    weight
  }

  query TestQuery {
    users {
      ...User
      reviews {
        ...Review
        product {
          ...Product
          reviews {
            ...Review
            author {
              ...User
              reviews {
                ...Review
                product {
                  ...Product
                }
              }
            }
          }
        }
      }
    }
    topProducts(first: 5) {
      ...Product
      reviews {
        ...Review
        author {
          ...User
          reviews {
            ...Review
            product {
              ...Product
            }
          }
        }
      }
    }
  }
`;

export const options = {
  scenarios: {
    warmup: {
      executor: 'ramping-arrival-rate',
      startRate: 50,
      timeUnit: '1s',
      preAllocatedVUs: 600,
      stages: [
        { target: 50, duration: '10s' },
        { target: 500, duration: '20s' },
      ],
      tags: { phase: 'warmup' },
    },
    measurement: {
      executor: 'constant-arrival-rate',
      startTime: '30s',
      duration: '1m',
      rate: 500,
      timeUnit: '1s',
      preAllocatedVUs: 600,
      tags: { phase: 'measurement' },
    },
  },
  thresholds: {
    // Apply to measurement scenario only
    'http_req_duration{scenario:measurement}': ['p(95)<500', 'p(99)<1000'],
    'http_req_failed{scenario:measurement}': ['rate<0.01'],

    // Global safety rails (both scenarios)
    'http_req_duration': ['p(95)<1000', 'p(99)<2000'],
    'http_req_failed': ['rate<0.05'],
  },
  summaryTrendStats: ['min', 'avg', 'med', 'max', 'p(90)', 'p(95)', 'p(99)'],
};

export default function () {
  const res = http.post(GRAPHQL_URL, JSON.stringify({ query }), {
    headers: { 'Content-Type': 'application/json' },
  });

  // Parse once, reuse
  let body;
  try {
    body = JSON.parse(res.body);
  } catch (_) {
    body = null;
  }

  check(res, {
    'status is 200': (r) => r.status === 200,
    'no errors': () => body && !body.errors,
    'has users': () => body?.data?.users?.length > 0,
    'has topProducts': () => body?.data?.topProducts?.length > 0,
    'users have reviews': () => {
      const users = body?.data?.users;
      return users && users.length > 0 && users.some(u => u.reviews && u.reviews.length > 0);
    },
    'products have inventory data': () => {
      const products = body?.data?.topProducts;
      return products && products.length > 0 && products.every(p =>
        typeof p.inStock === 'boolean' && typeof p.shippingEstimate === 'number'
      );
    },
    'reviews have authors': () => {
      const users = body?.data?.users;
      if (!users || users.length === 0) return false;
      const reviews = users.flatMap(u => u.reviews || []);
      return reviews.length > 0 && reviews.every(r => r.author && r.author.username);
    },
  });
}
