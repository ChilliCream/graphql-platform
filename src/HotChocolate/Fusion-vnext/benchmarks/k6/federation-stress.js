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
      preAllocatedVUs: 50,
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
      preAllocatedVUs: 50,
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
    'valid response structure': () => {
      // Check that the response has the expected nested structure
      if (!body?.data) return false;

      const users = body.data.users;
      const topProducts = body.data.topProducts;

      // Check users structure
      if (!Array.isArray(users) || users.length === 0) return false;
      const user = users[0];
      if (!user.id || !user.username || !user.name) return false;
      if (!Array.isArray(user.reviews)) return false;

      // Check topProducts structure
      if (!Array.isArray(topProducts) || topProducts.length === 0) return false;
      const product = topProducts[0];
      if (typeof product.inStock !== 'boolean') return false;
      if (!product.name || !product.upc) return false;
      if (typeof product.price !== 'number' || typeof product.weight !== 'number') return false;
      if (!Array.isArray(product.reviews)) return false;

      // Check nested review structure
      if (product.reviews.length > 0) {
        const review = product.reviews[0];
        if (!review.id || !review.body) return false;
        if (review.author && (!review.author.id || !review.author.username)) return false;
      }

      return true;
    },
  });
}
