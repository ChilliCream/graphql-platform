import { motion, useTransform, type MotionValue } from "motion/react";
import { Stage } from "../../../primitives/reel/Stage";
import { AppFrame } from "../../../primitives/reel/AppFrame";
import { Cursor } from "../../../primitives/reel/Cursor";
import {
  GatewayChrome,
  GW_HEADER_H,
} from "../../../primitives/reel/GatewayChrome";
import { TABREEL_CANVAS } from "../../../primitives/reel/TabReel";
import { token } from "../../../lib/tokens";
import { ease } from "../../../lib/motion";
import { timeline } from "../../../lib/timeline";
import {
  IconCheck,
  IconSpinner,
  IconLink,
  IconApiGateway,
  IconHistory,
  IconChevronDown,
  IconField,
  IconObject,
} from "../../../primitives/icons";

const W = TABREEL_CANVAS.w;
const H = TABREEL_CANVAS.h;
const RAIL = 50;
const GREEN = token.success;
const GREEN_TEXT = token.successText;

interface Row {
  id: string;
  title: string;
  tag: string;
  subgraph?: string;
  approved?: boolean;
  started: string;
  by?: string;
  status: "success";
}
const ROWS: Row[] = [
  {
    id: "d0",
    title: "Gateway Configuration Deployment succeeded",
    tag: "v2.4.0",
    subgraph: "Reviews",
    approved: true,
    started: "2 minutes ago",
    by: "m.tanaka",
    status: "success",
  },
  {
    id: "d1",
    title: "Schema Deployment succeeded",
    tag: "v2.3.0",
    subgraph: "Reviews",
    started: "3 hours ago",
    status: "success",
  },
  {
    id: "d2",
    title: "Gateway Configuration Deployment succeeded",
    tag: "v2.3.0",
    started: "3 hours ago",
    status: "success",
  },
  {
    id: "d3",
    title: "Schema Deployment succeeded",
    tag: "v2.2.1",
    subgraph: "Orders",
    started: "yesterday",
    status: "success",
  },
  {
    id: "d4",
    title: "Gateway Configuration Deployment succeeded",
    tag: "v2.2.1",
    started: "yesterday",
    status: "success",
  },
  {
    id: "d5",
    title: "Schema Deployment succeeded",
    tag: "v2.2.0",
    subgraph: "Products",
    started: "2 days ago",
    status: "success",
  },
];
const SEQUENCE: [string, string][] = [
  ["Deployment Created", "Jun 20, 2026, 4:41:50 PM"],
  ["Deployment Started Processing", "Jun 20, 2026, 4:41:58 PM"],
  ["Deployment Waiting for Approval", "Jun 20, 2026, 4:42:05 PM"],
  ["Deployment Approved by m.tanaka", "Jun 20, 2026, 4:42:11 PM"],
  ["Deployment Succeeded", "Jun 20, 2026, 4:42:18 PM"],
];
const DIFF = `type Review {
  id: ID!
  rating: Int!
  body: String!
  helpfulVotes: Int!
  verifiedPurchase: Boolean!
}`;
const ADDED_LINES = new Set([4, 5]);

const LIST_W = 300;
const TOOLBAR_H = 36;
const HEADER_H = GW_HEADER_H + TOOLBAR_H;
const ENTRY_H = 68;
const listRowY = (i: number) => HEADER_H + 20 + 1 + i * ENTRY_H + ENTRY_H / 2;

const TL = timeline([
  { name: "load", ms: 500 },
  { name: "select", ms: 400 },
  { name: "tree", ms: 450 },
  { name: "diff", ms: 450 },
  { name: "read", ms: 1200 },
  { name: "safe", ms: 400 },
  { name: "moveToPlus", ms: 1500 },
  { name: "plusClick", ms: 300 },
]);

export const DEPLOYMENTS_MS = TL.total;
export const DEPLOYMENTS_TL = TL;

const PLUS_X = RAIL + 8 + 165 + 6 + 6 + 8;
const PLUS_Y = 19;
const ROW0_X = RAIL + 150;
const ROW0_Y = listRowY(0);

export interface FusionDeploymentsScreenProps {
  progress: MotionValue<number>;
  active?: boolean;
  showCursor?: boolean;
}

