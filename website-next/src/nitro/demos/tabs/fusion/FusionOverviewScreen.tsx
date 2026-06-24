/**
 * FusionOverviewScreen — Fusion reel Scene 1: the GATEWAY OVERVIEW for the EShops federated
 * gateway (Production stage). It clones the real "Gateway Overview" surface (see
 * src/demos/monitoring/image.png): a top half with a TOPOLOGY graph + a DETAILS panel, and a
 * bottom half with the SUBGRAPHS tile + the DEPLOYMENTS widget. Everything is present from the
 * first frame (no slow assembly):
 *
 *  ROW 1
 *   - TOPOLOGY tile (left): a client → Gateway hub → 4 subgraph node graph. The Gateway hub is a
 *     teal-green badge; the four subgraph nodes (Products, Reviews, Orders, Accounts) each carry a
 *     PINK subgraph icon. Curved SVG edges connect them.
 *   - DETAILS tile (right): a 3-up KPI strip (Latency / Throughput / Error Rate) over a
 *     label/value table (API ID with copy glyph, Stage, Version, Last published, Clients, Subgraphs).
 *
 *  ROW 2
 *   - SUBGRAPHS tile (left): the four source schemas — each a row with a green health dot, a PINK
 *     subgraph icon, name, version and a small metric.
 *   - DEPLOYMENTS widget (right): recent deployments, newest at top. The top row is a freshly landed
 *     deployment — "Reviews v2.4.0 · deployed · 2 minutes ago" — with a green "deployed" badge and a
 *     subtle highlight. It is the scripted click target (`data-testid="fusion-new-deployment"`).
 *
 * Beat (a developer scanning the gateway after a deploy): the cursor rests neutrally while the
 * viewer reads the overview, then makes one long, slow travel down to the deployments widget, rests
 * on and clicks the NEW Reviews v2.4.0 row (hand cursor), then holds. The cursor does NOT visit the
 * subgraphs. The final frame is the resolved end state so reduced-motion (frozen at progress=1)
 * reads correctly. All motion derives from `progress` via useTransform (no internal clocks); the
 * windows come from a STAGE-BASED timeline (see TL / OVERVIEW_MS below) whose total is DERIVED.
 */
import { motion, useTransform, type MotionValue } from "motion/react";
import { Stage } from "../../../primitives/reel/Stage";
import { AppFrame } from "../../../primitives/reel/AppFrame";
import { Cursor } from "../../../primitives/reel/Cursor";
import { GatewayChrome } from "../../../primitives/reel/GatewayChrome";
import { TABREEL_CANVAS } from "../../../primitives/reel/TabReel";
import { token } from "../../../lib/tokens";
import { ease } from "../../../lib/motion";
import { timeline } from "../../../lib/timeline";
import {
  IconCheck,
  IconApiGateway,
  IconChevronDown,
} from "../../../primitives/icons";

const W = TABREEL_CANVAS.w;
const H = TABREEL_CANVAS.h;

/* ── data ─────────────────────────────────────────────────────────────────── */

interface Subgraph {
  name: string;
  version: string;
  metricLabel: string;
  metricValue: string;
  updated?: boolean;
}
const SUBGRAPHS: Subgraph[] = [
  {
    name: "Products",
    version: "v3.1.0",
    metricLabel: "opm",
    metricValue: "9.2K",
  },
  {
    name: "Reviews",
    version: "v2.4.0",
    metricLabel: "opm",
    metricValue: "3.1K",
    updated: true,
  },
  {
    name: "Orders",
    version: "v2.7.4",
    metricLabel: "opm",
    metricValue: "4.6K",
  },
  {
    name: "Accounts",
    version: "v1.9.2",
    metricLabel: "opm",
    metricValue: "1.5K",
  },
];

interface Deployment {
  title: string;
  tag: string;
  target: string; // subgraph or client name
  when: string;
  isNew?: boolean;
}
const DEPLOYMENTS: Deployment[] = [
  {
    title: "Gateway Configuration Deployment succeeded",
    tag: "v2.4.0",
    target: "Reviews",
    when: "2 minutes ago",
    isNew: true,
  },
  {
    title: "Gateway Configuration Deployment succeeded",
    tag: "v2.3.1",
    target: "Orders",
    when: "3 hours ago",
  },
  {
    title: "Gateway Configuration Deployment succeeded",
    tag: "v2.3.0",
    target: "Products",
    when: "yesterday",
  },
];

