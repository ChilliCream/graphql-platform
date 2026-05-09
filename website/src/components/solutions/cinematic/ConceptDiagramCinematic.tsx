"use client";

import React, { FC } from "react";

import { Band } from "@/components/redesign-system/Band";
import {
  ActLabel,
  Anchor,
  ConnectorLine,
  TerminalChipRow,
} from "@/components/redesign-system/cinematic";
import type { DiagramKind } from "@/data/solutions/types";

import { ConceptDiagramBody, DIAGRAM_TITLES } from "../ConceptDiagram";

// Cinematic variant of section 04 (Architecture). Reuses the diagram
// titles and SVG vocabulary from the default `ConceptDiagram` so the
// page reads as the same architecture beat, but threads:
//
//   * a per-variant `<TerminalChipRow accent="prism">` above the diagram
//     ("what's in the supergraph") so the band opens with adapter-row
//     DNA from the homepage;
//   * an overlay of named `<Anchor>` endpoints positioned inside the
//     existing language/team/topic node circles AND inside the gateway,
//     plus `<ConnectorLine curve="bezier">` instances drawn between
//     them. The overlay sits absolute over the diagram canvas; the
//     `containerSelector` scopes the connectors to the diagram canvas
//     so the curves resolve against the SVG box, not the page.
//
// Each diagram variant is a per-variant module that picks node positions
// (as % of the canvas) and connector colors (mapped to --cc-col-*).
// Positions are derived from the SVG viewBox math in `ConceptDiagram`.

interface ConceptDiagramCinematicProps {
  readonly kind: DiagramKind;
}

// ============================================================
// Anchor placement helpers
// ============================================================
//
// The existing diagrams use viewBox="0 0 800 450". An anchor placed at
// SVG (cx, cy) lives at (cx / 800, cy / 450) in % of the canvas. The
// overlay container has the same aspect ratio because the canvas uses
// preserveAspectRatio="xMidYMid meet". We mark the anchors at the
// inner-circle / inner-rect centers so connector lines terminate on the
// node interior, not its outline.
//
// Note: because preserveAspectRatio="meet" letterboxes the SVG inside its
// canvas, the visible diagram may be smaller than the canvas. For the
// connector overlay we accept the small drift (visually tracks the
// underlying nodes within ~1-2% on standard breakpoints).

const LANGUAGE_CHIPS = ["C# / .NET", "Java", "Go", "Python", "Node", "Rust"];

const FEDERATION_CHIPS = [
  "TEAM IDENTITY",
  "TEAM ORDERS",
  "TEAM CATALOG",
  "TEAM SUPPORT",
];

const SINGLE_GRAPH_CHIPS = ["WEB", "MOBILE", "AGENTS"];

const AGENT_CHIPS = ["CURSOR", "CLAUDE", "COPILOT", "IN-HOUSE"];

const EVENT_BUS_CHIPS = ["ORDERS", "BILLING", "INVENTORY"];

const COMPLIANCE_CHIPS = ["SUBGRAPH A", "SUBGRAPH B", "SUBGRAPH C"];

const CHIP_ROWS: Record<DiagramKind, readonly string[]> = {
  polyglot: LANGUAGE_CHIPS,
  federation: FEDERATION_CHIPS,
  "single-graph": SINGLE_GRAPH_CHIPS,
  agents: AGENT_CHIPS,
  "event-bus": EVENT_BUS_CHIPS,
  compliance: COMPLIANCE_CHIPS,
};

// Positioned absolute span inside the canvas. Style accepts left/top in %.
const PositionedAnchor: FC<{
  readonly id: string;
  readonly x: number;
  readonly y: number;
}> = ({ id, x, y }) => (
  <span
    style={{
      position: "absolute",
      left: `${x}%`,
      top: `${y}%`,
      width: 0,
      height: 0,
      pointerEvents: "none",
    }}
  >
    <Anchor id={id} />
  </span>
);

// ============================================================
// Per-variant overlays. Each maps to the geometry baked into the
// matching variant in `ConceptDiagram`, expressed as % of the
// 800x450 viewBox.
// ============================================================

