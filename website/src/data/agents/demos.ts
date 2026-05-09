// Two proof-of-thesis demos for Section 05 (the load-bearing section of the
// page). Each demo has a fixed-order transcript on the left + a side-output
// renderer on the right. The transcript step shape is shared so AgentDemo can
// render both demos with one component. Side-output kinds:
//   - "trace": render the agent-trace TraceWaterfall + a SQL/index snippet
//   - "ledger": render the 4-surface registration ledger (graph/openapi/mcp/
//      strawberry-shake) flipping from `—` to ✓ as the agent registers them.

export type DemoStepRole = "user" | "agent";

export interface DemoStep {
  readonly role: DemoStepRole;
  // Plain message body. May reference fragments via the `code` slot.
  readonly body: string;
  // Optional MCP tool call shown in a dark inset block under the body.
  readonly mcp?: string;
}

export type DemoOutputKind = "trace" | "ledger";

export interface Demo {
  readonly key: string;
  readonly badge: string;
  readonly title: string;
  readonly sub: string;
  readonly session: string;
  readonly steps: readonly DemoStep[];
  readonly output: DemoOutputKind;
}

export const DEMOS: readonly Demo[] = [
  {
    key: "why-orders-slow",
    badge: "Demo A · Investigate",
    title: "Why is /orders slow?",
    sub: "From a one-line user prompt to a PR with the right index, in five MCP calls.",
    session: "nitro mcp · session 7c3a · cart-ops",
    steps: [
      {
        role: "user",
        body: "Why is /orders slow?",
      },
      {
        role: "agent",
        body: "Pulling latest p95 traces for the createOrder mutation, last hour.",
        mcp: 'traces.search({ op: "createOrder", since: "1h" })',
      },
      {
        role: "agent",
        body: "Found 412ms p95 in Billing.charge. Opening the resolver.",
        mcp: 'resolver.read({ path: "Billing.charge" })',
      },
      {
        role: "agent",
        body: "Resolver issues SELECT * FROM payments WHERE customer_id = ? without an index. The query plan shows a sequential scan.",
      },
      {
        role: "agent",
        body: "Suggesting a composite index on (customer_id, created_at). Opening PR #4187.",
        mcp: 'pr.open({ branch: "perf/billing-index", title: "Add composite index on payments" })',
      },
    ],
    output: "trace",
  },
  {
    key: "add-cancel-order",
    badge: "Demo B · Operate",
    title: "Add a cancel-order command.",
    sub: "Conventions read, command scaffolded, four surfaces registered. No human glue.",
    session: "nitro mcp · session 9f12 · order-svc",
    steps: [
      {
        role: "user",
        body: "Add a cancel-order command.",
      },
      {
        role: "agent",
        body: "Reading your CQRS conventions in OrderModule.Commands.",
        mcp: 'conventions.list({ module: "OrderModule" })',
      },
      {
        role: "agent",
        body: "Generating CancelOrderCommand + handler with audit log via Mocha.",
        mcp: 'command.scaffold({ name: "CancelOrder", emits: "OrderCancelled" })',
      },
      {
        role: "agent",
        body: "Registering on the graph, OpenAPI, and the MCP tool surface.",
        mcp: 'register({ surfaces: ["graph", "openapi", "mcp"] })',
      },
      {
        role: "agent",
        body: "Strawberry Shake will regenerate the client on next build. The loop is closed.",
      },
    ],
    output: "ledger",
  },
];