interface Kpi {
  label: string;
  value: string;
  unit: string;
  dir: "up" | "down";
  good: boolean;
}
const KPIS: Kpi[] = [
  { label: "Latency", value: "42", unit: "ms", dir: "down", good: true },
  { label: "Throughput", value: "18.4K", unit: "opm", dir: "up", good: true },
  { label: "Error Rate", value: "0.12", unit: "%", dir: "down", good: true },
];

interface DetailRow {
  label: string;
  value: string;
  mono?: boolean;
  copy?: boolean;
}
const DETAILS: DetailRow[] = [
  { label: "API ID", value: "eshops-gateway-7f3a9c", mono: true, copy: true },
  { label: "Stage", value: "Production" },
  { label: "Version", value: "v2.4.0", mono: true },
  { label: "Last published on", value: "Jun 20, 2026, 10:18 AM" },
  { label: "Clients (registered)", value: "6" },
  { label: "Subgraphs", value: "4" },
];

/* ── geometry / timeline ──────────────────────────────────────────────────── */

const RAIL = 50;
const HEADER_H = 74; // GatewayChrome: 38 doc-tabs + 36 view-nav
const CRUMB_H = 28;
const FOOTER_H = 23;
const PAD = 16;
const GAP = 16;

// content area below header + breadcrumb, inside the rail.
const CONTENT_LEFT = RAIL + PAD;
const CONTENT_TOP = HEADER_H + CRUMB_H + PAD;
const CONTENT_H = H - FOOTER_H - HEADER_H - CRUMB_H - PAD * 2; // ~775
const ROW1_H = 336;
const ROW2_H = CONTENT_H - ROW1_H - GAP; // remaining for row 2

const LEFT_W = 720; // shared left-column width (topology + subgraphs)
const RIGHT_X = CONTENT_LEFT + LEFT_W + GAP; // left edge of the right column

const TILE_HEAD = 38;

// ── ROW 2 row geometry (for the cursor scan / click) ──
const ROW2_TOP = CONTENT_TOP + ROW1_H + GAP;
const SG_ROW_H = 58;

const DEP_ROW_H = 70;
const depRowY = (i: number) =>
  ROW2_TOP + TILE_HEAD + i * DEP_ROW_H + DEP_ROW_H / 2;
const DEP_ROW_CX = RIGHT_X + 320; // a point well inside the deployments tile

/* ── STAGE-BASED timeline ─────────────────────────────────────────────────────
 * Each interaction is a NAMED stage with its OWN ms; the screen's total duration is
 * DERIVED (OVERVIEW_MS = TL.total). Flow: establish/read (hold) → ONE slow cursor
 * move down to the new "Reviews v2.4.0" deployment row → hover → click → short dwell.
 * The cursor does NOT visit the subgraphs.
 *  - read:             hold while the viewer reads the overview
 *  - moveToDeployment: ONE generous, slow glide down to the new deployment row
 *  - hover:            dwell on the row (hand cursor)
 *  - click:            the scripted click pulse (snappy)
 *  - settle:           short hold after the click (resolved end state for reduced-motion)
 */
const TL = timeline([
  { name: "read", ms: 1200 },
  { name: "moveToDeployment", ms: 1700 },
  { name: "hover", ms: 900 },
  { name: "click", ms: 400 },
  { name: "settle", ms: 1000 },
]);

export const OVERVIEW_MS = TL.total;
export const OVERVIEW_TL = TL;

// scripted click: the NEW Reviews v2.4.0 deployment row.
const CLICK = TL.start("click");

export interface FusionOverviewScreenProps {
  progress: MotionValue<number>;
  active?: boolean;
  /** when false (sequenced reel), the per-scene cursor is suppressed — the reel hosts one shared cursor */
  showCursor?: boolean;
}