const PolyglotOverlay: FC<{ readonly canvasSelector: string }> = ({
  canvasSelector,
}) => (
  <div className="cc-sl-cin-diagram-overlay" data-cc-connector-layer="polyglot">
    {/* language nodes along the top arc, gateway center at (400, 236) */}
    <PositionedAnchor
      id="poly-java"
      x={(100 / 800) * 100}
      y={(90 / 450) * 100}
    />
    <PositionedAnchor id="poly-go" x={(250 / 800) * 100} y={(70 / 450) * 100} />
    <PositionedAnchor
      id="poly-python"
      x={(400 / 800) * 100}
      y={(50 / 450) * 100}
    />
    <PositionedAnchor
      id="poly-rust"
      x={(550 / 800) * 100}
      y={(70 / 450) * 100}
    />
    <PositionedAnchor
      id="poly-net"
      x={(700 / 800) * 100}
      y={(90 / 450) * 100}
    />
    <PositionedAnchor
      id="poly-gateway"
      x={(400 / 800) * 100}
      y={(236 / 450) * 100}
    />
    <ConnectorLine
      from="poly-java"
      to="poly-gateway"
      curve="bezier"
      tone="accent-cat"
      weight="thin"
      containerSelector={canvasSelector}
    />
    <ConnectorLine
      from="poly-go"
      to="poly-gateway"
      curve="bezier"
      tone="var(--cc-col-bil)"
      weight="thin"
      containerSelector={canvasSelector}
    />
    <ConnectorLine
      from="poly-python"
      to="poly-gateway"
      curve="bezier"
      tone="var(--cc-col-ord)"
      weight="thin"
      containerSelector={canvasSelector}
    />
    <ConnectorLine
      from="poly-rust"
      to="poly-gateway"
      curve="bezier"
      tone="accent-shi"
      weight="thin"
      containerSelector={canvasSelector}
    />
    <ConnectorLine
      from="poly-net"
      to="poly-gateway"
      curve="bezier"
      tone="accent-usr"
      weight="thin"
      containerSelector={canvasSelector}
    />
  </div>
);

const FederationOverlay: FC<{ readonly canvasSelector: string }> = ({
  canvasSelector,
}) => (
  <div
    className="cc-sl-cin-diagram-overlay"
    data-cc-connector-layer="federation"
  >
    <PositionedAnchor
      id="fed-identity"
      x={(140 / 800) * 100}
      y={(90 / 450) * 100}
    />
    <PositionedAnchor
      id="fed-orders"
      x={(300 / 800) * 100}
      y={(70 / 450) * 100}
    />
    <PositionedAnchor
      id="fed-catalog"
      x={(500 / 800) * 100}
      y={(70 / 450) * 100}
    />
    <PositionedAnchor
      id="fed-support"
      x={(660 / 800) * 100}
      y={(90 / 450) * 100}
    />
    <PositionedAnchor
      id="fed-gateway"
      x={(400 / 800) * 100}
      y={(236 / 450) * 100}
    />
    <ConnectorLine
      from="fed-identity"
      to="fed-gateway"
      curve="bezier"
      tone="accent-shi"
      weight="thin"
      containerSelector={canvasSelector}
    />
    <ConnectorLine
      from="fed-orders"
      to="fed-gateway"
      curve="bezier"
      tone="accent-usr"
      weight="thin"
      containerSelector={canvasSelector}
    />
    <ConnectorLine
      from="fed-catalog"
      to="fed-gateway"
      curve="bezier"
      tone="accent-cat"
      weight="thin"
      containerSelector={canvasSelector}
    />
    <ConnectorLine
      from="fed-support"
      to="fed-gateway"
      curve="bezier"
      tone="var(--cc-col-bil)"
      weight="thin"
      containerSelector={canvasSelector}
    />
  </div>
);

const SingleGraphOverlay: FC<{ readonly canvasSelector: string }> = ({
  canvasSelector,
}) => (
  <div
    className="cc-sl-cin-diagram-overlay"
    data-cc-connector-layer="single-graph"
  >
    {/* one service center at (400, 231); three client nodes at y=376 */}
    <PositionedAnchor
      id="sg-service"
      x={(400 / 800) * 100}
      y={(231 / 450) * 100}
    />
    <PositionedAnchor id="sg-web" x={(180 / 800) * 100} y={(376 / 450) * 100} />
    <PositionedAnchor
      id="sg-mobile"
      x={(400 / 800) * 100}
      y={(376 / 450) * 100}
    />
    <PositionedAnchor
      id="sg-agents"
      x={(620 / 800) * 100}
      y={(376 / 450) * 100}
    />
    <ConnectorLine
      from="sg-service"
      to="sg-web"
      curve="bezier"
      tone="accent-shi"
      weight="thin"
      containerSelector={canvasSelector}
    />
    <ConnectorLine
      from="sg-service"
      to="sg-mobile"
      curve="bezier"
      tone="accent-cat"
      weight="thin"
      containerSelector={canvasSelector}
    />
    <ConnectorLine
      from="sg-service"
      to="sg-agents"
      curve="bezier"
      tone="accent-usr"
      weight="thin"
      containerSelector={canvasSelector}
    />
  </div>
);

