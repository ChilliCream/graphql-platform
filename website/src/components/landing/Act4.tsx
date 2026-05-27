"use client";

import React from "react";

export const ADAPTERS = [
  {
    key: "graphql",
    label: "GraphQL",
    caption: "Catalog · Billing · Users → GraphQL",
  },
  { key: "openapi", label: "OpenAPI", caption: "Ordering → OpenAPI" },
  { key: "mcp", label: "MCP", caption: "Users → MCP" },
  { key: "grpc", label: "gRPC", caption: "Shipping → gRPC" },
] as const;

export const Act4: React.FC = () => {
  return (
    <section className="act adapters" data-screen-label="04 Adapters">
      <div className="act-label">
        <span className="num">04</span> Adapters
      </div>
      <div className="act-heading section-headline-fade">
        <div className="eyebrow">Adapters</div>
        <h2 className="display">The API that speaks any language.</h2>
      </div>

      <div className="adapter-stack">
        {ADAPTERS.map((a) => (
          <div key={a.key} className="adapter-cell">
            <div className="adapter-caption">{a.caption}</div>
            <div className="adapter-pill" data-key={a.key}>
              {a.label}
            </div>
          </div>
        ))}
      </div>

      <svg
        className="adapter-fanout"
        width="120"
        height="24"
        viewBox="0 0 120 24"
        aria-hidden
      >
        <defs>
          <linearGradient
            id="cc-act4-line-fade"
            x1="0"
            y1="0"
            x2="0"
            y2="24"
            gradientUnits="userSpaceOnUse"
          >
            <stop offset="0" stopColor="black" />
            <stop offset="0.25" stopColor="white" />
            <stop offset="0.75" stopColor="white" />
            <stop offset="1" stopColor="black" />
          </linearGradient>
          <mask
            id="cc-act4-line-mask"
            maskUnits="userSpaceOnUse"
            x="0"
            y="0"
            width="120"
            height="24"
          >
            <rect
              x="0"
              y="0"
              width="120"
              height="24"
              fill="url(#cc-act4-line-fade)"
            />
          </mask>
        </defs>
        <g mask="url(#cc-act4-line-mask)">
          {[15, 50, 70, 105].map((x, i) => (
            <line
              key={i}
              x1={x}
              y1="0"
              x2={x}
              y2="24"
              stroke="var(--cc-ink)"
              strokeWidth="2"
              strokeDasharray="2 4"
              strokeLinecap="round"
              opacity="0.7"
            />
          ))}
        </g>
      </svg>
    </section>
  );
};
