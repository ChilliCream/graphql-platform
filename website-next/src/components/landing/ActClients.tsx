"use client";

import React, { useEffect, useRef, useState } from "react";

import { DESKTOP_ADAPTERS, adapterExitXs } from "./constants";
import {
  useAnchorContext,
  useLandingRoot,
  useMeasureEffect,
} from "./AnchorContext";

interface Client {
  key: string;
  name: string;
  kind: string;
  adapter?: string;
  adapters?: string[];
}

// Reference geometry matches Act 4's stage so the fan-in lines line up with
// the adapter exit lanes one section above. No transform: scale.
const REF_W = 1480;
const REF_H = 760;
const PILL_W_REF = 200;
const PILL_GAP_REF = 60;
const PILLS_TOTAL_REF =
  DESKTOP_ADAPTERS.length * PILL_W_REF +
  (DESKTOP_ADAPTERS.length - 1) * PILL_GAP_REF;
const PILLS_X0_REF = (REF_W - PILLS_TOTAL_REF) / 2;
const PILL_CX_REF = DESKTOP_ADAPTERS.map(
  (_, i) => PILLS_X0_REF + i * (PILL_W_REF + PILL_GAP_REF) + PILL_W_REF / 2
);

const CLIENT_X_REF = [0, 1, 2, 3].map((i) => 200 + i * 360);
const CLIENT_Y_FRAME_TOP_REF = 460;
const FRAME_W_REF = 160;
const TARGET_Y_REF = CLIENT_Y_FRAME_TOP_REF - 8;

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

interface Edge {
  key: string;
  clientIdx: number;
  adapterKey: string;
  xFromRef: number;
  xToRef: number;
}

// Build the edge list in reference space. Each adapter has a slot cursor
// that hands out exit lanes in declaration order; clients claim them in
// the same iteration order as the CLIENTS array.
const buildEdges = (): Edge[] => {
  const slotCursor: Record<string, number> = {
    graphql: 0,
    openapi: 0,
    mcp: 0,
    grpc: 0,
  };
  const adapterIdx = (key: string) =>
    DESKTOP_ADAPTERS.findIndex((a) => a.key === key);

  const edges: Edge[] = [];
  CLIENTS.forEach((c, ci) => {
    const adapters = c.adapters || (c.adapter ? [c.adapter] : []);
    adapters.forEach((aKey) => {
      const ai = adapterIdx(aKey);
      const slot = slotCursor[aKey]++;
      const xFromRef = adapterExitXs(ai, PILL_CX_REF[ai])[slot];
      edges.push({
        key: `${c.key}-${aKey}`,
        clientIdx: ci,
        adapterKey: aKey,
        xFromRef,
        xToRef: 0,
      });
    });
  });
  // Resolve xTo by spreading multiple incoming edges across the client's
  // frame top so the lines don't overlap on arrival.
  const incomingPerClient: Record<number, number> = {};
  edges.forEach((e) => {
    incomingPerClient[e.clientIdx] = (incomingPerClient[e.clientIdx] || 0) + 1;
  });
  const seenPerClient: Record<number, number> = {};
  edges.forEach((e) => {
    const total = incomingPerClient[e.clientIdx];
    const idx =
      (seenPerClient[e.clientIdx] = (seenPerClient[e.clientIdx] || 0) + 1) - 1;
    e.xToRef = CLIENT_X_REF[e.clientIdx] + (idx - (total - 1) / 2) * 22;
  });
  return edges;
};

const EDGES = buildEdges();

export const ActClients: React.FC = () => {
  const sectionRef = useRef<HTMLElement>(null);
  const stageRef = useRef<HTMLDivElement>(null);
  const endpointRefs = useRef<Array<HTMLDivElement | null>>([]);
  const [stageDims, setStageDims] = useState<{ w: number; h: number }>({
    w: REF_W,
    h: REF_H,
  });
  const { register, unregister } = useAnchorContext();
  const root = useLandingRoot();

  // Register one anchor per endpoint frame so future connector code can
  // route to them (ConnectorLayer doesn't currently consume these, but the
  // anchors are kept for parity with the rest of the act surface).
  useMeasureEffect(
    () => {
      const stage = stageRef.current;
      if (!stage || !root) {
        return;
      }
      const sRect = stage.getBoundingClientRect();
      const rRect = root.getBoundingClientRect();
      setStageDims((prev) =>
        prev.w === sRect.width && prev.h === sRect.height
          ? prev
          : { w: sRect.width, h: sRect.height }
      );

      CLIENTS.forEach((_, i) => {
        const el = endpointRefs.current[i];
        if (!el) {
          return;
        }
        const eRect = el.getBoundingClientRect();
        register(`actClients.endpoint-${i}`, {
          x: eRect.left - rRect.left + eRect.width / 2,
          y: eRect.top - rRect.top + eRect.height / 2,
          kind: "service-entry",
        });
      });
    },
    [stageRef, sectionRef],
    [register, root]
  );

  useEffect(
    () => () => {
      CLIENTS.forEach((_, i) => {
        unregister(`actClients.endpoint-${i}`);
      });
    },
    [unregister]
  );

  const xPct = (refX: number) => `${(refX / REF_W) * 100}%`;
  const yPct = (refY: number) => `${(refY / REF_H) * 100}%`;
  const wPct = (sizeRef: number) => `${(sizeRef / REF_W) * 100}%`;

  const { w: stageW, h: stageH } = stageDims;
  const scaleX = stageW / REF_W;
  const scaleY = stageH / REF_H;

  const linePath = (e: Edge): string => {
    // Reference geometry: lines drop from y=0 to y=80 then bend through
    // y=200 to TARGET_Y. We project ref-space to stage-pixel-space using
    // the current stage dims.
    const yIn0 = 0;
    const yStraight = 80 * scaleY;
    const yBend = 200 * scaleY;
    const yTarget = TARGET_Y_REF * scaleY;
    const xFrom = e.xFromRef * scaleX;
    const xTo = e.xToRef * scaleX;
    return [
      `M ${xFrom} ${yIn0}`,
      `L ${xFrom} ${yStraight}`,
      `C ${xFrom} ${yBend} ${xTo} ${(yBend + yTarget) / 2} ${xTo} ${yTarget}`,
    ].join(" ");
  };

  return (
    <section
      ref={sectionRef}
      className="cc-act cc-act-clients cc-act-spills"
      data-screen-label="05 Clients & Surfaces"
    >
      <div className="cc-actclients-stage" ref={stageRef}>
        <svg
          width={stageW}
          height={stageH}
          viewBox={`0 0 ${stageW} ${stageH}`}
          style={{
            position: "absolute",
            inset: 0,
            width: "100%",
            height: "100%",
            pointerEvents: "none",
            overflow: "visible",
          }}
          aria-hidden
        >
          {EDGES.map((e) => (
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
              ref={(el) => {
                endpointRefs.current[i] = el;
              }}
              className="cc-endpoint-d"
              style={{
                position: "absolute",
                left: xPct(CLIENT_X_REF[i] - FRAME_W_REF / 2),
                top: yPct(CLIENT_Y_FRAME_TOP_REF),
                width: wPct(FRAME_W_REF),
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
            width: "100%",
            top: yPct(40),
            textAlign: "center",
            zIndex: 5,
          }}
        >
          <div className="eyebrow">Every surface, one truth</div>
          <h2
            className="display cc-actclients-headline"
            style={{ margin: "8px auto", maxWidth: "20ch" }}
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
      </div>
    </section>
  );
};