const AgentsOverlay: FC<{ readonly canvasSelector: string }> = ({
  canvasSelector,
}) => (
  <div className="cc-sl-cin-diagram-overlay" data-cc-connector-layer="agents">
    {/* MCP center at (400, 212), four agents at y=376 */}
    <PositionedAnchor id="ag-mcp" x={(400 / 800) * 100} y={(212 / 450) * 100} />
    <PositionedAnchor
      id="ag-cursor"
      x={(140 / 800) * 100}
      y={(376 / 450) * 100}
    />
    <PositionedAnchor
      id="ag-claude"
      x={(313 / 800) * 100}
      y={(376 / 450) * 100}
    />
    <PositionedAnchor
      id="ag-copilot"
      x={(487 / 800) * 100}
      y={(376 / 450) * 100}
    />
    <PositionedAnchor
      id="ag-inhouse"
      x={(660 / 800) * 100}
      y={(376 / 450) * 100}
    />
    <ConnectorLine
      from="ag-mcp"
      to="ag-cursor"
      curve="bezier"
      tone="var(--cc-amber)"
      weight="thin"
      containerSelector={canvasSelector}
    />
    <ConnectorLine
      from="ag-mcp"
      to="ag-claude"
      curve="bezier"
      tone="var(--cc-amber)"
      weight="thin"
      containerSelector={canvasSelector}
    />
    <ConnectorLine
      from="ag-mcp"
      to="ag-copilot"
      curve="bezier"
      tone="var(--cc-amber)"
      weight="thin"
      containerSelector={canvasSelector}
    />
    <ConnectorLine
      from="ag-mcp"
      to="ag-inhouse"
      curve="bezier"
      tone="var(--cc-amber)"
      weight="thin"
      containerSelector={canvasSelector}
    />
  </div>
);

const EventBusOverlay: FC<{ readonly canvasSelector: string }> = ({
  canvasSelector,
}) => (
  <div
    className="cc-sl-cin-diagram-overlay"
    data-cc-connector-layer="event-bus"
  >
    {/* producers at x=130, bus left edge at x=310, consumers at x=670 */}
    <PositionedAnchor
      id="eb-orders"
      x={(130 / 800) * 100}
      y={(104 / 450) * 100}
    />
    <PositionedAnchor
      id="eb-billing"
      x={(130 / 800) * 100}
      y={(194 / 450) * 100}
    />
    <PositionedAnchor
      id="eb-inventory"
      x={(130 / 800) * 100}
      y={(284 / 450) * 100}
    />
    <PositionedAnchor
      id="eb-bus-in"
      x={(310 / 800) * 100}
      y={(208 / 450) * 100}
    />
    <PositionedAnchor
      id="eb-bus-out"
      x={(490 / 800) * 100}
      y={(208 / 450) * 100}
    />
    <PositionedAnchor
      id="eb-webhooks"
      x={(670 / 800) * 100}
      y={(104 / 450) * 100}
    />
    <PositionedAnchor
      id="eb-analytics"
      x={(670 / 800) * 100}
      y={(194 / 450) * 100}
    />
    <PositionedAnchor
      id="eb-audit"
      x={(670 / 800) * 100}
      y={(284 / 450) * 100}
    />
    <ConnectorLine
      from="eb-orders"
      to="eb-bus-in"
      curve="bezier"
      tone="var(--cc-amber)"
      weight="thin"
      containerSelector={canvasSelector}
    />
    <ConnectorLine
      from="eb-billing"
      to="eb-bus-in"
      curve="bezier"
      tone="var(--cc-amber)"
      weight="thin"
      containerSelector={canvasSelector}
    />
    <ConnectorLine
      from="eb-inventory"
      to="eb-bus-in"
      curve="bezier"
      tone="var(--cc-amber)"
      weight="thin"
      containerSelector={canvasSelector}
    />
    <ConnectorLine
      from="eb-bus-out"
      to="eb-webhooks"
      curve="bezier"
      tone="accent-shi"
      weight="thin"
      containerSelector={canvasSelector}
    />
    <ConnectorLine
      from="eb-bus-out"
      to="eb-analytics"
      curve="bezier"
      tone="accent-shi"
      weight="thin"
      containerSelector={canvasSelector}
    />
    <ConnectorLine
      from="eb-bus-out"
      to="eb-audit"
      curve="bezier"
      tone="accent-shi"
      weight="thin"
      containerSelector={canvasSelector}
    />
  </div>
);