export function FusionOverviewScreen({
  progress,
  showCursor = true,
}: FusionOverviewScreenProps) {
  // STAGE-BASED, deliberate path (no detour to subgraphs):
  //  read              rest neutrally above the deployments area while the viewer reads
  //  moveToDeployment  ONE long, slow glide down to the new Reviews v2.4.0 deployment row
  //  hover             rest / hover on the row (hand cursor)
  //  click             the scripted click
  //  settle            hold after the click (resolved end state for reduced-motion)
  // The cursor holds its resting spot through `read`, then travels only during
  // `moveToDeployment`, and stays on the row from `hover` onward.
  const cx = useTransform(
    progress,
    [TL.start("moveToDeployment"), TL.end("moveToDeployment")],
    [DEP_ROW_CX - 40, DEP_ROW_CX],
    { ease: ease.glide },
  );
  const cy = useTransform(
    progress,
    [TL.start("moveToDeployment"), TL.end("moveToDeployment")],
    [depRowY(0) - 150, depRowY(0)],
    { ease: ease.glide },
  );

  return (
    <Stage
      width={W}
      height={H}
      fit="fill"
      chrome={false}
      ariaLabel="Nitro Fusion — the EShops gateway overview: topology, details, subgraphs and the newly deployed Reviews v2.4.0"
      overlay={
        showCursor ? (
          <Cursor
            x={cx}
            y={cy}
            progress={progress}
            clickTimes={[TL.start("click")]}
            pointerWindows={[[TL.start("hover"), TL.end("click")]]}
          />
        ) : null
      }
    >
      <AppFrame railActive="documents">
        <div
          style={{
            position: "absolute",
            inset: 0,
            display: "flex",
            flexDirection: "column",
          }}
        >
          <GatewayChrome activeView="Overview" />
          {/* breadcrumb / stage toolbar */}
          <div
            style={{
              height: CRUMB_H,
              flex: "0 0 auto",
              display: "flex",
              alignItems: "center",
              gap: 8,
              padding: "0 16px",
              borderBottom: `1px solid ${token.border}`,
            }}
          >
            <IconApiGateway size={13} color={token.icObject} />
            <span
              style={{ fontSize: 13, fontWeight: 600, color: token.textStrong }}
            >
              Production
            </span>
            <span style={{ fontSize: 13, color: token.textSecondary }}>
              Stage
            </span>
          </div>
          {/* content */}
          <div
            style={{
              flex: 1,
              minHeight: 0,
              display: "flex",
              flexDirection: "column",
              gap: GAP,
              padding: PAD,
              background: token.bg,
            }}
          >
            {/* ROW 1: topology + details */}
            <div
              style={{
                flex: `0 0 ${ROW1_H}px`,
                minHeight: 0,
                display: "flex",
                gap: GAP,
              }}
            >
              <TopologyTile />
              <DetailsTile />
            </div>
            {/* ROW 2: subgraphs + deployments */}
            <div
              style={{
                flex: `0 0 ${ROW2_H}px`,
                minHeight: 0,
                display: "flex",
                gap: GAP,
              }}
            >
              <SubgraphsTile />
              <DeploymentsTile progress={progress} />
            </div>
          </div>
        </div>
      </AppFrame>
    </Stage>
  );
}

/* ── shared tile shell ────────────────────────────────────────────────────── */

function Tile({
  title,
  count,
  children,
  flex,
}: {
  title: string;
  count?: string;
  children: React.ReactNode;
  flex: string;
}) {
  return (
    <div
      style={{
        flex,
        minWidth: 0,
        display: "flex",
        flexDirection: "column",
        background: token.card,
        border: `1px solid ${token.borderStrong}`,
        borderRadius: 8,
        overflow: "hidden",
      }}
    >
      <div
        style={{
          height: TILE_HEAD,
          flex: "0 0 auto",
          display: "flex",
          alignItems: "center",
          gap: 8,
          padding: "0 14px",
          borderBottom: `1px solid ${token.border}`,
        }}
      >
        <span
          style={{ fontSize: 13.5, fontWeight: 600, color: token.textStrong }}
        >
          {title}
        </span>
        {count && (
          <span
            style={{
              fontSize: 11.5,
              fontWeight: 600,
              color: token.textSecondary,
              background: token.surface,
              border: `1px solid ${token.border}`,
              borderRadius: 10,
              padding: "1px 8px",
            }}
          >
            {count}
          </span>
        )}
      </div>
      <div style={{ flex: 1, minHeight: 0, overflow: "hidden" }}>
        {children}
      </div>
    </div>
  );
}

