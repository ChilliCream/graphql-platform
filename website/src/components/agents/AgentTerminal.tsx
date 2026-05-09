"use client";

import React, { FC, ReactNode, useEffect, useState } from "react";

// Hero terminal mock for Section 01. Auto-cycles through a fixed list of
// realistic-feeling MCP tool calls so the page feels alive on first paint.
// Each line carries a role pill (USER / AGENT / MCP) and either freeform
// monospace text or a structured tool-call body. The cursor blinks on the
// last visible line.
//
// Animation strategy: every TICK_MS we either reveal the next line in the
// script (until the visible window is full) or we drop the oldest line and
// append a new one from the top of the script. This keeps the visible block
// roughly constant-height so the page doesn't reflow.

type Role = "user" | "agent" | "mcp";

interface ScriptLine {
  readonly role: Role;
  readonly body: ReactNode;
}

const SCRIPT: readonly ScriptLine[] = [
  {
    role: "user",
    body: "find the slowest resolver this hour",
  },
  {
    role: "agent",
    body: "Searching traces across the federation.",
  },
  {
    role: "mcp",
    body: (
      <>
        <span className="key">graph.search</span>
        {"({ "}
        <span className="key">since</span>
        {": "}
        <span className="arg">"1h"</span>
        {", "}
        <span className="key">orderBy</span>
        {": "}
        <span className="arg">"p95"</span>
        {" })"}
      </>
    ),
  },
  {
    role: "mcp",
    body: (
      <>
        <span className="ok">→</span> 1 result ·{" "}
        <span className="arg">Billing.charge</span> · p95{" "}
        <span className="arg">412ms</span>
      </>
    ),
  },
  {
    role: "agent",
    body: "Opening the resolver in the Billing service.",
  },
  {
    role: "mcp",
    body: (
      <>
        <span className="key">resolver.read</span>
        {"({ "}
        <span className="key">path</span>
        {": "}
        <span className="arg">"Billing.charge"</span>
        {" })"}
      </>
    ),
  },
  {
    role: "mcp",
    body: (
      <>
        <span className="ok">→</span> Billing.Resolvers/Charge.cs · 84 lines · 1
        hot path
      </>
    ),
  },
  {
    role: "user",
    body: "add a cancel-order command",
  },
  {
    role: "agent",
    body: "Reading CQRS conventions in OrderModule.Commands.",
  },
  {
    role: "mcp",
    body: (
      <>
        <span className="key">conventions.list</span>
        {"({ "}
        <span className="key">module</span>
        {": "}
        <span className="arg">"OrderModule"</span>
        {" })"}
      </>
    ),
  },
  {
    role: "mcp",
    body: (
      <>
        <span className="key">command.scaffold</span>
        {"({ "}
        <span className="key">name</span>
        {": "}
        <span className="arg">"CancelOrder"</span>
        {" })"}
      </>
    ),
  },
  {
    role: "mcp",
    body: (
      <>
        <span className="ok">→</span> registered on graph · openapi · mcp ·
        client regen queued
      </>
    ),
  },
];

const VISIBLE = 8;
const TICK_MS = 1700;

const ROLE_LABEL: Record<Role, string> = {
  user: "USER",
  agent: "AGENT",
  mcp: "MCP",
};

interface AgentTerminalProps {
  readonly session?: string;
}

export const AgentTerminal: FC<AgentTerminalProps> = ({
  session = "nitro mcp · session 7c3a · cart-ops",
}) => {
  // Maintain a rolling window of indices into SCRIPT. Initial window is the
  // first VISIBLE lines so the terminal lands populated, not empty.
  const [head, setHead] = useState(0);

  useEffect(() => {
    const t = setInterval(() => {
      setHead((h) => (h + 1) % SCRIPT.length);
    }, TICK_MS);
    return () => clearInterval(t);
  }, []);

  const lines: ScriptLine[] = [];
  for (let i = 0; i < VISIBLE; i++) {
    lines.push(SCRIPT[(head + i) % SCRIPT.length]);
  }

  return (
    <div className="cc-term" aria-hidden>
      <div className="cc-term-inner">
        <div className="cc-term-header">
          <span>{session}</span>
          <span className="dots">
            <span />
            <span />
            <span />
          </span>
        </div>
        <div className="cc-term-body">
          {lines.map((l, i) => {
            const isLast = i === lines.length - 1;
            return (
              <div
                key={`${head}-${i}`}
                className="cc-term-line"
                style={{ animationDelay: `${i * 30}ms` }}
              >
                <span className={`cc-term-pill is-${l.role}`}>
                  {ROLE_LABEL[l.role]}
                </span>
                <span className="body">
                  {l.body}
                  {isLast && <span className="cc-term-cursor" />}
                </span>
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
};