const ComplianceOverlay: FC<{ readonly canvasSelector: string }> = ({
  canvasSelector,
}) => (
  <div
    className="cc-sl-cin-diagram-overlay"
    data-cc-connector-layer="compliance"
  >
    {/* three subgraphs at x=163; gateway center at (520, 224); mocha at x=700 */}
    <PositionedAnchor
      id="co-sub-a"
      x={(163 / 800) * 100}
      y={(136 / 450) * 100}
    />
    <PositionedAnchor
      id="co-sub-b"
      x={(163 / 800) * 100}
      y={(214 / 450) * 100}
    />
    <PositionedAnchor
      id="co-sub-c"
      x={(163 / 800) * 100}
      y={(292 / 450) * 100}
    />
    <PositionedAnchor
      id="co-gateway"
      x={(520 / 800) * 100}
      y={(224 / 450) * 100}
    />
    <PositionedAnchor
      id="co-mocha"
      x={(700 / 800) * 100}
      y={(224 / 450) * 100}
    />
    <ConnectorLine
      from="co-sub-a"
      to="co-gateway"
      curve="bezier"
      tone="accent-shi"
      weight="thin"
      containerSelector={canvasSelector}
    />
    <ConnectorLine
      from="co-sub-b"
      to="co-gateway"
      curve="bezier"
      tone="accent-shi"
      weight="thin"
      containerSelector={canvasSelector}
    />
    <ConnectorLine
      from="co-sub-c"
      to="co-gateway"
      curve="bezier"
      tone="accent-shi"
      weight="thin"
      containerSelector={canvasSelector}
    />
    <ConnectorLine
      from="co-gateway"
      to="co-mocha"
      curve="straight"
      tone="var(--cc-amber)"
      weight="thin"
      containerSelector={canvasSelector}
    />
  </div>
);

const OVERLAYS: Record<DiagramKind, FC<{ readonly canvasSelector: string }>> = {
  polyglot: PolyglotOverlay,
  federation: FederationOverlay,
  "single-graph": SingleGraphOverlay,
  agents: AgentsOverlay,
  "event-bus": EventBusOverlay,
  compliance: ComplianceOverlay,
};

// ============================================================
// Cinematic diagram variant. Reuses the default ConceptDiagram SVG body
// via `ConceptDiagramBody`, then layers per-variant anchors and Bezier
// connector lines on top, with a prism chip row above the heading as a
// "what's in the supergraph" exhibit.
// ============================================================
export const ConceptDiagramCinematic: FC<
  ConceptDiagramCinematicProps & { readonly stepNumber: string }
> = ({ kind, stepNumber }) => {
  const Overlay = OVERLAYS[kind];
  // Unique selector per instance so multiple diagrams on one page do not
  // collide. Slug pages only render one cinematic diagram, but the
  // selector keeps the connector layer self-contained.
  const layerId = `cc-sl-cin-canvas-${kind}`;

  return (
    <Band variant="inverted" ariaLabel="Architecture" className="cc-band">
      <ActLabel n={stepNumber} name="Architecture" />
      <div className="cc-sl-section cc-sl-diagram">
        <div className="cc-sl-cin-language-row">
          <TerminalChipRow
            accent="prism"
            chips={CHIP_ROWS[kind] as string[]}
            align="center"
          />
        </div>
        <div className="cc-sl-diagram-head">
          <div className="eyebrow">The shape</div>
          <h3>{DIAGRAM_TITLES[kind]}</h3>
        </div>
        <div
          className="cc-sl-diagram-canvas"
          id={layerId}
          data-cc-connector-layer={kind}
        >
          <ConceptDiagramBody kind={kind} />
          <Overlay canvasSelector={`#${layerId}`} />
        </div>
      </div>
    </Band>
  );
};
