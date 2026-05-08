"use client";

import React from "react";

import { ADAPTERS } from "./Act4";

interface Client {
  key: string;
  name: string;
  kind: string;
  adapters: string[];
}

const CLIENTS: Client[] = [
  { key: "web", name: "Web", kind: "Browser SPA", adapters: ["graphql"] },
  {
    key: "mobile",
    name: "Mobile",
    kind: "iOS / Android",
    adapters: ["graphql"],
  },
  {
    key: "agent",
    name: "AI Agents",
    kind: "MCP / Tools",
    adapters: ["graphql", "mcp"],
  },
  {
    key: "partner",
    name: "Partners",
    kind: "Federated API",
    adapters: ["openapi", "grpc"],
  },
];

export const ActClients: React.FC = () => {
  const adapterLabel = (key: string) => {
    const a = ADAPTERS.find((x) => x.key === key);
    return a ? a.label : key;
  };

  return (
    <section className="act clients" data-screen-label="05 Clients & Surfaces">
      <div className="act-label">
        <span className="num">05</span> Clients &amp; Surfaces
      </div>
      <div className="act-heading section-headline-fade">
        <div className="eyebrow">Every surface, one truth</div>
        <h2 className="display">
          Choose your protocol.
          <br />
          One source of truth.
        </h2>
      </div>

      <div className="clients-grid">
        {CLIENTS.map((c) => (
          <div key={c.key} className="endpoint">
            <div className="frame">
              <svg
                viewBox="0 0 100 100"
                width="70%"
                height="70%"
                fill="none"
                stroke="var(--cc-ink)"
                strokeWidth="2"
              >
                {c.key === "web" && (
                  <g>
                    <rect x="14" y="22" width="72" height="56" rx="4" />
                    <line x1="14" y1="34" x2="86" y2="34" />
                    <circle cx="22" cy="28" r="1.5" fill="var(--cc-ink)" />
                    <circle cx="28" cy="28" r="1.5" fill="var(--cc-ink)" />
                  </g>
                )}
                {c.key === "mobile" && (
                  <g>
                    <rect x="32" y="14" width="36" height="72" rx="6" />
                    <line x1="44" y1="80" x2="56" y2="80" />
                  </g>
                )}
                {c.key === "agent" && (
                  <g>
                    <circle cx="50" cy="50" r="22" />
                    <circle cx="42" cy="46" r="2.5" fill="var(--cc-ink)" />
                    <circle cx="58" cy="46" r="2.5" fill="var(--cc-ink)" />
                    <line x1="42" y1="58" x2="58" y2="58" />
                    <line x1="50" y1="22" x2="50" y2="14" />
                    <circle cx="50" cy="12" r="2" />
                  </g>
                )}
                {c.key === "partner" && (
                  <g>
                    <rect x="18" y="30" width="28" height="40" />
                    <rect x="54" y="30" width="28" height="40" />
                    <line x1="46" y1="50" x2="54" y2="50" />
                  </g>
                )}
              </svg>
            </div>
            <div className="name">{c.name}</div>
            <div className="kind">{c.kind}</div>
            <div className="endpoint-protocols">
              {c.adapters.map((aKey) => (
                <span key={aKey} className="endpoint-protocol">
                  {adapterLabel(aKey)}
                </span>
              ))}
            </div>
          </div>
        ))}
      </div>
    </section>
  );
};