/** PINK subgraph glyph — used everywhere a subgraph is represented. */
function SubgraphIcon({ size = 16 }: { size?: number }) {
  return <IconApiGateway size={size} color={token.pink} />;
}

/* ── ROW 1: TOPOLOGY tile ─────────────────────────────────────────────────── */

function TopologyTile() {
  // graph drawn in the tile body (minus head). The body is ROW1_H - TILE_HEAD tall.
  const VW = 860;
  const VH = ROW1_H - TILE_HEAD;
  const cx = VW * 0.5; // gateway hub
  const cy = VH * 0.5;
  const clientX = VW * 0.12;
  const clientY = cy;
  const sgX = VW * 0.84;
  const subs = SUBGRAPHS.map((s, i) => ({
    name: s.name,
    x: sgX,
    y: VH * (0.2 + (i * 0.6) / (SUBGRAPHS.length - 1)),
  }));

  return (
    <Tile title="Topology" flex={`0 0 ${LEFT_W}px`}>
      <div
        style={{
          position: "relative",
          width: "100%",
          height: "100%",
          background: token.graphCanvas,
        }}
      >
        <svg
          width="100%"
          height="100%"
          viewBox={`0 0 ${VW} ${VH}`}
          preserveAspectRatio="xMidYMid meet"
          style={{ position: "absolute", inset: 0 }}
        >
          <defs>
            <pattern
              id="topo-dots"
              width="22"
              height="22"
              patternUnits="userSpaceOnUse"
            >
              <circle
                cx="1.2"
                cy="1.2"
                r="1.2"
                fill={token.graphDots}
                opacity={0.35}
              />
            </pattern>
          </defs>
          <rect x="0" y="0" width={VW} height={VH} fill="url(#topo-dots)" />
          {/* client → gateway */}
          <path
            d={`M ${clientX + 34} ${clientY} C ${(clientX + cx) / 2} ${clientY}, ${(clientX + cx) / 2} ${cy}, ${cx - 44} ${cy}`}
            fill="none"
            stroke={token.graphEdge}
            strokeWidth={1.6}
          />
          {/* gateway → each subgraph */}
          {subs.map((s) => (
            <path
              key={s.name}
              d={`M ${cx + 44} ${cy} C ${(cx + s.x) / 2} ${cy}, ${(cx + s.x) / 2} ${s.y}, ${s.x - 30} ${s.y}`}
              fill="none"
              stroke={token.graphEdge}
              strokeWidth={1.6}
            >
              <animate
                attributeName="stroke-dashoffset"
                values="24;0"
                dur="2.6s"
                repeatCount="indefinite"
              />
              <set attributeName="stroke-dasharray" to="3 5" />
            </path>
          ))}
        </svg>

        {/* overlay nodes (HTML for crisp text + icons) */}
        <NodeBox
          xPct={clientX / VW}
          yPct={clientY / VH}
          label="eshops-web"
          sub="client"
        >
          <span
            style={{
              width: 9,
              height: 9,
              borderRadius: "50%",
              background: token.textSecondary,
              flex: "0 0 auto",
            }}
          />
        </NodeBox>

        <div
          style={{
            position: "absolute",
            left: `${(cx / VW) * 100}%`,
            top: `${(cy / VH) * 100}%`,
            transform: "translate(-50%, -50%)",
          }}
        >
          <div
            style={{
              display: "flex",
              flexDirection: "column",
              alignItems: "center",
              gap: 6,
            }}
          >
            <div
              style={{
                width: 56,
                height: 56,
                borderRadius: 14,
                background: token.graphNode,
                border: `1.5px solid ${token.chLatency}`,
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
                boxShadow: `0 0 0 4px rgba(59,206,172,0.12)`,
              }}
            >
              <IconApiGateway size={28} color={token.chLatency} />
            </div>
            <span
              style={{
                fontSize: 12.5,
                fontWeight: 600,
                color: token.textStrong,
              }}
            >
              Gateway
            </span>
          </div>
        </div>

        {subs.map((s) => (
          <NodeBox
            key={s.name}
            xPct={s.x / VW}
            yPct={s.y / VH}
            label={s.name}
            sub="subgraph"
          >
            <SubgraphIcon size={15} />
          </NodeBox>
        ))}
      </div>
    </Tile>
  );
}

