"use client";

import React, { FC, useMemo } from "react";
import { useTheme } from "@/src/theme/ThemeProvider";
import {
  ReactFlow,
  Background,
  BackgroundVariant,
  Controls,
  Handle,
  Position,
  MarkerType,
  Panel,
  type Node,
  type Edge,
  type NodeProps,
} from "@xyflow/react";

interface Palette {
  readonly bg: string;
  readonly surface: string;
  readonly surfaceAlt: string;
  readonly border: string;
  readonly dots: string;
  readonly text: string;
  readonly muted: string;
  readonly legendBg: string;
  readonly shadow: string;
  readonly shadowEmphatic: string;
  readonly client: string; // subscription (client -> gateway)
  readonly stream: string; // event stream (broker -> gateway)
  readonly publish: string; // publish (service -> broker)
  readonly http: string; // GraphQL over HTTP (gateway -> service)
  readonly broker: string;
  readonly gateway: string;
}

// Dark mirrors the GitHub-dark palette of the rest of the docs visualizations.
// Light surfaces and text follow the site tokens; edge hues are literals
// because React Flow derives marker ids from them.
const PALETTES: Record<"light" | "dark", Palette> = {
  dark: {
    bg: "#0d1117",
    surface: "#161b22",
    surfaceAlt: "#1c2230",
    border: "#30363d",
    dots: "#30363d",
    text: "#e6edf3",
    muted: "#8b949e",
    legendBg: "rgba(13,17,23,0.82)",
    shadow: "0 4px 12px rgba(0,0,0,0.25)",
    shadowEmphatic: "0 8px 24px rgba(0,0,0,0.35)",
    client: "#58a6ff",
    stream: "#3fb950",
    publish: "#d29922",
    http: "#a371f7",
    broker: "#d29922",
    gateway: "#a371f7",
  },
  light: {
    bg: "var(--color-cc-code-bg)",
    surface: "var(--color-cc-bg)",
    surfaceAlt: "var(--color-cc-card-bg)",
    border: "var(--color-cc-card-border)",
    dots: "var(--color-cc-ink-faint)",
    text: "var(--color-cc-heading)",
    muted: "var(--color-cc-ink-dim)",
    legendBg: "color-mix(in srgb, var(--color-cc-code-bg) 82%, transparent)",
    shadow: "0 4px 12px rgba(15,23,42,0.1)",
    shadowEmphatic: "0 8px 24px rgba(15,23,42,0.14)",
    client: "#075985",
    stream: "#0f6651",
    publish: "#854d0e",
    http: "#5b21b6",
    broker: "#854d0e",
    gateway: "#5b21b6",
  },
};

function usePalette(): Palette {
  return PALETTES[useTheme().resolvedTheme];
}

type NodeKind = "broker" | "gateway" | "client" | "service";

interface DiagramNodeData {
  readonly kind: NodeKind;
  readonly title: string;
  readonly subtitle?: string;
  readonly [key: string]: unknown;
}

function accentOf(kind: NodeKind, palette: Palette): string {
  switch (kind) {
    case "broker":
      return palette.broker;
    case "gateway":
      return palette.gateway;
    case "client":
      return palette.client;
    case "service":
      return palette.stream;
  }
}

const BrokerIcon: FC<{ color: string }> = ({ color }) => (
  <svg
    width="18"
    height="18"
    viewBox="0 0 24 24"
    fill="none"
    stroke={color}
    strokeWidth="2"
    strokeLinecap="round"
    strokeLinejoin="round"
  >
    <rect x="3" y="4" width="18" height="4" rx="1" />
    <rect x="3" y="10" width="18" height="4" rx="1" />
    <rect x="3" y="16" width="18" height="4" rx="1" />
  </svg>
);

const GatewayIcon: FC<{ color: string }> = ({ color }) => (
  <svg
    width="18"
    height="18"
    viewBox="0 0 24 24"
    fill="none"
    stroke={color}
    strokeWidth="2"
    strokeLinecap="round"
    strokeLinejoin="round"
  >
    <polygon points="12 2 20 7 20 17 12 22 4 17 4 7" />
    <circle cx="12" cy="12" r="3" />
  </svg>
);

const ClientIcon: FC<{ color: string }> = ({ color }) => (
  <svg
    width="18"
    height="18"
    viewBox="0 0 24 24"
    fill="none"
    stroke={color}
    strokeWidth="2"
    strokeLinecap="round"
    strokeLinejoin="round"
  >
    <rect x="2" y="4" width="20" height="14" rx="2" />
    <line x1="8" y1="21" x2="16" y2="21" />
    <line x1="12" y1="18" x2="12" y2="21" />
  </svg>
);

const ServiceIcon: FC<{ color: string }> = ({ color }) => (
  <svg
    width="18"
    height="18"
    viewBox="0 0 24 24"
    fill="none"
    stroke={color}
    strokeWidth="2"
    strokeLinecap="round"
    strokeLinejoin="round"
  >
    <ellipse cx="12" cy="5" rx="8" ry="3" />
    <path d="M4 5v6c0 1.7 3.6 3 8 3s8-1.3 8-3V5" />
    <path d="M4 11v6c0 1.7 3.6 3 8 3s8-1.3 8-3v-6" />
  </svg>
);

