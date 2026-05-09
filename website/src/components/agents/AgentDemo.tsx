"use client";

import React, { FC } from "react";

import { Demo, DemoStep } from "@/data/agents/demos";
import {
  AGENT_TRACE,
  TraceWaterfall,
} from "@/components/observability/TraceWaterfall";

// Section 05: two demos that PROVE the loop. The two demos are now
// shape-differentiated so the reader sees "diagnose vs. compose" without a
// label:
//
//   Demo A (Investigate, "Why is /orders slow?")
//   Vertical INVESTIGATIVE WATERFALL: narrow chat at top, large trace
//   waterfall spanning the FULL WIDTH below. Top-down: question, descending
//   into causes. Maps to Loop stages: Observe + Reason.
//
//   Demo B (Operate, "Add a cancel-order command")
//   Horizontal FAN-OUT LEDGER: chat in the center, the four surfaces fan
//   out HORIZONTALLY around it, each lighting up green as the agent
//   registers. Lateral: one command, four parallel writes. Maps to Loop
//   stages: Act + Compose + Ship.
//
// These shapes are wired into the Loop diagram in Section 03 as a legend —
// every demo header carries a "stages" badge naming the Loop stages it
// proves, so the diagram is a table of contents the demos cite.

interface AgentDemoProps {
  readonly demo: Demo;
}

const ChatMessage: FC<{ step: DemoStep }> = ({ step }) => {
  return (
    <div
      className={
        "cc-ag-demo-msg " + (step.role === "agent" ? "is-agent" : "is-user")
      }
    >
      <span className="role">
        {step.role === "agent" && <span className="agent-pill" aria-hidden />}
        {step.role === "agent" ? "Agent" : "You"}
      </span>
      <div className="body">{step.body}</div>
      {step.mcp && (
        <div className="cc-ag-demo-mcp" aria-label="MCP tool call">
          <span className="pill">MCP</span>
          <code>{step.mcp}</code>
        </div>
      )}
    </div>
  );
};

const ChatPanel: FC<{ demo: Demo }> = ({ demo }) => (
  <div className="cc-ag-demo-chat">
    <div className="cc-ag-demo-chat-head">
      <span className="cc-ag-demo-chat-pill">MCP · Nitro</span>
      <span>{demo.session}</span>
    </div>
    {demo.steps.map((step, i) => (
      <ChatMessage key={i} step={step} />
    ))}
  </div>
);

// Demo A: vertical investigative waterfall layout. Chat on top (narrow
// column), full-width trace waterfall below, SQL/index proposal under
// that. Reads top-down like a real diagnosis session.
const InvestigateDemo: FC<{ demo: Demo }> = ({ demo }) => {
  return (
    <article className="cc-ag-demo cc-ag-demo-investigate">
      <header className="cc-ag-demo-head">
        <span className="badge">{demo.badge}</span>
        <h3>{demo.title}</h3>
        <span
          className="cc-ag-demo-stages"
          aria-label="Loop stages demonstrated"
        >
          <span className="stage-label">LOOP</span> Observe / Reason
        </span>
        <p>{demo.sub}</p>
      </header>

      <div className="cc-ag-demo-investigate-grid">
        <ChatPanel demo={demo} />
        <aside className="cc-ag-demo-investigate-side">
          <span className="cc-ag-demo-side-tag">Descending into causes</span>
          <ol className="cc-ag-demo-cause-chain">
            <li>
              <span className="cause-step">01</span>
              <span className="cause-name">/orders mutation</span>
              <span className="cause-meta">created 600ms total</span>
            </li>
            <li>
              <span className="cause-step">02</span>
              <span className="cause-name">Billing.charge resolver</span>
              <span className="cause-meta">412ms p95</span>
            </li>
            <li>
              <span className="cause-step">03</span>
              <span className="cause-name">SELECT FROM payments</span>
              <span className="cause-meta">sequential scan</span>
            </li>
          </ol>
        </aside>
      </div>

      <div className="cc-ag-demo-trace-full">
        <div className="cc-ag-demo-out-head">
          <span>Returned trace · createOrder</span>
          <span>0ms · 600ms</span>
        </div>
        <TraceWaterfall
          spans={AGENT_TRACE}
          totalLabel="0ms · 600ms"
          axisMs={[0, 150, 300, 450, 600]}
        />
      </div>

      <div className="cc-ag-demo-out-snippet" aria-label="Suggested SQL change">
        <div>
          <span className="com">-- Billing.Resolvers/Charge.cs · proposed</span>
        </div>
        <div>
          <span className="kw">CREATE INDEX</span>{" "}
          <span className="ty">idx_payments_customer_created</span>
        </div>
        <div>
          {"  "}
          <span className="kw">ON</span> <span className="ty">payments</span>{" "}
          (customer_id, created_at);
        </div>
        <div>
          <span className="add">+ filed as PR #4187</span>
        </div>
      </div>
    </article>
  );
};

