"use client";

import React, { FC } from "react";

import { AGENT_TRACE, TraceWaterfall } from "./TraceWaterfall";

// MCP / agent access mock for Section 08 (the page differentiator). Left pane
// is a chat-style transcript with a small AGENT pill in the header; right
// pane is the resulting trace with the slowest span highlighted. The arrow
// between the two panels is rendered in CSS (not a real connector) because
// the panels stack on small viewports and a hard arrow would look bad.

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

      <div className="cc-agent-arrow" aria-hidden>
        →
      </div>

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