function NodeBox({
  xPct,
  yPct,
  label,
  sub,
  children,
}: {
  xPct: number;
  yPct: number;
  label: string;
  sub: string;
  children: React.ReactNode;
}) {
  return (
    <div
      style={{
        position: "absolute",
        left: `${xPct * 100}%`,
        top: `${yPct * 100}%`,
        transform: "translate(-50%, -50%)",
      }}
    >
      <div
        style={{
          display: "flex",
          alignItems: "center",
          gap: 7,
          padding: "6px 10px",
          background: token.graphNode,
          border: `1px solid ${token.borderStrong}`,
          borderRadius: 8,
        }}
      >
        {children}
        <div
          style={{ display: "flex", flexDirection: "column", lineHeight: 1.1 }}
        >
          <span
            style={{ fontSize: 11.5, fontWeight: 600, color: token.textStrong }}
          >
            {label}
          </span>
          <span style={{ fontSize: 9.5, color: token.textSecondary }}>
            {sub}
          </span>
        </div>
      </div>
    </div>
  );
}

/* ── ROW 1: DETAILS tile ──────────────────────────────────────────────────── */

function DetailsTile() {
  return (
    <Tile title="Details" flex="1">
      <div
        style={{
          display: "flex",
          flexDirection: "column",
          gap: 14,
          padding: 14,
          height: "100%",
        }}
      >
        {/* 3-up KPI strip in a bordered inner card */}
        <div
          style={{
            flex: "0 0 auto",
            display: "flex",
            background: token.surface,
            border: `1px solid ${token.border}`,
            borderRadius: 8,
          }}
        >
          {KPIS.map((k, i) => (
            <div
              key={k.label}
              style={{
                flex: 1,
                padding: "10px 12px",
                borderLeft: i ? `1px solid ${token.border}` : "none",
              }}
            >
              <div
                style={{
                  fontSize: 10.5,
                  color: token.textSecondary,
                  textTransform: "uppercase",
                  letterSpacing: 0.3,
                }}
              >
                {k.label}
              </div>
              <div
                style={{
                  display: "flex",
                  alignItems: "baseline",
                  gap: 4,
                  marginTop: 3,
                }}
              >
                <span
                  style={{
                    fontSize: 18,
                    fontWeight: 700,
                    fontFamily: token.mono,
                    color: token.textStrong,
                  }}
                >
                  {k.value}
                </span>
                <span style={{ fontSize: 10.5, color: token.textSecondary }}>
                  {k.unit}
                </span>
                <Sentiment dir={k.dir} good={k.good} />
              </div>
            </div>
          ))}
        </div>
        {/* label / value table */}
        <div style={{ display: "flex", flexDirection: "column" }}>
          {DETAILS.map((d, i) => (
            <div
              key={d.label}
              style={{
                display: "flex",
                alignItems: "center",
                justifyContent: "space-between",
                gap: 12,
                padding: "7px 2px",
                borderTop: i ? `1px solid ${token.border}` : "none",
              }}
            >
              <span
                style={{
                  fontSize: 12,
                  color: token.textSecondary,
                  flex: "0 0 auto",
                }}
              >
                {d.label}
              </span>
              <span
                style={{
                  display: "flex",
                  alignItems: "center",
                  gap: 6,
                  minWidth: 0,
                }}
              >
                <span
                  style={{
                    fontSize: 12,
                    fontWeight: 600,
                    fontFamily: d.mono ? token.mono : undefined,
                    color: token.textStrong,
                    whiteSpace: "nowrap",
                    overflow: "hidden",
                    textOverflow: "ellipsis",
                  }}
                >
                  {d.value}
                </span>
                {d.copy && <CopyIcon />}
              </span>
            </div>
          ))}
        </div>
      </div>
    </Tile>
  );
}