const ICON: Record<NodeKind, FC<{ color: string }>> = {
  broker: BrokerIcon,
  gateway: GatewayIcon,
  client: ClientIcon,
  service: ServiceIcon,
};

const DiagramNode: FC<NodeProps> = ({ data }) => {
  const d = data as DiagramNodeData;
  const palette = usePalette();
  const accent = accentOf(d.kind, palette);
  const handleStyle: React.CSSProperties = {
    width: 7,
    height: 7,
    background: palette.border,
    border: "none",
  };
  const Icon = ICON[d.kind];
  const emphatic = d.kind === "broker" || d.kind === "gateway";

  return (
    <div
      style={{
        position: "relative",
        width: d.kind === "broker" ? 332 : emphatic ? 300 : 156,
        padding: "12px 14px",
        borderRadius: 10,
        background: emphatic ? palette.surfaceAlt : palette.surface,
        border: `1px solid ${palette.border}`,
        borderTop: `3px solid ${accent}`,
        boxShadow: emphatic
          ? `0 0 0 1px ${accent}22, ${palette.shadowEmphatic}`
          : palette.shadow,
        display: "flex",
        alignItems: "center",
        gap: 10,
      }}
    >
      <span
        style={{
          flexShrink: 0,
          width: 32,
          height: 32,
          borderRadius: 8,
          background: `${accent}1a`,
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
        }}
      >
        <Icon color={accent} />
      </span>
      <span style={{ display: "flex", flexDirection: "column", minWidth: 0 }}>
        <span
          style={{
            color: palette.text,
            fontSize: emphatic ? 15 : 13,
            fontWeight: 600,
            lineHeight: 1.25,
            whiteSpace: "nowrap",
          }}
        >
          {d.title}
        </span>
        {d.subtitle ? (
          <span
            style={{
              color: palette.muted,
              fontSize: 11,
              lineHeight: 1.3,
              fontFamily:
                "ui-monospace, SFMono-Regular, Menlo, Consolas, monospace",
              marginTop: 2,
            }}
          >
            {d.subtitle}
          </span>
        ) : null}
      </span>

      {/* Broker: receives publishes on the right, streams out the bottom. */}
      {d.kind === "broker" && (
        <>
          <Handle
            id="in"
            type="target"
            position={Position.Right}
            style={handleStyle}
          />
          <Handle
            id="out"
            type="source"
            position={Position.Bottom}
            style={handleStyle}
          />
        </>
      )}
      {/* Gateway: subs in (left), events in (top), http out (right). */}
      {d.kind === "gateway" && (
        <>
          <Handle
            id="subs"
            type="target"
            position={Position.Left}
            style={handleStyle}
          />
          <Handle
            id="events"
            type="target"
            position={Position.Top}
            style={handleStyle}
          />
          <Handle
            id="http"
            type="source"
            position={Position.Right}
            style={handleStyle}
          />
        </>
      )}
      {/* Client: subscribes out the right. */}
      {d.kind === "client" && (
        <Handle
          id="out"
          type="source"
          position={Position.Right}
          style={handleStyle}
        />
      )}
      {/* Service: publishes out the top, answers http on the left. */}
      {d.kind === "service" && (
        <>
          <Handle
            id="pub"
            type="source"
            position={Position.Top}
            style={handleStyle}
          />
          <Handle
            id="http"
            type="target"
            position={Position.Left}
            style={handleStyle}
          />
        </>
      )}
    </div>
  );
};

const nodeTypes = { diagram: DiagramNode };

const NODES: Node[] = [
  {
    id: "broker",
    type: "diagram",
    position: { x: 314, y: 8 },
    data: {
      kind: "broker",
      title: "Message Broker",
      subtitle: "NATS · Kafka · Redis · SQS · Event Hubs",
    },
    draggable: false,
    selectable: false,
  },
  {
    id: "gateway",
    type: "diagram",
    position: { x: 385, y: 268 },
    data: {
      kind: "gateway",
      title: "Fusion Gateway",
      subtitle: "subscribes · resolves · streams",
    },
    draggable: false,
    selectable: false,
  },
  {
    id: "client-a",
    type: "diagram",
    position: { x: 24, y: 168 },
    data: { kind: "client", title: "Client A", subtitle: "subscription" },
    draggable: false,
    selectable: false,
  },
  {
    id: "client-b",
    type: "diagram",
    position: { x: 24, y: 268 },
    data: { kind: "client", title: "Client B", subtitle: "subscription" },
    draggable: false,
    selectable: false,
  },
  {
    id: "client-c",
    type: "diagram",
    position: { x: 24, y: 368 },
    data: { kind: "client", title: "Client C", subtitle: "subscription" },
    draggable: false,
    selectable: false,
  },
  {
    id: "svc-products",
    type: "diagram",
    position: { x: 772, y: 168 },
    data: { kind: "service", title: "Products", subtitle: "subgraph" },
    draggable: false,
    selectable: false,
  },
  {
    id: "svc-reviews",
    type: "diagram",
    position: { x: 772, y: 268 },
    data: { kind: "service", title: "Reviews", subtitle: "subgraph" },
    draggable: false,
    selectable: false,
  },
  {
    id: "svc-accounts",
    type: "diagram",
    position: { x: 772, y: 368 },
    data: { kind: "service", title: "Accounts", subtitle: "subgraph" },
    draggable: false,
    selectable: false,
  },
];

