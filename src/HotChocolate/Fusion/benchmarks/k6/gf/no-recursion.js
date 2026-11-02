import http from "k6/http";
import { check } from "k6";
import { Rate } from "k6/metrics";
import { textSummary } from "https://jslib.k6.io/k6-summary/0.0.1/index.js";

const endpoint = __ENV.GATEWAY_ENDPOINT || "http://0.0.0.0:5220/graphql";
const mode = __ENV.MODE || "constant";
const isConstant = mode === "constant";
const parsedVus = parseInt(__ENV.BENCH_VUS || "", 10);
const defaultVus = isConstant ? 50 : 500;
const vus = Number.isFinite(parsedVus) && parsedVus > 0 ? parsedVus : defaultVus;
const duration = __ENV.BENCH_OVER_TIME || (isConstant ? "120s" : "60s");
const burstStages = [
  { duration: "10s", target: 50 },
  { duration: "40s", target: vus },
  { duration: "10s", target: 50 },
];
// Planned load duration (burst: its stages); excludes setup() and graceful stop.
const plannedDurationSeconds = isConstant
  ? parseInt(duration, 10)
  : burstStages.reduce((sum, stage) => sum + parseInt(stage.duration, 10), 0);

const successRate = new Rate("success_rate");

// Error-response sampling: capture a small, per-VU-capped sample of error
// response bodies into the run artifact for later analysis. Gated on
// SUMMARY_PATH so only measured runs capture (the warmup run omits it).
const CAPTURE_MAX_PER_VU = parseInt(__ENV.CAPTURE_MAX_PER_VU || "5", 10);
const CAPTURE_BODY_BYTES = parseInt(__ENV.CAPTURE_BODY_BYTES || "1024", 10);
let capturedThisVU = 0;

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
          startVUs: 50,
          stages: burstStages,
          gracefulRampDown: "5s",
          gracefulStop: "5s",
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
  return handleBenchmarkSummary(data, {
    vus,
    duration: `${plannedDurationSeconds}s`,
    plannedDurationSeconds,
  });
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
    topProducts(first: 5) {
      ...Product
      reviews {
        ...Review
        author {
          ...User
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

let _reqCounter = 0;

function sendGraphQLRequest() {
  const reqId = typeof __ITER !== "undefined" ? `${__VU}-${__ITER}` : `s-${_reqCounter++}`;
  const res = http.post(
    endpoint,
    graphqlRequest.payload,
    {
      headers: {
        "Content-Type": "application/json",
        "Authorization": `Bearer bench-${reqId}`,
      },
    }
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
          res.json().errors
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
            "response_strcuture",
            `‼️ Got invalid structure, here's a sample:`,
            res.body
          );
        }

        return isValid;
      });
    },
  });

  successRate.add(ok);

  // Sample error response bodies (capped per VU) for post-run analysis.
  // Fires only on a failed request and only during measured runs, so clean
  // runs and the warmup never log and throughput stays unperturbed.
  if (!ok && __ENV.SUMMARY_PATH && capturedThisVU < CAPTURE_MAX_PER_VU) {
    capturedThisVU++;
    console.log(
      "ERRSAMPLE|" +
        JSON.stringify({
          vu: __VU,
          iter: __ITER,
          status: res.status,
          error_code: res.error_code,
          error: res.error,
          body: String(res.body || "").slice(0, CAPTURE_BODY_BYTES),
        })
    );
  }
}

