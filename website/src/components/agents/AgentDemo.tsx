"use client";

import React, { FC } from "react";

import { Demo, DemoOutputKind, DemoStep } from "@/data/agents/demos";
import {
  AGENT_TRACE,
  TraceWaterfall,
} from "@/components/observability/TraceWaterfall";

// Section 05: a single demo block. Two-column layout with the chat-style
// transcript on the left and a side-output on the right (TraceWaterfall +
// SQL snippet for Demo A, ledger for Demo B). Both demos share this one
// component; the variant is keyed off `demo.output`.

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

const TraceOutput: FC = () => {
  return (
    <>
      <div className="cc-ag-demo-out-head">
        <span>Returned trace · createOrder</span>
        <span>0ms · 600ms</span>
      </div>
      <TraceWaterfall
        spans={AGENT_TRACE}
        totalLabel="0ms · 600ms"
        axisMs={[0, 150, 300, 450, 600]}
      />
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
    </>
  );
};

const LEDGER_ROWS: readonly { name: string; kind: string }[] = [
  { name: "schema.graphql · Mutation.cancelOrder", kind: "GraphQL" },
  { name: "openapi.yaml · POST /orders/:id/cancel", kind: "OpenAPI" },
  { name: "mcp.tools · command.cancelOrder", kind: "MCP tool" },
  { name: "client.csproj · CancelOrderMutation", kind: "Strawberry Shake" },
];

const LedgerOutput: FC = () => {
  // The transcript implies a sequential flip from `—` to ✓ as the agent
  // registers each surface. We render all four checked here because a real
  // demo recording would land on this end-state; future iterations can
  // animate the flip with the same pattern as AgentTerminal's tick loop.
  return (
    <>
      <div className="cc-ag-demo-out-head">
        <span>Registration ledger · CancelOrder</span>
        <span>4 / 4 surfaces</span>
      </div>
      <div className="cc-ag-ledger">
        {LEDGER_ROWS.map((row) => (
          <div key={row.name} className="cc-ag-ledger-row is-on">
            <span className="check" aria-hidden>
              <svg viewBox="0 0 12 12" width="11" height="11">
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
            <span className="name">{row.name}</span>
            <span className="kind">{row.kind}</span>
          </div>
        ))}
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
    </>
  );
};

const OUTPUTS: Record<DemoOutputKind, FC> = {
  trace: TraceOutput,
  ledger: LedgerOutput,
};

export const AgentDemo: FC<AgentDemoProps> = ({ demo }) => {
  const Output = OUTPUTS[demo.output];
  return (
    <article className="cc-ag-demo">
      <header className="cc-ag-demo-head">
        <span className="badge">{demo.badge}</span>
        <h3>{demo.title}</h3>
        <p>{demo.sub}</p>
      </header>

      <div className="cc-ag-demo-grid">
        <div className="cc-ag-demo-chat">
          <div className="cc-ag-demo-chat-head">
            <span className="cc-ag-demo-chat-pill">MCP · Nitro</span>
            <span>{demo.session}</span>
          </div>
          {demo.steps.map((step, i) => (
            <ChatMessage key={i} step={step} />
          ))}
        </div>

        <div className="cc-ag-demo-out">
          <Output />
        </div>
      </div>
    </article>
  );
};
