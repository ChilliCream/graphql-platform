"use client";

import React from "react";

import { ScaledCanvas } from "./ScaledCanvas";
import { DESKTOP_ADAPTERS, adapterExitXs } from "./constants";

interface Client {
  key: string;
  name: string;
  kind: string;
  adapter?: string;
  adapters?: string[];
}

export const ActClients: React.FC = () => {
  const W = 1480;
  const H = 760;

  const CLIENTS: Client[] = [
    { key: "web", name: "Web", kind: "Browser SPA", adapter: "graphql" },
    {
      key: "mobile",
      name: "Mobile",
      kind: "iOS / Android",
      adapter: "graphql",
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

  const slotCursor: Record<string, number> = {
    graphql: 0,
    openapi: 0,
    mcp: 0,
    grpc: 0,
  };
  const adapterIdx = (key: string) =>
    DESKTOP_ADAPTERS.findIndex((a) => a.key === key);

  interface Edge {
    key: string;
    clientIdx: number;
    adapterKey: string;
    xFrom: number;
    xTo: number;
    span: number;
  }

  const edges: Edge[] = [];
  CLIENTS.forEach((c, ci) => {
    const adapters = c.adapters || (c.adapter ? [c.adapter] : []);
    adapters.forEach((aKey) => {
      const ai = adapterIdx(aKey);
      const slot = slotCursor[aKey]++;
      const xFrom = adapterExitXs(ai)[slot];
      edges.push({
        key: `${c.key}-${aKey}`,
        clientIdx: ci,
        adapterKey: aKey,
        xFrom,
        xTo: 0,
        span: 0,
      });
    });
  });

  const CLIENT_X = [0, 1, 2, 3].map((i) => 200 + i * 360);
  const CLIENT_Y_FRAME_TOP = 460;
  const FRAME_W = 160;
  const TARGET_Y = CLIENT_Y_FRAME_TOP - 8;

  const incomingPerClient: Record<number, number> = {};
  edges.forEach((e) => {
    incomingPerClient[e.clientIdx] = (incomingPerClient[e.clientIdx] || 0) + 1;
  });
  const seenPerClient: Record<number, number> = {};
  edges.forEach((e) => {
    const total = incomingPerClient[e.clientIdx];
    const idx =
      (seenPerClient[e.clientIdx] = (seenPerClient[e.clientIdx] || 0) + 1) - 1;
    const span = (total - 1) * 22;
    e.xTo = CLIENT_X[e.clientIdx] + (idx - (total - 1) / 2) * 22;
    e.span = span;
  });

  const linePath = (e: Edge) => {
    const yIn0 = 0;
    const yStraight = 80;
    const yBend = 200;
    return [
      `M ${e.xFrom} ${yIn0}`,
      `L ${e.xFrom} ${yStraight}`,
      `C ${e.xFrom} ${yBend} ${e.xTo} ${(yBend + TARGET_Y) / 2} ${
        e.xTo
      } ${TARGET_Y}`,
    ].join(" ");
  };

  return (
    <section
      className="cc-act cc-act-clients cc-act-spills"
      data-screen-label="05 Clients & Surfaces"
    >
      <div className="cc-act-label">
        <span className="num">05</span> Clients &amp; Surfaces
      </div>

      <ScaledCanvas width={W} height={H}>
        <svg
          width={W}
          height={H}
          viewBox={`0 0 ${W} ${H}`}
          style={{ position: "absolute", inset: 0, pointerEvents: "none" }}
          aria-hidden
        >
          {edges.map((e) => (
            <path
              key={e.key}
              d={linePath(e)}
              stroke="var(--cc-ink)"
              strokeDasharray="3 6"
              strokeWidth="var(--cc-line-w)"
              fill="none"
              strokeLinecap="round"
              opacity="0.7"
            />
          ))}
        </svg>

        {CLIENTS.map((c, i) => {
          const adapters = c.adapters || (c.adapter ? [c.adapter] : []);
          return (
            <div
              key={c.key}
              className="cc-endpoint-d"
              style={{
                position: "absolute",
                left: CLIENT_X[i] - FRAME_W / 2,
                top: CLIENT_Y_FRAME_TOP,
                width: FRAME_W,
              }}
            >
              <div className="cc-endpoint-frame-d">
                <svg
                  viewBox="0 0 100 100"
                  width="60%"
                  height="60%"
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
              <div className="cc-endpoint-name-d">{c.name}</div>
              <div className="cc-endpoint-kind-d">{c.kind}</div>
              <div className="cc-endpoint-protocols-d">
                {adapters.map((aKey) => {
                  const a = DESKTOP_ADAPTERS.find((x) => x.key === aKey);
                  return (
                    <span key={aKey} className="cc-endpoint-protocol-d">
                      {a?.label}
                    </span>
                  );
                })}
              </div>
            </div>
          );
        })}

        <div
          className="cc-section-headline-fade"
          style={{
            position: "absolute",
            left: 0,
            width: W,
            top: 40,
            textAlign: "center",
            zIndex: 5,
          }}
        >
          <div className="eyebrow">Every surface, one truth</div>
          <h2
            className="display"
            style={{
              fontSize: "clamp(36px, 4.4vw, 60px)",
              margin: "8px auto",
              maxWidth: "20ch",
            }}
          >
            Choose your protocol.
            <br />
            One source of truth.
          </h2>
          <p className="cc-explainer">
            Each surface picks the protocol that fits it. Web and mobile apps
            consume the graph over GraphQL for typed, request-shaped queries. AI
            agents speak MCP. Partner integrations and external systems pull the
            same data over OpenAPI or gRPC. Same composed graph, different doors
            in.
          </p>
        </div>
      </ScaledCanvas>
    </section>
  );
};