export function FusionDeploymentsScreen({
  progress,
  showCursor = true,
}: FusionDeploymentsScreenProps) {
  const cx = useTransform(
    progress,
    [0, TL.end("select"), TL.start("moveToPlus"), TL.start("plusClick"), 1],
    [ROW0_X + 120, ROW0_X, ROW0_X, PLUS_X, PLUS_X],
    { ease: ease.glide },
  );
  const cy = useTransform(
    progress,
    [0, TL.end("select"), TL.start("moveToPlus"), TL.start("plusClick"), 1],
    [ROW0_Y + 70, ROW0_Y, ROW0_Y, PLUS_Y, PLUS_Y],
    { ease: ease.glide },
  );

  return (
    <Stage
      width={W}
      height={H}
      fit="fill"
      chrome={false}
      ariaLabel="Nitro Fusion — a deployment's additive schema changes recompose the gateway, then starting a new query"
      overlay={
        showCursor ? (
          <Cursor
            x={cx}
            y={cy}
            progress={progress}
            clickTimes={[TL.start("plusClick")]}
            pointerWindows={[
              [TL.at("moveToPlus", 0.7), TL.at("plusClick", 0.3)],
            ]}
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
          <GatewayChrome activeView="Deployments" />
          <div
            style={{
              height: TOOLBAR_H,
              flex: "0 0 auto",
              display: "flex",
              alignItems: "center",
              gap: 8,
              padding: "0 14px",
              borderBottom: `1px solid ${token.border}`,
            }}
          >
            <IconHistory size={14} color={token.textSecondary} />
            <span
              style={{ fontSize: 13, fontWeight: 600, color: token.textStrong }}
            >
              Production Stage
            </span>
            <span
              style={{
                marginLeft: "auto",
                fontSize: 11.5,
                color: token.textSecondary,
              }}
            >
              {ROWS.length} deployments
            </span>
          </div>
          <div
            style={{ flex: 1, minHeight: 0, padding: 20, position: "relative" }}
          >
            <div
              style={{
                position: "absolute",
                inset: 20,
                borderRadius: 8,
                border: `1px solid ${token.borderStrong}`,
                background: token.card,
                overflow: "hidden",
                display: "flex",
              }}
            >
              <DeploymentList progress={progress} />
              <div
                style={{
                  flex: 1,
                  minWidth: 0,
                  borderLeft: `1px solid ${token.border}`,
                  position: "relative",
                  background: token.surface,
                }}
              >
                <DetailPane progress={progress} />
                <Spinner
                  progress={progress}
                  show={TL.at("load", 0.1)}
                  hide={TL.end("load")}
                />
              </div>
            </div>
          </div>
        </div>
      </AppFrame>
    </Stage>
  );
}

function DeploymentList({ progress }: { progress: MotionValue<number> }) {
  const fade = useTransform(
    progress,
    [TL.at("load", 0.8), TL.at("select", 0.3)],
    [0, 1],
    { clamp: true },
  );
  const topSlide = useTransform(
    progress,
    [TL.end("load"), TL.at("select", 0.6)],
    [-10, 0],
    { clamp: true },
  );
  const activeBg = useTransform(
    progress,
    [TL.at("select", 0.5), TL.end("select")],
    [0, 1],
    { clamp: true },
  );
  const activeRowBg = useTransform(
    progress,
    [TL.at("select", 0.5), TL.end("select")],
    ["rgba(35,134,54,0)", "rgba(35,134,54,0.09)"],
    { clamp: true },
  );
  return (
    <div
      style={{
        width: LIST_W,
        flex: "0 0 auto",
        display: "flex",
        flexDirection: "column",
        background: token.surface,
      }}
    >
      <motion.div style={{ opacity: fade, flex: 1, minHeight: 0 }}>
        {ROWS.map((r, i) => {
          const active = i === 0;
          return (
            <motion.div
              key={r.id}
              data-testid={active ? "deploy-row-active" : undefined}
              style={{
                position: "relative",
                display: "flex",
                gap: 16,
                padding: "12px 24px",
                borderBottom: `1px solid ${token.border}`,
                background: active ? activeRowBg : "transparent",
                y: active ? topSlide : 0,
              }}
            >
              <StatusIcon />
              <div
                style={{
                  flex: 1,
                  minWidth: 0,
                  display: "flex",
                  flexDirection: "column",
                  gap: 4,
                }}
              >
                <div
                  style={{
                    display: "flex",
                    alignItems: "center",
                    gap: 8,
                    flexWrap: "wrap",
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
                      maxWidth: 168,
                    }}
                  >
                    {r.title}
                  </span>
                  <TagBadge tag={r.tag} />
                  {r.subgraph && <SubgraphBadge name={r.subgraph} />}
                  {r.approved && <ApprovedBadge />}
                </div>
                <div style={{ fontSize: 11.5, color: token.textSecondary }}>
                  Started{" "}
                  <strong style={{ color: token.text, fontWeight: 600 }}>
                    {r.started}
                  </strong>
                  {r.approved && r.by && (
                    <>
                      {" "}
                      · Approved{" "}
                      <strong style={{ color: token.text, fontWeight: 600 }}>
                        1 minute ago
                      </strong>{" "}
                      by{" "}
                      <strong style={{ color: token.text, fontWeight: 600 }}>
                        {r.by}
                      </strong>
                    </>
                  )}
                </div>
              </div>
              {active && (
                <motion.div
                  style={{
                    position: "absolute",
                    left: 0,
                    top: 0,
                    bottom: 0,
                    width: 3,
                    background: GREEN,
                    opacity: activeBg,
                  }}
                />
              )}
            </motion.div>
          );
        })}
      </motion.div>
    </div>
  );
}

function StatusIcon() {
  return (
    <span
      style={{
        flex: "0 0 auto",
        width: 20,
        height: 20,
        borderRadius: "50%",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        background: "rgba(35,134,54,0.18)",
      }}
    >
      <IconCheck size={13} color={GREEN_TEXT} />
    </span>
  );
}

function TagBadge({ tag }: { tag: string }) {
  return (
    <span
      style={{
        display: "inline-flex",
        alignItems: "center",
        gap: 4,
        fontSize: 11,
        fontFamily: token.mono,
        color: token.text,
        border: `1px solid ${token.border}`,
        borderRadius: 4,
        padding: "1px 6px",
      }}
    >
      {tag}
    </span>
  );
}

function SubgraphBadge({ name }: { name: string }) {
  return (
    <span
      style={{
        display: "inline-flex",
        alignItems: "center",
        gap: 4,
        fontSize: 11,
        color: token.textSecondary,
      }}
    >
      <IconApiGateway size={11} color={token.icObject} /> {name}
    </span>
  );
}

function ApprovedBadge() {
  return (
    <span
      style={{
        marginLeft: "auto",
        display: "inline-flex",
        alignItems: "center",
        gap: 4,
        fontSize: 10.5,
        fontWeight: 600,
        color: GREEN_TEXT,
        border: `1px solid ${GREEN}`,
        borderRadius: 4,
        padding: "1px 6px",
      }}
    >
      <IconCheck size={10} color={GREEN_TEXT} /> Approved
    </span>
  );
}

function DetailPane({ progress }: { progress: MotionValue<number> }) {
  const fade = useTransform(
    progress,
    [TL.at("select", 0.4), TL.end("select")],
    [0, 1],
    { clamp: true },
  );
  return (
    <motion.div
      style={{
        position: "absolute",
        inset: 0,
        overflow: "hidden",
        padding: "14px 16px 16px",
        display: "flex",
        flexDirection: "column",
        gap: 16,
        opacity: fade,
      }}
    >
      <div style={{ display: "flex", alignItems: "flex-start" }}>
        <div style={{ flex: 1 }}>
          <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
            <span
              style={{
                width: 22,
                height: 22,
                borderRadius: "50%",
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
                background: "rgba(35,134,54,0.18)",
              }}
            >
              <IconCheck size={14} color={GREEN_TEXT} />
            </span>
            <span
              style={{ fontSize: 18, fontWeight: 700, color: token.textStrong }}
            >
              Deployment Succeeded
            </span>
          </div>
          <div
            style={{
              fontSize: 12,
              color: token.textSecondary,
              marginTop: 4,
              marginLeft: 30,
            }}
          >
            Jun 20, 2026, 4:42:18 PM
          </div>
        </div>
      </div>

      <Group label="Subgraph">
        <span
          style={{
            display: "inline-flex",
            alignItems: "center",
            gap: 5,
            fontSize: 13,
            color: token.blue,
          }}
        >
          <IconApiGateway size={13} color={token.icObject} /> Reviews{" "}
          <IconLink size={11} color={token.blue} />
        </span>
      </Group>

      <Group label="Changes">
        <SafeChip progress={progress} />
      </Group>

      <div
        style={{
          flex: 1,
          minHeight: 0,
          display: "flex",
          flexDirection: "column",
          gap: 12,
        }}
      >
        <ChangelogTree progress={progress} />
        <RawDiff progress={progress} />
      </div>

      <Sequence />
    </motion.div>
  );
}

function Group({
  label,
  children,
}: {
  label: string;
  children: React.ReactNode;
}) {
  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
      <div
        style={{
          fontSize: 11,
          fontWeight: 600,
          letterSpacing: 0.4,
          textTransform: "uppercase",
          color: token.textSecondary,
        }}
      >
        {label}
      </div>
      {children}
    </div>
  );
}

function SafeChip({ progress }: { progress: MotionValue<number> }) {
  const scale = useTransform(
    progress,
    [TL.start("safe"), TL.at("safe", 0.4), TL.end("safe")],
    [1, 1.12, 1],
    { clamp: true },
  );
  return (
    <motion.span
      data-testid="safe-chip"
      style={{
        scale,
        alignSelf: "flex-start",
        display: "inline-flex",
        alignItems: "center",
        gap: 6,
        fontSize: 12.5,
        fontWeight: 600,
        color: GREEN_TEXT,
        background: "rgba(35,134,54,0.14)",
        border: `1px solid ${GREEN}`,
        borderRadius: 5,
        padding: "3px 9px",
      }}
    >
      <IconCheck size={13} color={GREEN_TEXT} /> 2 safe
    </motion.span>
  );
}

function ChangelogTree({ progress }: { progress: MotionValue<number> }) {
  const chevRot = useTransform(
    progress,
    [TL.start("tree"), TL.at("tree", 0.35)],
    [-90, 0],
    { clamp: true },
  );
  const c0 = useTransform(
    progress,
    [TL.at("tree", 0.3), TL.at("tree", 0.7)],
    [0, 1],
    { clamp: true },
  );
  const c1 = useTransform(
    progress,
    [TL.at("tree", 0.6), TL.end("tree")],
    [0, 1],
    { clamp: true },
  );
  const children: [string, string, MotionValue<number>][] = [
    ["helpfulVotes", "Review.helpfulVotes: Int!", c0],
    ["verifiedPurchase", "Review.verifiedPurchase: Boolean!", c1],
  ];
  return (
    <div style={{ fontSize: 12.5 }}>
      <div
        style={{
          display: "flex",
          alignItems: "center",
          gap: 6,
          padding: "3px 0",
        }}
      >
        <motion.span
          style={{
            display: "flex",
            rotate: chevRot,
            color: token.textSecondary,
          }}
        >
          <IconChevronDown size={12} color="currentColor" />
        </motion.span>
        <IconObject size={13} color={token.icObject} />
        <Coordinate text="Review" />
        <span style={{ color: token.textSecondary }}>object type modified</span>
      </div>
      {children.map(([coord, full, op]) => (
        <motion.div
          key={coord}
          style={{
            opacity: op,
            display: "flex",
            alignItems: "center",
            gap: 6,
            padding: "3px 0 3px 26px",
          }}
        >
          <span
            style={{
              color: GREEN_TEXT,
              fontFamily: token.mono,
              fontWeight: 700,
              width: 10,
            }}
          >
            +
          </span>
          <IconField size={12} color={token.icField} />
          <Coordinate text={coord} green />
          <span style={{ color: token.textSecondary }}>field added</span>
          <span
            style={{
              marginLeft: 6,
              fontSize: 11.5,
              fontFamily: token.mono,
              color: token.textDim,
            }}
          >
            {full}
          </span>
        </motion.div>
      ))}
    </div>
  );
}

function Coordinate({ text, green }: { text: string; green?: boolean }) {
  return (
    <span
      style={{
        fontFamily: token.mono,
        fontWeight: 600,
        color: green ? GREEN_TEXT : token.blue,
        background: green ? "rgba(35,134,54,0.12)" : "rgba(88,166,255,0.12)",
        borderRadius: 3,
        padding: "0 4px",
      }}
    >
      {text}
    </span>
  );
}

function RawDiff({ progress }: { progress: MotionValue<number> }) {
  const reveal = useTransform(
    progress,
    [TL.start("diff"), TL.at("diff", 0.6)],
    [0, 1],
    { clamp: true },
  );
  const lines = DIFF.split("\n");
  return (
    <motion.div
      style={{
        opacity: reveal,
        borderRadius: 6,
        border: `1px solid ${token.border}`,
        overflow: "hidden",
        background: token.bg,
      }}
    >
      <div
        style={{
          height: 26,
          display: "flex",
          alignItems: "center",
          gap: 8,
          padding: "0 10px",
          borderBottom: `1px solid ${token.border}`,
          fontSize: 11,
          color: token.textSecondary,
        }}
      >
        <span style={{ fontFamily: token.mono }}>schema.graphql</span>
        <span style={{ marginLeft: "auto", fontSize: 11, color: GREEN_TEXT }}>
          +2 −0
        </span>
      </div>
      <div
        style={{
          fontFamily: token.mono,
          fontSize: 12.5,
          lineHeight: "20px",
          padding: "6px 0",
        }}
      >
        {lines.map((ln, i) => {
          const added = ADDED_LINES.has(i);
          return (
            <div
              key={i}
              style={{
                display: "flex",
                background: added ? "rgba(35,134,54,0.14)" : "transparent",
              }}
            >
              <span
                style={{
                  width: 22,
                  flex: "0 0 auto",
                  textAlign: "center",
                  color: added ? GREEN_TEXT : token.textDim,
                  userSelect: "none",
                }}
              >
                {added ? "+" : ""}
              </span>
              <span
                style={{
                  whiteSpace: "pre",
                  color: added ? GREEN_TEXT : token.text,
                }}
              >
                {ln}
              </span>
            </div>
          );
        })}
      </div>
    </motion.div>
  );
}

function Sequence() {
  return (
    <Group label="Sequence">
      <div style={{ display: "flex", flexDirection: "column" }}>
        {SEQUENCE.map(([msg, time], i) => {
          const last = i === SEQUENCE.length - 1;
          return (
            <div key={msg} style={{ display: "flex", gap: 10 }}>
              <div
                style={{
                  display: "flex",
                  flexDirection: "column",
                  alignItems: "center",
                  flex: "0 0 auto",
                }}
              >
                <span
                  style={{
                    width: 16,
                    height: 16,
                    borderRadius: "50%",
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "center",
                    background: last ? "rgba(35,134,54,0.18)" : "transparent",
                    border: last ? "none" : `1.5px solid ${token.borderStrong}`,
                  }}
                >
                  {last && <IconCheck size={11} color={GREEN_TEXT} />}
                </span>
                {!last && (
                  <span
                    style={{
                      flex: 1,
                      width: 1,
                      background: token.border,
                      minHeight: 14,
                    }}
                  />
                )}
              </div>
              <div style={{ paddingBottom: last ? 0 : 8 }}>
                <div style={{ fontSize: 12, color: token.textStrong }}>
                  {msg}
                </div>
                <div
                  style={{
                    fontSize: 11,
                    color: token.textSecondary,
                    fontFamily: token.mono,
                  }}
                >
                  {time}
                </div>
              </div>
            </div>
          );
        })}
      </div>
    </Group>
  );
}

function Spinner({
  progress,
  show,
  hide,
}: {
  progress: MotionValue<number>;
  show: number;
  hide: number;
}) {
  const fade = (hide - show) * 0.15;
  const opacity = useTransform(
    progress,
    [show, show + fade, hide - fade, hide],
    [0, 1, 1, 0],
    { clamp: true },
  );
  const rotate = useTransform(progress, [show, hide], [0, 720]);
  return (
    <motion.div
      style={{
        position: "absolute",
        inset: 0,
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        opacity,
        pointerEvents: "none",
        zIndex: 5,
        background: token.surface,
      }}
    >
      <motion.div style={{ rotate, display: "flex" }}>
        <IconSpinner size={26} color={token.accent} />
      </motion.div>
    </motion.div>
  );
}