function Sentiment({ dir, good }: { dir: "up" | "down"; good: boolean }) {
  const color = good ? token.successText : token.errorText;
  return (
    <svg
      width={11}
      height={11}
      viewBox="0 0 16 16"
      fill="none"
      stroke={color}
      strokeWidth={2}
      style={{
        flex: "0 0 auto",
        transform: dir === "up" ? undefined : "rotate(180deg)",
      }}
    >
      <path
        d="M8 13V3M8 3l-4 4M8 3l4 4"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

function CopyIcon() {
  return (
    <svg
      width={13}
      height={13}
      viewBox="0 0 16 16"
      fill="none"
      stroke={token.textSecondary}
      strokeWidth={1.3}
      style={{ flex: "0 0 auto" }}
    >
      <rect x="5.5" y="5.5" width="8" height="8" rx="1.5" />
      <path
        d="M3 10.5H2.5A1.5 1.5 0 0 1 1 9V2.5A1.5 1.5 0 0 1 2.5 1H9a1.5 1.5 0 0 1 1.5 1.5V3"
        strokeLinecap="round"
      />
    </svg>
  );
}

/* ── ROW 2: SUBGRAPHS tile ────────────────────────────────────────────────── */

function HealthDot() {
  return (
    <span
      style={{
        width: 8,
        height: 8,
        borderRadius: "50%",
        background: token.accentHover,
        boxShadow: `0 0 0 3px rgba(30,217,148,0.16)`,
        flex: "0 0 auto",
      }}
    />
  );
}

function SubgraphsTile() {
  // The cursor does NOT visit the subgraphs — these rows are static (no hover tint).
  return (
    <Tile title="Subgraphs" count="4" flex={`0 0 ${LEFT_W}px`}>
      {SUBGRAPHS.map((s, i) => (
        <SubgraphRow key={s.name} s={s} i={i} />
      ))}
    </Tile>
  );
}

function SubgraphRow({ s, i }: { s: Subgraph; i: number }) {
  return (
    <div
      style={{
        display: "flex",
        alignItems: "center",
        gap: 12,
        height: SG_ROW_H,
        padding: "0 16px",
        borderTop: i ? `1px solid ${token.border}` : "none",
        background: "transparent",
      }}
    >
      <HealthDot />
      <span style={{ display: "flex" }}>
        <SubgraphIcon size={17} />
      </span>
      <div style={{ flex: 1, minWidth: 0 }}>
        <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
          <span
            style={{ fontSize: 14, fontWeight: 600, color: token.textStrong }}
          >
            {s.name}
          </span>
          <span
            style={{
              fontSize: 11.5,
              fontFamily: token.mono,
              color: token.textSecondary,
              background: token.surface,
              border: `1px solid ${token.border}`,
              borderRadius: 4,
              padding: "1px 6px",
            }}
          >
            {s.version}
          </span>
          {s.updated && (
            <span
              style={{
                fontSize: 10.5,
                fontWeight: 600,
                color: token.successText,
                display: "flex",
                alignItems: "center",
                gap: 3,
              }}
            >
              <IconCheck size={11} /> updated
            </span>
          )}
        </div>
        <div style={{ fontSize: 11, color: token.textSecondary, marginTop: 1 }}>
          Healthy · source schema
        </div>
      </div>
      <div style={{ textAlign: "right" }}>
        <div
          style={{
            fontSize: 14.5,
            fontWeight: 700,
            fontFamily: token.mono,
            color: token.textStrong,
          }}
        >
          {s.metricValue}
        </div>
        <div
          style={{
            fontSize: 10,
            color: token.textSecondary,
            textTransform: "uppercase",
            letterSpacing: 0.3,
          }}
        >
          {s.metricLabel}
        </div>
      </div>
    </div>
  );
}

/* ── ROW 2: DEPLOYMENTS widget ────────────────────────────────────────────── */

function DeploymentsTile({ progress }: { progress: MotionValue<number> }) {
  return (
    <Tile title="Deployments" flex="1">
      {DEPLOYMENTS.map((d, i) => (
        <DeploymentRow
          key={`${d.tag}-${d.target}`}
          d={d}
          i={i}
          progress={progress}
        />
      ))}
    </Tile>
  );
}

function DeploymentRow({
  d,
  i,
  progress,
}: {
  d: Deployment;
  i: number;
  progress: MotionValue<number>;
}) {
  const isNew = !!d.isNew;
  // The NEW row gets a hover tint as the cursor arrives (hover stage) and goes ACTIVE on click.
  const HOVER = TL.start("hover");
  const bg = useTransform(progress, (p): string => {
    if (!isNew) return "transparent";
    if (p >= CLICK) return token.highlight; // active
    if (p >= HOVER) return token.surface; // hover
    return "rgba(30,217,148,0.06)"; // resting subtle highlight on the fresh row
  });
  const accentOpacity = useTransform(progress, (p): number =>
    isNew ? (p >= CLICK ? 1 : 0.5) : 0,
  );
  return (
    <motion.div
      data-testid={isNew ? "fusion-new-deployment" : undefined}
      style={{
        position: "relative",
        display: "flex",
        alignItems: "center",
        gap: 12,
        height: DEP_ROW_H,
        padding: "0 16px",
        borderTop: i ? `1px solid ${token.border}` : "none",
        background: bg,
      }}
    >
      {isNew && (
        <motion.span
          style={{
            position: "absolute",
            left: 0,
            top: 8,
            bottom: 8,
            width: 3,
            borderRadius: "0 3px 3px 0",
            background: token.accentHover,
            opacity: accentOpacity,
          }}
        />
      )}
      {/* status icon: green success check */}
      <span
        style={{
          flex: "0 0 auto",
          width: 22,
          height: 22,
          borderRadius: "50%",
          background: "rgba(52,157,135,0.18)",
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
        }}
      >
        <IconCheck size={14} color={token.successText} />
      </span>
      <div style={{ flex: 1, minWidth: 0 }}>
        {/* primary line: title + tag badge + subgraph badge + deployed badge */}
        <div
          style={{
            display: "flex",
            alignItems: "center",
            gap: 8,
            flexWrap: "nowrap",
          }}
        >
          <span
            style={{
              fontSize: 13,
              fontWeight: 600,
              color: token.textStrong,
              whiteSpace: "nowrap",
              overflow: "hidden",
              textOverflow: "ellipsis",
            }}
          >
            {d.title}
          </span>
          <TagBadge tag={d.tag} />
          <TargetBadge target={d.target} />
          {isNew && (
            <span
              style={{
                marginLeft: "auto",
                flex: "0 0 auto",
                display: "flex",
                alignItems: "center",
                gap: 4,
                fontSize: 10.5,
                fontWeight: 600,
                color: "#fff",
                background: token.success,
                borderRadius: 4,
                padding: "2px 7px",
              }}
            >
              <IconCheck size={11} color="#fff" /> deployed
            </span>
          )}
        </div>
        {/* secondary line */}
        <div
          style={{ fontSize: 11.5, color: token.textSecondary, marginTop: 3 }}
        >
          Started {d.when}
          {isNew && " · Approved 2 minutes ago by pascal"}
        </div>
      </div>
      {!isNew && (
        <IconChevronDown
          size={13}
          color={token.textDim}
          style={{ transform: "rotate(-90deg)", flex: "0 0 auto" }}
        />
      )}
    </motion.div>
  );
}

function TagBadge({ tag }: { tag: string }) {
  return (
    <span
      style={{
        flex: "0 0 auto",
        display: "flex",
        alignItems: "center",
        gap: 4,
        fontSize: 11,
        fontFamily: token.mono,
        color: token.text,
        background: token.surface,
        border: `1px solid ${token.border}`,
        borderRadius: 4,
        padding: "1px 6px",
      }}
    >
      <svg
        width={10}
        height={10}
        viewBox="0 0 16 16"
        fill="none"
        stroke={token.textSecondary}
        strokeWidth={1.4}
        style={{ flex: "0 0 auto" }}
      >
        <path d="M2.5 2.5h5l6 6-5 5-6-6z" />
        <circle cx="5" cy="5" r="1" fill={token.textSecondary} stroke="none" />
      </svg>
      {tag}
    </span>
  );
}

function TargetBadge({ target }: { target: string }) {
  return (
    <span
      style={{
        flex: "0 0 auto",
        display: "flex",
        alignItems: "center",
        gap: 4,
        fontSize: 11,
        color: token.text,
        background: token.surface,
        border: `1px solid ${token.border}`,
        borderRadius: 4,
        padding: "1px 6px",
      }}
    >
      <SubgraphIcon size={11} />
      {target}
    </span>
  );
}
