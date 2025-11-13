import http from "k6/http";
import { check } from "k6";
import { Rate } from "k6/metrics";
import { textSummary } from "https://jslib.k6.io/k6-summary/0.0.1/index.js";

const GRAPHQL_URL = 'http://localhost:5220/graphql';
const endpoint = __ENV.GATEWAY_ENDPOINT || GRAPHQL_URL;
const mode = __ENV.MODE || "constant";
const isConstant = mode === "constant";
const vus = __ENV.BENCH_VUS ? parseInt(__ENV.BENCH_VUS) : isConstant ? 50 : 500;
const duration = __ENV.BENCH_OVER_TIME || "60s";

const successRate = new Rate("success_rate");

const summaryTrendStats = [
  "avg",
  "min",
  "med",
  "max",
  "p(90)",
  "p(95)",
  "p(99.9)",
];

export const options = isConstant
  ? {
      duration,
      vus,
      summaryTrendStats,
    }
  : {
      scenarios: {
        stress: {
          executor: "ramping-vus",
          startVUs: 0,
          stages: [
            { duration: "10s", target: 50 },
            { duration: "40s", target: vus },
            { duration: "10s", target: 50 },
          ],
          gracefulRampDown: "1s",
          gracefulStop: "0s",
        },
      },
      summaryTrendStats,
    };

export function setup() {
  for (let i = 0; i < vus * 2; i++) {
    sendGraphQLRequest();
  }
}

export default function () {
  makeGraphQLRequest();
}

export function handleSummary(data) {
  return handleBenchmarkSummary(data, { vus, duration });
}

let printIdentifiersMap = {};
let runIdentifiersMap = {};

function printOnce(identifier, ...args) {
  if (printIdentifiersMap[identifier]) {
    return;
  }

  console.log(...args);
  printIdentifiersMap[identifier] = true;
}

function runOnce(identifier, cb) {
  if (runIdentifiersMap[identifier]) {
    return true;
  }

  runIdentifiersMap[identifier] = true;
  return cb();
}

// Complex nested query testing federation across all source schemas
// Based on: https://github.com/graphql-hive/graphql-gateways-benchmark
const graphqlRequest = {
  payload: JSON.stringify({
    query: `
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
`,
  }),
  params: {
    headers: {
      "Content-Type": "application/json",
    },
  },
};

function handleBenchmarkSummary(data, additionalContext = {}) {
  const out = {
    stdout: textSummary(data, { indent: " ", enableColors: true }),
  };

  if (__ENV.SUMMARY_PATH) {
    out[`${__ENV.SUMMARY_PATH}/k6_summary.json`] = JSON.stringify(
      Object.assign(data, additionalContext)
    );
    out[`${__ENV.SUMMARY_PATH}/k6_summary.txt`] = textSummary(data, {
      indent: " ",
      enableColors: false,
    });
  }

  return out;
}

function sendGraphQLRequest() {
  const res = http.post(
    endpoint,
    graphqlRequest.payload,
    graphqlRequest.params
  );

  return res;
}

function makeGraphQLRequest() {
  const res = sendGraphQLRequest();
  const ok = check(res, {
    "response code was 200": (res) => res.status == 200,
    "no graphql errors": (resp) => {
      let has_errors = !!resp.body && resp.body.includes(`"errors"`);
      if (has_errors && __ENV.PRINT_ONCE) {
        printOnce(
          "graphql_errors",
          `‼️ Got GraphQL errors, here's a sample:`,
          res.json.errors
        );
      }

      return !has_errors;
    },
    "valid response structure": (resp) => {
      return runOnce("valid response structure", () => {
        const json = resp.json();

        let isValid = checkResponseStructure(json);

        if (!isValid && __ENV.PRINT_ONCE) {
          printOnce(
            "response_structure",
            `‼️ Got invalid structure, here's a sample:`,
            res.body
          );
        }

        return isValid;
      });
    },
  });

  successRate.add(ok);
}

function checkResponseStructure(body) {
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
}
