import http from 'k6/http';
import { check } from 'k6';

const GRAPHQL_URL = 'http://localhost:5220/graphql';

const query = `
  query GetTopProduct {
    topProducts(first: 1) {
      name
    }
  }
`;

export const options = {
  scenarios: {
    warmup: {
      executor: 'ramping-arrival-rate',
      startRate: 50,
      timeUnit: '1s',
      preAllocatedVUs: 1000,
      stages: [
        { target: 50, duration: '10s' },
        { target: 3000, duration: '20s' },
      ],
      tags: { phase: 'warmup' },
    },
    measurement: {
      executor: 'constant-arrival-rate',
      startTime: '30s',
      duration: '1m',
      rate: 3000,
      timeUnit: '1s',
      preAllocatedVUs: 1000,
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
    'has products': () => body?.data?.topProducts?.length > 0,
    'returns 1 product': () => body?.data?.topProducts?.length === 1,
    'has product name': () => body?.data?.topProducts?.[0]?.name,
  });
}