const edge = (
  palette: Palette,
  id: string,
  source: string,
  sourceHandle: string,
  target: string,
  targetHandle: string,
  color: string,
  label?: string,
): Edge => ({
  id,
  source,
  sourceHandle,
  target,
  targetHandle,
  animated: true,
  label,
  labelShowBg: true,
  labelBgPadding: [6, 3],
  labelBgBorderRadius: 4,
  labelBgStyle: { fill: palette.bg, fillOpacity: 0.85 },
  labelStyle: { fill: color, fontSize: 11, fontWeight: 600 },
  style: { stroke: color, strokeWidth: 2 },
  markerEnd: { type: MarkerType.ArrowClosed, color, width: 16, height: 16 },
});

const buildEdges = (palette: Palette): Edge[] => [
  // Clients subscribe to the gateway.
  edge(
    palette,
    "sub-a",
    "client-a",
    "out",
    "gateway",
    "subs",
    palette.client,
    "subscribe",
  ),
  edge(palette, "sub-b", "client-b", "out", "gateway", "subs", palette.client),
  edge(palette, "sub-c", "client-c", "out", "gateway", "subs", palette.client),
  // Gateway reads the event stream from the broker.
  edge(
    palette,
    "stream",
    "broker",
    "out",
    "gateway",
    "events",
    palette.stream,
    "event stream",
  ),
  // Services publish events to the broker.
  edge(
    palette,
    "pub-p",
    "svc-products",
    "pub",
    "broker",
    "in",
    palette.publish,
    "publish",
  ),
  edge(palette, "pub-r", "svc-reviews", "pub", "broker", "in", palette.publish),
  edge(
    palette,
    "pub-a",
    "svc-accounts",
    "pub",
    "broker",
    "in",
    palette.publish,
  ),
  // Gateway resolves each event with stateless GraphQL-over-HTTP fetches.
  edge(
    palette,
    "http-p",
    "gateway",
    "http",
    "svc-products",
    "http",
    palette.http,
    "GraphQL / HTTP",
  ),
  edge(
    palette,
    "http-r",
    "gateway",
    "http",
    "svc-reviews",
    "http",
    palette.http,
  ),
  edge(
    palette,
    "http-a",
    "gateway",
    "http",
    "svc-accounts",
    "http",
    palette.http,
  ),
];

const legendOf = (
  palette: Palette,
): ReadonlyArray<{ color: string; label: string }> => [
  { color: palette.client, label: "Client subscription" },
  { color: palette.stream, label: "Event stream (read)" },
  { color: palette.publish, label: "Publish event" },
  { color: palette.http, label: "GraphQL over HTTP" },
];

interface FusionSubscriptionsDiagramInnerProps {
  readonly expanded: boolean;
}

export const FusionSubscriptionsDiagramInner: FC<
  FusionSubscriptionsDiagramInnerProps
> = ({ expanded }) => {
  const { resolvedTheme } = useTheme();
  const palette = PALETTES[resolvedTheme];
  const nodes = useMemo(() => NODES, []);
  const edges = useMemo(() => buildEdges(palette), [palette]);

  return (
    <ReactFlow
      nodes={nodes}
      edges={edges}
      nodeTypes={nodeTypes}
      fitView
      fitViewOptions={{ padding: 0.12 }}
      proOptions={{ hideAttribution: true }}
      nodesDraggable={false}
      nodesConnectable={false}
      elementsSelectable={false}
      panOnDrag={expanded}
      zoomOnScroll={expanded}
      zoomOnPinch={expanded}
      zoomOnDoubleClick={expanded}
      preventScrolling={expanded}
      minZoom={0.4}
      maxZoom={1.6}
      colorMode={resolvedTheme}
      style={{ background: palette.bg }}
    >
      <Background
        variant={BackgroundVariant.Dots}
        gap={22}
        size={1}
        color={palette.dots}
      />
      {expanded ? <Controls showInteractive={false} /> : null}
      <Panel position="bottom-left">
        <div
          style={{
            display: "flex",
            flexWrap: "wrap",
            gap: "8px 16px",
            padding: "10px 12px",
            borderRadius: 8,
            background: palette.legendBg,
            border: `1px solid ${palette.border}`,
            backdropFilter: "blur(4px)",
          }}
        >
          {legendOf(palette).map((item) => (
            <span
              key={item.label}
              style={{
                display: "inline-flex",
                alignItems: "center",
                gap: 7,
                color: palette.muted,
                fontSize: 11.5,
                fontWeight: 500,
              }}
            >
              <span
                style={{
                  width: 18,
                  height: 0,
                  borderTop: `2px solid ${item.color}`,
                }}
              />
              {item.label}
            </span>
          ))}
        </div>
      </Panel>
    </ReactFlow>
  );
};
