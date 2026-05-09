"use client";

import React, { FC } from "react";

import { AGENT_TRACE, TraceWaterfall } from "./TraceWaterfall";

// MCP / agent access mock for Section 08 (the page differentiator). Sits
// inside an inverted Band — the transcript and the returned trace are NOT
// framed as cards, they sit on the band surface separated by a single
// accent-cyan vertical rule. The right pane bleeds toward the band edge.

export const AgentTranscriptMock: FC = () => {
  return (
    <div className="cc-agent-mock">
      <div className="cc-agent-chat">
        <div className="cc-agent-chat-header">
          <span className="cc-agent-pill">MCP · Nitro</span>
          <span>session 3a4f</span>
        </div>
        <div className="cc-agent-msg is-user">
          <span className="cc-agent-msg-role">You</span>
          <div className="cc-agent-msg-body">
            show me the slowest resolver in the last hour
          </div>
        </div>
        <div className="cc-agent-msg is-agent">
          <span className="cc-agent-msg-role">Agent</span>
          <div className="cc-agent-msg-body">
            Querying <code>traces.aggregate</code> on the Nitro MCP surface,
            filtered by p95 over the last hour.
          </div>
          <div className="cc-agent-step">
            → MCP call: <code>traces.topResolvers</code>(window: 1h, by: p95)
          </div>
          <div className="cc-agent-step">
            ← 1 result · <code>Billing.charge</code> · p95 412ms · 2.4k samples
          </div>
        </div>
        <div className="cc-agent-msg is-agent">
          <span className="cc-agent-msg-role">Agent</span>
          <div className="cc-agent-msg-body">
            <code>Billing.charge</code> is slowest at p95 412ms, mostly on the
            external charge call. Want me to replay one of the failing traces
            against staging?
          </div>
        </div>
      </div>

      <div className="cc-agent-rule" aria-hidden />

      <div className="cc-agent-trace">
        <div className="cc-agent-trace-header">
          Returned trace · cart-checkout
        </div>
        <TraceWaterfall
          spans={AGENT_TRACE}
          totalLabel="0ms · 600ms"
          axisMs={[0, 150, 300, 450, 600]}
        />
      </div>
    </div>
  );
};
