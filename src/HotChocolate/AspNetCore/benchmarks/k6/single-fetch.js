import http from 'k6/http';
import { check, sleep } from 'k6';
import exec from 'k6/execution';

export const options = {
  stages: [
    { duration: '10s', target: 50 },
    { duration: '20s', target: 500 },
    { duration: '10s', target: 500 },
  ],
  thresholds: {
    'http_req_duration{phase:measurement}': ['p(95)<500', 'p(99)<1000'],
    'http_req_failed{phase:measurement}': ['rate<0.01'],
    'http_req_duration': ['p(95)<1000', 'p(99)<2000'],
    'http_req_failed': ['rate<0.05'],
  },
  summaryTrendStats: ['min', 'avg', 'med', 'max', 'p(90)', 'p(95)', 'p(99)'],
};

const GRAPHQL_URL = 'http://localhost:5224/graphql';

const query = `
  {
    products(first: 50) {
      nodes {
        name
      }
    }
  }
`;

export default function () {
  const currentStageTime = exec.scenario.iterationInTest;
  const inMeasurementPhase = currentStageTime > 30;

  const payload = JSON.stringify({
    query: query,
  });

  const params = {
    headers: {
      'Content-Type': 'application/json',
    },
    tags: {
      phase: inMeasurementPhase ? 'measurement' : 'other',
    },
  };

  const res = http.post(GRAPHQL_URL, payload, params);

  check(res, {
    'status is 200': (r) => r.status === 200,
    'has products': (r) => {
      const body = JSON.parse(r.body);
      return body.data && body.data.products && body.data.products.nodes;
    },
    'returns 50 products': (r) => {
      const body = JSON.parse(r.body);
      return body.data?.products?.nodes?.length === 50;
    },
    'no errors': (r) => {
      const body = JSON.parse(r.body);
      return !body.errors;
    },
  });

  sleep(1);
}