// Demo B: horizontal fan-out. Chat sits in the center column, four surface
// tiles fan out around it (two on each side on desktop, stacked below on
// mobile), each lighting up amber as the agent registers it. Reads
// laterally like a parallel operate-the-system session.
const FAN_OUT_SURFACES: readonly {
  name: string;
  kind: string;
  lit: boolean;
}[] = [
  { name: "schema.graphql", kind: "GraphQL", lit: true },
  { name: "openapi.yaml", kind: "OpenAPI", lit: true },
  { name: "mcp.tools", kind: "MCP tool", lit: true },
  { name: "client.csproj", kind: "Strawberry Shake", lit: true },
];

const FAN_OUT_LABELS: readonly string[] = [
  "Mutation.cancelOrder",
  "POST /orders/:id/cancel",
  "command.cancelOrder",
  "CancelOrderMutation",
];

const Spoke: FC<{ side: "left" | "right" }> = ({ side }) => (
  <svg
    viewBox="0 0 60 12"
    aria-hidden
    className={`cc-ag-fanout-spoke is-${side}`}
  >
    <line
      x1={side === "left" ? 60 : 0}
      y1="6"
      x2={side === "left" ? 0 : 60}
      y2="6"
      stroke="var(--cc-amber)"
      strokeOpacity="0.55"
      strokeWidth="1.4"
      strokeDasharray="3 4"
    />
    <circle
      cx={side === "left" ? 0 : 60}
      cy="6"
      r="2.4"
      fill="var(--cc-amber)"
    />
  </svg>
);

const OperateDemo: FC<{ demo: Demo }> = ({ demo }) => {
  return (
    <article className="cc-ag-demo cc-ag-demo-operate">
      <header className="cc-ag-demo-head">
        <span className="badge">{demo.badge}</span>
        <h3>{demo.title}</h3>
        <span
          className="cc-ag-demo-stages"
          aria-label="Loop stages demonstrated"
        >
          <span className="stage-label">LOOP</span> Act / Compose / Ship
        </span>
        <p>{demo.sub}</p>
      </header>

      <div className="cc-ag-demo-fanout">
        {/* left column: two surfaces fanning out to the left */}
        <div className="cc-ag-fanout-col is-left">
          {FAN_OUT_SURFACES.slice(0, 2).map((surf, i) => (
            <div key={surf.name} className="cc-ag-fanout-tile is-on">
              <span className="check" aria-hidden>
                <svg viewBox="0 0 12 12" width="12" height="12">
                  <path
                    d="M2 6.5 L5 9 L10 3"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="1.6"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                  />
                </svg>
              </span>
              <span className="kind">{surf.kind}</span>
              <span className="name">{surf.name}</span>
              <span className="payload">{FAN_OUT_LABELS[i]}</span>
              <Spoke side="right" />
            </div>
          ))}
        </div>

        {/* center column: chat */}
        <div className="cc-ag-fanout-center">
          <ChatPanel demo={demo} />
        </div>

        {/* right column: two surfaces fanning out to the right */}
        <div className="cc-ag-fanout-col is-right">
          {FAN_OUT_SURFACES.slice(2).map((surf, i) => (
            <div key={surf.name} className="cc-ag-fanout-tile is-on">
              <Spoke side="left" />
              <span className="check" aria-hidden>
                <svg viewBox="0 0 12 12" width="12" height="12">
                  <path
                    d="M2 6.5 L5 9 L10 3"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="1.6"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                  />
                </svg>
              </span>
              <span className="kind">{surf.kind}</span>
              <span className="name">{surf.name}</span>
              <span className="payload">{FAN_OUT_LABELS[i + 2]}</span>
            </div>
          ))}
        </div>
      </div>

      <div className="cc-ag-demo-out-snippet" aria-label="Generated handler">
        <div>
          <span className="com">
            // OrderModule/Commands/CancelOrderHandler.cs
          </span>
        </div>
        <div>
          <span className="kw">public sealed class</span>{" "}
          <span className="ty">CancelOrderHandler</span>
        </div>
        <div>
          {"  "}: <span className="ty">ICommandHandler</span>
          {"<"}
          <span className="ty">CancelOrder</span>
          {">"}
        </div>
      </div>
    </article>
  );
};

export const AgentDemo: FC<AgentDemoProps> = ({ demo }) => {
  // Pick the demo shape from the demo's output kind (trace = investigate,
  // ledger = operate). Keeps the data layer untouched while the visual
  // layer differentiates the two beats.
  if (demo.output === "trace") {
    return <InvestigateDemo demo={demo} />;
  }
  return <OperateDemo demo={demo} />;
};
