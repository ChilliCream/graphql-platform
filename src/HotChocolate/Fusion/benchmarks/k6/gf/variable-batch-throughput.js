import http from "k6/http";
import { check } from "k6";
import { Rate } from "k6/metrics";
import { textSummary } from "https://jslib.k6.io/k6-summary/0.0.1/index.js";

const GRAPHQL_URL = 'http://localhost:5222/graphql';
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

const query = `
query TestQuery_8f7a46ce_2(
  $__fusion_1_upc: ID!
  $__fusion_2_price: Long!
  $__fusion_2_weight: Long!
) {
  productByUpc(upc: $__fusion_1_upc) {
    inStock
    shippingEstimate(weight: $__fusion_2_weight, price: $__fusion_2_price)
  }
}
`;

const variableSets = [
  {
    "__fusion_1_upc": "1",
    "__fusion_2_price": 899,
    "__fusion_2_weight": 100
  },
  {
    "__fusion_1_upc": "2",
    "__fusion_2_price": 1299,
    "__fusion_2_weight": 1000
  },
  {
    "__fusion_1_upc": "3",
    "__fusion_2_price": 15,
    "__fusion_2_weight": 20
  },
  {
    "__fusion_1_upc": "4",
    "__fusion_2_price": 499,
    "__fusion_2_weight": 100
  },
  {
    "__fusion_1_upc": "5",
    "__fusion_2_price": 1299,
    "__fusion_2_weight": 1000
  }
];

const graphqlRequest = {
  payload: JSON.stringify({
    query: query,
    variables: variableSets
  }),
  params: {
    headers: {
      "Content-Type": "application/json",
      "Accept": "application/jsonl",
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
          resp.body
        );
      }

      return !has_errors;
    },
    "valid response structure": (resp) => {
      return runOnce("valid response structure", () => {
        let isValid = checkResponseStructure(resp.body);

        if (!isValid && __ENV.PRINT_ONCE) {
          printOnce(
            "response_strcuture",
            `‼️ Got invalid structure, here's a sample:`,
            resp.body
          );
        }

        return isValid;
      });
    },
  });

  successRate.add(ok);
}

function checkResponseStructure(body) {
  function checkRecursive(obj, structure) {
    if (obj == null) {
      return false;
    }
    for (var key in structure) {
      if (
        !obj.hasOwnProperty(key) ||
        typeof obj[key] !== typeof structure[key]
      ) {
        return false;
      }
      if (typeof structure[key] === "object" && structure[key] !== null) {
        if (!checkRecursive(obj[key], structure[key])) {
          return false;
        }
      }
    }
    return true;
  }

  const expectedStructure = {
    data: {
      productByUpc: {
        inStock: true,
        shippingEstimate: 50,
      },
    },
  };

  // Parse JSONL response (newline-delimited JSON)
  try {
    const lines = body.trim().split('\n');

    // Should have 5 responses for the 5 variable sets
    if (lines.length !== 5) {
      return false;
    }

    // Validate each response
    for (let i = 0; i < lines.length; i++) {
      const json = JSON.parse(lines[i]);

      // Check that response has no errors property
      if (json.hasOwnProperty('errors')) {
        return false;
      }

      // Check that response has valid structure
      if (!checkRecursive(json, expectedStructure)) {
        return false;
      }
    }

    return true;
  } catch (e) {
    return false;
  }
}