function checkResponseStructure(x) {
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
      topProducts: [
        {
          inStock: true,
          name: "Table",
          price: 899,
          shippingEstimate: 50,
          upc: "1",
          weight: 100,
          reviews: [
            {
              id: "1",
              body: "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.",
              author: {
                id: "1",
                username: "urigo",
                name: "Uri Goldshtein",
              },
            },
            {
              id: "2",
              body: "Sed ut perspiciatis unde omnis iste natus error sit voluptatem accusantium doloremque laudantium, totam rem aperiam, eaque ipsa quae ab illo inventore veritatis et quasi architecto beatae vitae dicta sunt explicabo. Nemo enim ipsam voluptatem quia voluptas sit aspernatur aut odit aut fugi",
              author: {
                id: "1",
                username: "urigo",
                name: "Uri Goldshtein",
              },
            },
            {
              id: "3",
              body: "sed quia consequuntur magni dolores eos qui ratione voluptatem sequi nesciunt. Neque porro quisquam est, qui dolorem ipsum quia dolor sit amet, consectetur, adipisci velit, sed quia non numquam eius modi tempora incidunt ut labore et dolore magnam aliquam quaerat voluptatem.",
              author: {
                id: "1",
                username: "urigo",
                name: "Uri Goldshtein",
              },
            },
            {
              id: "4",
              body: "Ut enim ad minima veniam, quis nostrum exercitationem ullam corporis suscipit laboriosam, nisi ut aliquid ex ea commodi consequatur? Quis autem",
              author: {
                id: "1",
                username: "urigo",
                name: "Uri Goldshtein",
              },
            },
          ],
        },
        {
          inStock: false,
          name: "Couch",
          price: 1299,
          shippingEstimate: 0,
          upc: "2",
          weight: 1000,
          reviews: [
            {
              id: "5",
              body: "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.",
              author: {
                id: "1",
                username: "urigo",
                name: "Uri Goldshtein",
              },
            },
            {
              id: "6",
              body: "Sed ut perspiciatis unde omnis iste natus error sit voluptatem accusantium doloremque laudantium, totam rem aperiam, eaque ipsa quae ab illo inventore veritatis et quasi architecto beatae vitae dicta sunt explicabo. Nemo enim ipsam voluptatem quia voluptas sit aspernatur aut odit aut fugi",
              author: {
                id: "1",
                username: "urigo",
                name: "Uri Goldshtein",
              },
            },
            {
              id: "7",
              body: "sed quia consequuntur magni dolores eos qui ratione voluptatem sequi nesciunt. Neque porro quisquam est, qui dolorem ipsum quia dolor sit amet, consectetur, adipisci velit, sed quia non numquam eius modi tempora incidunt ut labore et dolore magnam aliquam quaerat voluptatem.",
              author: {
                id: "1",
                username: "urigo",
                name: "Uri Goldshtein",
              },
            },
            {
              id: "8",
              body: "Ut enim ad minima veniam, quis nostrum exercitationem ullam corporis suscipit laboriosam, nisi ut aliquid ex ea commodi consequatur? Quis autem",
              author: {
                id: "1",
                username: "urigo",
                name: "Uri Goldshtein",
              },
            },
          ],
        },
        {
          inStock: false,
          name: "Glass",
          price: 15,
          shippingEstimate: 10,
          upc: "3",
          weight: 20,
          reviews: [
            {
              id: "9",
              body: "Ut enim ad minima veniam, quis nostrum exercitationem ullam corporis suscipit laboriosam, nisi ut aliquid ex ea commodi consequatur? Quis autem",
              author: {
                id: "1",
                username: "urigo",
                name: "Uri Goldshtein",
              },
            },
          ],
        },
        {
          inStock: false,
          name: "Chair",
          price: 499,
          shippingEstimate: 50,
          upc: "4",
          weight: 100,
          reviews: [
            {
              id: "10",
              body: "Ut enim ad minima veniam, quis nostrum exercitationem ullam corporis suscipit laboriosam, nisi ut aliquid ex ea commodi consequatur? Quis autem",
              author: {
                id: "1",
                username: "urigo",
                name: "Uri Goldshtein",
              },
            },
            {
              id: "11",
              body: "At vero eos et accusamus et iusto odio dignissimos ducimus qui blanditiis praesentium voluptatum deleniti atque corrupti quos dolores et quas molestias excepturi sint occaecati cupiditate non provident, similique sunt in culpa qui officia deserunt mollitia animi, id est laborum et dolorum fuga. Et harum quidem rerum facilis est et expedita distinctio. Nam libero tempore, cum soluta nobis est eligendi optio cumque nihil impedit quo minus id quod maxime placeat facere possimus, omnis voluptas assumenda est, omnis dolor repellendus. Temporibus autem quibusdam et aut officiis debitis aut rerum necessitatibus saepe eveniet ut et voluptates repudiandae sint et molestiae non recusandae. Itaque earum rerum hic tenetur a sapiente delectus, ut aut reiciendis voluptatibus maiores alias consequatur aut perferendis doloribus asperiores repellat.",
              author: {
                id: "1",
                username: "urigo",
                name: "Uri Goldshtein",
              },
            },
          ],
        },
        {
          inStock: true,
          name: "TV",
          price: 1299,
          shippingEstimate: 0,
          upc: "5",
          weight: 1000,
          reviews: [],
        },
      ],
    },
  };

  return checkRecursive(x, expectedStructure);
}
