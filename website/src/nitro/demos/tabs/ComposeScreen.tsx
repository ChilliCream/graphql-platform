/**
 * ComposeScreen — Tab 1: "Write GraphQL at the speed of thought."
 *
 * STORY (one continuous developer action): the IDE is open on the GetOrder document. The LEFT
 * companion (a thin Builder/Lens icon-rail + the QUERY BUILDER — a checkbox field-tree of the
 * GetOrder operation) is visible alongside the Request|Response panes. The developer
 *   1. ticks the `customer` field in the query builder — its checkbox fills,
 *   2. a ready `customer { name email }` selection-set snaps into the Request editor,
 *   3. they hit Run,
 *   4. the federated order — Ada Lovelace, PROCESSING, $129.97 — streams back in 142 ms,
 * then we dwell on the finished build→run→data story. All motion derives from a STAGE-BASED
 * timeline (`src/lib/timeline.ts`): each beat owns its ms and the total (`COMPOSE_MS`) is derived.
 *
 * The operation-view lays out a thin companion icon-rail (Operation Builder = object-type icon,
 * Operation Lens = info icon) + a companion panel (the Operation Builder tree — each row a CHECKBOX
 * + field-type icon + `field: Type`) then a Request|Response splitter.
 */
import { useState } from "react";
import {
  motion,
  useMotionValueEvent,
  useTransform,
  type MotionValue,
} from "motion/react";
import { Stage } from "../../primitives/reel/Stage";
import { AppFrame } from "../../primitives/reel/AppFrame";
import { Cursor } from "../../primitives/reel/Cursor";
import { CodeBlock } from "../../primitives/CodeBlock";
import { TABREEL_CANVAS } from "../../primitives/reel/TabReel";
import { token } from "../../lib/tokens";
import { ease } from "../../lib/motion";
import { timeline } from "../../lib/timeline";
import { composeData as D } from "../../lib/data/tabs";
import {
  IconObject,
  IconScalar,
  IconEnum,
  IconQuery,
  IconMutation,
  IconSubscription,
  IconRun,
  IconSpinner,
  IconCheck,
  IconErrorCircle,
  IconChevronDown,
  IconChevronRight,
  IconFormat,
  IconPlus,
  IconSave,
  IconClose,
  IconSearch,
  IconInfo,
  IconField,
  type IconProps,
} from "../../primitives/icons";

const W = TABREEL_CANVAS.w;
const H = TABREEL_CANVAS.h;

// Nitro IDE active-accent (DocTabStrip / tab underline / tree selection) is orange, not the pink token.active.
const ORANGE = token.graphEdgeActive;

type Kind = "query" | "mutation" | "subscription";
const kindColor = (k: Kind) =>
  k === "mutation"
    ? token.icMutation
    : k === "subscription"
      ? token.icObject
      : token.icQuery;
function KindGlyph({ kind, size }: { kind: Kind; size: number }) {
  if (kind === "mutation") return <IconMutation size={size} />;
  if (kind === "subscription") return <IconSubscription size={size} />;
  return <IconQuery size={size} />;
}

/* ── STAGE-BASED timeline: each beat owns its ms; the total is DERIVED ──
 * Flow: establish → moveToBuilder → tickCustomer → insertBlock → moveToRun → runClick →
 *       runLoad → response → statusResolve → moveToLens → lensClick → lensReveal → lensDwell.
 * The cursor first glides to the `customer` checkbox in the query builder (left companion),
 * ticks it; the `customer { name email }` block snaps into the editor; then it moves to Run and
 * runs the operation. Finally it moves to the OPERATION LENS toggle in the companion icon-rail,
 * clicks it, and the left panel switches from the Operation Builder to the Operation Lens —
 * the schema-reference detail for the just-selected `customer: Customer!` field — and we dwell
 * on the lens as the focus.
 */
const TL = timeline([
  { name: "establish", ms: 700 }, // settle on the open GetOrder doc + query builder
  { name: "moveToBuilder", ms: 1400 }, // cursor glides to the `customer` checkbox in the builder
  { name: "tickCustomer", ms: 130 }, // click the checkbox (it fills immediately, no hover-wait)
  { name: "insertBlock", ms: 700 }, // `customer { name email }` snaps into the editor
  { name: "moveToRun", ms: 1300 }, // builder → Run split-button (slow glide)
  { name: "runClick", ms: 120 }, // click Run
  { name: "runLoad", ms: 450 }, // in-flight spinner (~142 ms feel)
  { name: "response", ms: 1400 }, // federated JSON streams into the Response pane
  { name: "statusResolve", ms: 550 }, // 200 · ms · size status + history check resolve
  { name: "moveToLens", ms: 1300 }, // cursor glides to the Operation Lens toggle (companion rail)
  { name: "lensClick", ms: 120 }, // click the Operation Lens toggle
  { name: "lensReveal", ms: 650 }, // left panel switches to the Operation Lens detail
  { name: "lensDwell", ms: 2000 }, // rest on the Operation Lens — the schema detail for `customer`
]);

/** DERIVED total duration in ms — feed to SoloScreen / the reel tab. */
export const COMPOSE_MS = TL.total;
export const COMPOSE_TL = TL;

export interface ComposeScreenProps {
  progress: MotionValue<number>;
  active?: boolean;
}

export function ComposeScreen({ progress }: ComposeScreenProps) {
  // run lifecycle — snappy ~140ms feel, not a drawn-out spin
  const runClick = TL.start("runClick");
  const runDone = TL.end("runLoad");
  const running = useTransform(progress, (p): number =>
    p >= runClick && p < runDone ? 1 : 0,
  );
  const statusOpacity = useTransform(
    progress,
    [TL.start("statusResolve"), TL.end("statusResolve")],
    [0, 1],
    { clamp: true },
  );
  const responseOpacity = useTransform(
    progress,
    [TL.start("response"), TL.at("response", 0.1)],
    [0, 1],
    { clamp: true },
  );

  // whether the `customer` field is checked in the builder — flips at the tick click
  const tick = TL.start("tickCustomer");
  const checkedAt = (p: number) => (p >= tick ? 1 : 0);
  const [custChecked, setCustChecked] = useState(() =>
    checkedAt(progress.get()),
  );
  useMotionValueEvent(progress, "change", (p) => setCustChecked(checkedAt(p)));

  // whether the left companion shows the Operation LENS (true) or the Operation BUILDER (false).
  // Flips at the lens-toggle click; the panel cross-fades over lensReveal.
  const lensClickP = TL.start("lensClick");
  const lensAt = (p: number) => (p >= lensClickP ? 1 : 0);
  const [lensOn, setLensOn] = useState(() => lensAt(progress.get()));
  useMotionValueEvent(progress, "change", (p) => setLensOn(lensAt(p)));

  // cursor path (canvas coords): establish → `customer` checkbox in the query builder companion →
  // (tick, block snaps in) → Run split-button (parked) → settle.
  // The builder companion sits left of Request: rail at x≈50+36, panel ≈ x86..366. The `customer`
  // checkbox row is the last top-level field in the builder → ~x 150, y 300.
  // Run split-button sits top-right of the Request column header.
  // Builder `customer` checkbox (MEASURED on-screen in canvas px so the cursor hand lands on it).
  const CUST_X = 396;
  const CUST_Y = 327;
  const RUN_X = 980;
  const RUN_Y = 64;
  // Operation Lens toggle — 2nd button in the companion icon-rail (MEASURED in canvas px).
  const LENS_X = 324;
  const LENS_Y = 102;
  const cx = useTransform(
    progress,
    [
      TL.start("establish"),
      TL.start("moveToBuilder"),
      TL.start("tickCustomer"),
      TL.end("insertBlock"),
      TL.start("moveToRun"),
      TL.start("runClick"),
      TL.start("moveToLens"),
      TL.start("lensClick"),
      TL.start("lensDwell"),
      1,
    ],
    [320, CUST_X, CUST_X, CUST_X, CUST_X, RUN_X, RUN_X, LENS_X, LENS_X, LENS_X],
    { ease: ease.glide },
  );
  const cy = useTransform(
    progress,
    [
      TL.start("establish"),
      TL.start("moveToBuilder"),
      TL.start("tickCustomer"),
      TL.end("insertBlock"),
      TL.start("moveToRun"),
      TL.start("runClick"),
      TL.start("moveToLens"),
      TL.start("lensClick"),
      TL.start("lensDwell"),
      1,
    ],
    [250, CUST_Y, CUST_Y, CUST_Y, CUST_Y, RUN_Y, RUN_Y, LENS_Y, LENS_Y, LENS_Y],
    { ease: ease.glide },
  );

  return (
    <Stage
      width={W}
      height={H}
      fit="fill"
      chrome={false}
      ariaLabel="Nitro — ticking a field in the query builder, then running the GraphQL query"
      overlay={
        <Cursor
          x={cx}
          y={cy}
          progress={progress}
          clickTimes={[
            TL.start("tickCustomer"),
            runClick,
            TL.start("lensClick"),
          ]}
          pointerWindows={[
            [TL.start("moveToBuilder"), TL.end("insertBlock")],
            [TL.start("moveToLens"), TL.end("lensReveal")],
          ]}
        />
      }
    >
      <AppFrame
        railActive="documents"
        aside={<DocTree />}
        toolbar={<DocTabs />}
      >
        <div style={{ position: "absolute", inset: 0, display: "flex" }}>
          {/* ── LEFT COMPANION: thin Builder/Lens icon-rail + the QUERY BUILDER / OPERATION LENS panel ── */}
          <CompanionRail lensOn={lensOn === 1} />
          <div
            style={{
              width: 280,
              flex: "0 0 auto",
              display: "flex",
              flexDirection: "column",
              minWidth: 0,
              borderRight: `1px solid ${token.border}`,
            }}
          >
            <CompanionPanel
              lensOn={lensOn === 1}
              custChecked={custChecked === 1}
              progress={progress}
            />
          </div>
          <div style={{ width: 1, background: token.border }} />

          {/* Request column */}
          <div
            style={{
              flex: "1 1 0",
              display: "flex",
              flexDirection: "column",
              minWidth: 0,
            }}
          >
            <ColumnHeader title="Request">
              <span style={{ display: "flex", color: token.textSecondary }}>
                <IconFormat size={16} color="currentColor" />
              </span>
              <RunButton progress={progress} running={running} />
            </ColumnHeader>
            <div
              style={{
                flex: 1,
                minHeight: 0,
                overflow: "hidden",
                position: "relative",
              }}
            >
              <QueryEditor progress={progress} />
            </div>
            {/* variables sub-pane — statically present, no animated reveal */}
            <div
              style={{
                flex: "0 0 auto",
                borderTop: `1px solid ${token.border}`,
              }}
            >
              <div
                style={{
                  height: 30,
                  display: "flex",
                  alignItems: "center",
                  gap: 16,
                  padding: "0 12px",
                  borderBottom: `1px solid ${token.border}`,
                  fontSize: 12,
                }}
              >
                <span style={{ position: "relative", color: token.textStrong }}>
                  GraphQL Variables
                  <span
                    style={{
                      position: "absolute",
                      left: 0,
                      right: 0,
                      bottom: -8,
                      height: 2,
                      background: ORANGE,
                    }}
                  />
                </span>
                <span style={{ color: token.textSecondary }}>HTTP Headers</span>
              </div>
              <div style={{ height: 76, overflow: "hidden" }}>
                <CodeBlock
                  code={D.variables}
                  lang="json"
                  gutter
                  caret={false}
                  fontSize={12}
                />
              </div>
            </div>
          </div>

          {/* separator */}
          <div style={{ width: 1, background: token.border }} />

          {/* Response column */}
          <div
            style={{
              flex: "1 1 0",
              display: "flex",
              flexDirection: "column",
              minWidth: 0,
              borderLeft: `1px solid ${token.surface}`,
            }}
          >
            <ColumnHeader
              title="Response"
              right={
                <motion.div
                  style={{
                    display: "flex",
                    alignItems: "center",
                    gap: 0,
                    opacity: statusOpacity,
                  }}
                >
                  <StatusInfo
                    icon={<IconCheck size={15} />}
                    value={D.status.code}
                    label="status"
                    valueColor={token.successText}
                  />
                  <Divider />
                  <StatusInfo value={D.status.duration} label="duration" />
                  <Divider />
                  <StatusInfo value={D.status.size} label="size" />
                </motion.div>
              }
            />
            <div
              style={{
                flex: 1,
                minHeight: 0,
                overflow: "hidden",
                position: "relative",
              }}
            >
              <motion.div
                style={{
                  position: "absolute",
                  inset: 0,
                  opacity: responseOpacity,
                }}
              >
                <CodeBlock
                  code={D.response}
                  lang="json"
                  progress={progress}
                  playWindow={TL.span("response")}
                  caret={false}
                  ariaLabel="JSON response"
                />
              </motion.div>
              <NoResponseYet progress={progress} />
            </div>
            {/* responses history — already present at frame 0; only the new pending row animates on Run */}
            <div
              style={{
                flex: "0 0 auto",
                borderTop: `1px solid ${token.border}`,
              }}
            >
              <div
                style={{
                  height: 30,
                  display: "flex",
                  alignItems: "center",
                  padding: "0 12px",
                  borderBottom: `1px solid ${token.border}`,
                  fontSize: 12,
                  color: token.textStrong,
                }}
              >
                Responses
              </div>
              <HistoryList progress={progress} />
            </div>
          </div>
        </div>
      </AppFrame>
    </Stage>
  );
}

/* ── LEFT COMPANION icon-rail (operation-sidebar.tsx): Operation Builder (active) + Operation Lens ── */

function CompanionRail({ lensOn }: { lensOn: boolean }) {
  const btn = (
    Icon: (p: IconProps) => React.ReactElement,
    active?: boolean,
    testid?: string,
  ) => (
    <div
      data-testid={testid}
      style={{
        width: 36,
        height: 36,
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
      }}
    >
      <div
        style={{
          width: 24,
          height: 24,
          borderRadius: 4,
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          background: active ? ORANGE : token.highlight,
          color: active ? "#fff" : token.textSecondary,
        }}
      >
        <Icon size={15} color="currentColor" />
      </div>
    </div>
  );
  return (
    <div
      style={{
        width: 36,
        flex: "0 0 auto",
        borderRight: `1px solid ${token.border}`,
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        paddingTop: 6,
        gap: 6,
      }}
    >
      {btn(IconObject, !lensOn, "builder-toggle")}
      {btn(IconInfo, lensOn, "lens-toggle")}
    </div>
  );
}

/* ── the LEFT COMPANION PANEL: cross-fades the Operation Builder ↔ the Operation Lens. The lens
 *    reveal is driven by the lensClick → lensReveal beats (the cursor toggles the rail). ── */

function CompanionPanel({
  lensOn,
  custChecked,
  progress,
}: {
  lensOn: boolean;
  custChecked: boolean;
  progress: MotionValue<number>;
}) {
  // builder fades out / lens fades in over lensReveal
  const builderOpacity = useTransform(
    progress,
    [TL.start("lensClick"), TL.at("lensReveal", 0.5)],
    [1, 0],
    { clamp: true },
  );
  const lensOpacity = useTransform(
    progress,
    [TL.at("lensReveal", 0.1), TL.end("lensReveal")],
    [0, 1],
    { clamp: true },
  );
  return (
    <>
      <ColumnHeader title={lensOn ? "Operation Lens" : "Operation Builder"} />
      <div
        style={{
          flex: 1,
          minHeight: 0,
          overflow: "hidden",
          position: "relative",
        }}
      >
        <motion.div
          style={{ position: "absolute", inset: 0, opacity: builderOpacity }}
        >
          <QueryBuilder custChecked={custChecked} progress={progress} />
        </motion.div>
        <motion.div
          style={{ position: "absolute", inset: 0, opacity: lensOpacity }}
        >
          <OperationLens />
        </motion.div>
      </div>
    </>
  );
}

/* ── the OPERATION LENS — the schema-reference DETAIL for the selected field. For the
 *    just-selected `customer: Customer!` field it shows: a header (field icon + name), a
 *    description, a Schema Coordinate, an Arguments section, a Return Type section, and the Fusion
 *    "Source Schemas" chip list. ── */

const LENS = {
  name: "customer",
  signature: "customer: Customer!",
  kindIcon: "object" as FieldKind,
  description:
    "The customer that placed this order. Resolved from the Accounts subgraph and stitched into the federated Order via the gateway.",
  coordinate: "Order.customer",
  args: [{ name: "id", type: "ID" }],
  returnType: {
    name: "Customer",
    kind: "object" as FieldKind,
    description: "A registered account in the EShops storefront.",
  },
  sources: ["Accounts", "Orders"],
};

function LensSection({
  title,
  children,
}: {
  title: string;
  children: React.ReactNode;
}) {
  return (
    <div
      style={{
        padding: "10px 12px",
        borderBottom: `1px solid ${token.border}`,
      }}
    >
      <div
        style={{
          display: "flex",
          alignItems: "center",
          justifyContent: "space-between",
          height: 20,
          marginBottom: 6,
        }}
      >
        <span
          style={{
            fontSize: 11,
            fontWeight: 700,
            letterSpacing: 0.3,
            textTransform: "uppercase",
            color: token.textSecondary,
          }}
        >
          {title}
        </span>
        <span style={{ display: "flex", color: token.textDim }}>
          <IconChevronDown size={11} color="currentColor" />
        </span>
      </div>
      {children}
    </div>
  );
}

function LensCoordinateRow({
  icon,
  name,
  type,
  color,
}: {
  icon: React.ReactNode;
  name: string;
  type?: string;
  color?: string;
}) {
  return (
    <div
      style={{
        display: "flex",
        alignItems: "center",
        gap: 6,
        fontFamily: token.mono,
        fontSize: 12.5,
      }}
    >
      <span
        style={{
          display: "flex",
          color: color ?? token.icObject,
          flex: "0 0 auto",
        }}
      >
        {icon}
      </span>
      <span style={{ color: token.synField }}>{name}</span>
      {type && (
        <>
          <span style={{ color: token.synPunct }}>:</span>
          <span style={{ color: token.synType }}>{type}</span>
        </>
      )}
    </div>
  );
}

function OperationLens() {
  return (
    <div
      role="img"
      aria-label="Operation Lens — schema reference detail for the customer field"
      style={{
        position: "absolute",
        inset: 0,
        overflow: "hidden",
        fontFamily: token.font,
        display: "flex",
        flexDirection: "column",
      }}
    >
      {/* header: field icon + name (the active lens item) */}
      <div
        style={{
          display: "flex",
          alignItems: "center",
          gap: 8,
          padding: "10px 12px",
          borderBottom: `1px solid ${token.border}`,
        }}
      >
        <span style={{ display: "flex", color: token.icObject }}>
          <IconObject size={16} color="currentColor" />
        </span>
        <span
          style={{ fontSize: 15, fontWeight: 600, color: token.textStrong }}
        >
          {LENS.name}
        </span>
        <span
          style={{
            marginLeft: "auto",
            fontFamily: token.mono,
            fontSize: 11.5,
            color: token.synType,
          }}
        >
          {LENS.returnType.name}!
        </span>
      </div>

      {/* description */}
      <div
        style={{
          padding: "10px 12px",
          borderBottom: `1px solid ${token.border}`,
          fontSize: 12.5,
          lineHeight: "20px",
          color: token.text,
        }}
      >
        {LENS.description}
      </div>

      {/* schema coordinate */}
      <LensSection title="Schema Coordinate">
        <span
          style={{
            fontFamily: token.mono,
            fontSize: 13,
            fontWeight: 600,
            color: token.textStrong,
          }}
        >
          {LENS.coordinate}
        </span>
      </LensSection>

      {/* source schemas (Fusion) — chip list */}
      <LensSection title="Source Schemas">
        <div style={{ display: "flex", flexWrap: "wrap", gap: 6 }}>
          {LENS.sources.map((s) => (
            <span
              key={s}
              style={{
                display: "inline-flex",
                alignItems: "center",
                height: 20,
                padding: "0 8px",
                borderRadius: 10,
                fontSize: 11,
                fontWeight: 600,
                color: token.blue,
                background: token.highlight,
                border: `1px solid ${token.border}`,
              }}
            >
              {s}
            </span>
          ))}
        </div>
      </LensSection>

      {/* arguments */}
      <LensSection title={`Arguments (${LENS.args.length})`}>
        <div style={{ display: "flex", flexDirection: "column", gap: 4 }}>
          {LENS.args.map((a) => (
            <LensCoordinateRow
              key={a.name}
              icon={<IconScalar size={13} />}
              name={a.name}
              type={a.type}
              color={token.icScalar}
            />
          ))}
        </div>
      </LensSection>

      {/* return type */}
      <LensSection title="Return Type">
        <LensCoordinateRow
          icon={<IconObject size={13} />}
          name={LENS.returnType.name}
          color={token.icObject}
        />
        <div
          style={{
            fontSize: 12,
            lineHeight: "18px",
            color: token.textSecondary,
            marginTop: 4,
          }}
        >
          {LENS.returnType.description}
        </div>
      </LensSection>
    </div>
  );
}

/* ── the QUERY BUILDER: a checkbox field-tree of the GetOrder operation ──
 * Each row: an indent, an expand chevron (for object fields), a CHECKBOX, a field-type icon
 * (scalar/object/enum colors), the field name and its type. Checked rows are in the operation. */

type FieldKind = "scalar" | "object" | "enum";
interface BField {
  name: string;
  type: string;
  kind: FieldKind;
  depth: number;
  checked: boolean;
  /** the operation root row (query GetOrder) */
  op?: boolean;
  /** object rows render an expand chevron */
  expandable?: boolean;
  /** this row is the `customer` field whose check is driven by the story */
  cust?: boolean;
}

const fieldIconColor = (k: FieldKind) =>
  k === "object"
    ? token.icObject
    : k === "enum"
      ? token.icEnum
      : token.icScalar;
function FieldIcon({ kind }: { kind: FieldKind }) {
  if (kind === "object") return <IconObject size={13} />;
  if (kind === "enum") return <IconEnum size={13} />;
  return <IconScalar size={13} />;
}

// query GetOrder → orderById(id:) → { id, total, status, items { product { … }, quantity },
//                                     customer { name email } }
const BUILDER: BField[] = [
  {
    name: "GetOrder",
    type: "Query",
    kind: "object",
    depth: 0,
    checked: true,
    op: true,
    expandable: true,
  },
  {
    name: "orderById",
    type: "Order",
    kind: "object",
    depth: 1,
    checked: true,
    expandable: true,
  },
  { name: "id", type: "ID!", kind: "scalar", depth: 2, checked: true },
  { name: "total", type: "Float!", kind: "scalar", depth: 2, checked: true },
  {
    name: "status",
    type: "OrderStatus!",
    kind: "enum",
    depth: 2,
    checked: true,
  },
  {
    name: "items",
    type: "[OrderItem!]!",
    kind: "object",
    depth: 2,
    checked: true,
    expandable: true,
  },
  {
    name: "product",
    type: "Product!",
    kind: "object",
    depth: 3,
    checked: true,
    expandable: true,
  },
  { name: "id", type: "ID!", kind: "scalar", depth: 4, checked: true },
  { name: "name", type: "String!", kind: "scalar", depth: 4, checked: true },
  { name: "price", type: "Float!", kind: "scalar", depth: 4, checked: true },
  { name: "quantity", type: "Int!", kind: "scalar", depth: 3, checked: true },
  {
    name: "customer",
    type: "Customer!",
    kind: "object",
    depth: 2,
    checked: false,
    expandable: true,
    cust: true,
  },
];

const BROW_H = 22;

function QueryBuilder({
  custChecked,
  progress,
}: {
  custChecked: boolean;
  progress: MotionValue<number>;
}) {
  return (
    <div
      role="img"
      aria-label="Operation Builder — checkbox field tree for the GetOrder query"
      style={{
        position: "absolute",
        inset: 0,
        padding: "4px 0",
        fontFamily: token.font,
        overflow: "hidden",
      }}
    >
      {BUILDER.map((f, i) => (
        <BuilderRow
          key={`${f.depth}-${f.name}-${i}`}
          f={f}
          custChecked={custChecked}
          progress={progress}
        />
      ))}
    </div>
  );
}

function BuilderRow({
  f,
  custChecked,
  progress,
}: {
  f: BField;
  custChecked: boolean;
  progress: MotionValue<number>;
}) {
  const checked = f.cust ? custChecked : f.checked;
  // when `customer` becomes checked, its row gets the orange selection wash + the checkbox fills
  const sel = f.cust && custChecked;
  // a brief flash when the field is ticked (selection wash pulses in)
  const flashTick = TL.start("tickCustomer");
  const flash = useTransform(
    progress,
    f.cust
      ? [flashTick, flashTick + 0.01, flashTick + 0.08]
      : [0, 0.0001, 0.0002],
    [0, 1, sel ? 0.0 : 0],
    { clamp: true },
  );
  return (
    <div
      style={{
        position: "relative",
        display: "flex",
        alignItems: "center",
        gap: 4,
        height: BROW_H,
        paddingLeft: 8 + f.depth * 14,
        paddingRight: 8,
        background: sel ? token.highlight : "transparent",
      }}
    >
      {f.cust && (
        <motion.span
          style={{
            position: "absolute",
            inset: 0,
            background: ORANGE,
            opacity: flash,
          }}
        />
      )}
      {/* expand chevron (object fields) or a spacer */}
      <span
        style={{
          width: 10,
          flex: "0 0 auto",
          display: "flex",
          color: token.textSecondary,
          position: "relative",
        }}
      >
        {f.expandable && <IconChevronDown size={9} color="currentColor" />}
      </span>
      {/* checkbox — the operation always has GetOrder/orderById, so the op row is not checkable */}
      {f.op ? (
        <span
          style={{
            width: 13,
            flex: "0 0 auto",
            display: "flex",
            color: kindColor("query"),
            position: "relative",
          }}
        >
          <KindGlyph kind="query" size={12} />
        </span>
      ) : (
        <Checkbox
          checked={checked}
          accent={sel}
          testid={f.cust ? "cust-checkbox" : undefined}
        />
      )}
      {/* field-type icon */}
      <span
        style={{
          display: "flex",
          color: fieldIconColor(f.kind),
          flex: "0 0 auto",
          position: "relative",
        }}
      >
        {f.op ? null : <FieldIcon kind={f.kind} />}
      </span>
      {/* name : type */}
      <span
        style={{
          fontSize: 12,
          whiteSpace: "nowrap",
          overflow: "hidden",
          textOverflow: "ellipsis",
          position: "relative",
        }}
      >
        <span
          style={{
            color: f.op
              ? token.textStrong
              : sel
                ? token.textStrong
                : token.synField,
            fontFamily: token.mono,
          }}
        >
          {f.name}
        </span>
        <span style={{ color: token.synPunct }}>: </span>
        <span style={{ color: token.synType, fontFamily: token.mono }}>
          {f.type}
        </span>
      </span>
    </div>
  );
}

function Checkbox({
  checked,
  accent,
  testid,
}: {
  checked: boolean;
  accent?: boolean;
  testid?: string;
}) {
  return (
    <span
      data-testid={testid}
      style={{
        width: 13,
        height: 13,
        flex: "0 0 auto",
        borderRadius: 3,
        border: `1.5px solid ${checked ? (accent ? ORANGE : token.accent) : token.borderStrong}`,
        background: checked ? (accent ? ORANGE : token.accent) : "transparent",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        position: "relative",
      }}
    >
      {checked && (
        <svg
          width={9}
          height={9}
          viewBox="0 0 16 16"
          fill="none"
          stroke="#fff"
          strokeWidth={2.4}
          strokeLinecap="round"
          strokeLinejoin="round"
        >
          <path d="M3.5 8.5l3 3 6-6.5" />
        </svg>
      )}
    </span>
  );
}

/* ── the query editor: pre-written GraphQL; the `customer { name email }` block snaps in once
 *    the builder checkbox is ticked (insertBlock stage) ── */

type Seg = { t: string; c: string };

const synField = token.synField;
const synType = token.synType;
const synKw = token.synKeyword;
const synVar = token.blue;
const synPunct = token.synPunct;

// Static lines above and below the inserted block (line index irrelevant; we render in order).
const HEAD: Seg[][] = [
  [
    { t: "query ", c: synKw },
    { t: "GetOrder", c: synType },
    { t: "(", c: synPunct },
    { t: "$id", c: synVar },
    { t: ": ", c: synPunct },
    { t: "ID", c: synType },
    { t: "!", c: synPunct },
    { t: ") {", c: synPunct },
  ],
  [
    { t: "  orderById", c: synField },
    { t: "(", c: synPunct },
    { t: "id", c: synField },
    { t: ": ", c: synPunct },
    { t: "$id", c: synVar },
    { t: ") {", c: synPunct },
  ],
  [{ t: "    id", c: synField }],
  [{ t: "    total", c: synField }],
  [{ t: "    status", c: synField }],
  [
    { t: "    items", c: synField },
    { t: " {", c: synPunct },
  ],
  [
    { t: "      product", c: synField },
    { t: " {", c: synPunct },
  ],
  [{ t: "        id", c: synField }],
  [{ t: "        name", c: synField }],
  [{ t: "        price", c: synField }],
  [{ t: "      }", c: synPunct }],
  [{ t: "      quantity", c: synField }],
  [{ t: "    }", c: synPunct }],
];
// the accepted, ready selection set — snaps in once `customer` is ticked in the builder
const INSERTED: Seg[][] = [
  [
    { t: "    customer", c: synField },
    { t: " {", c: synPunct },
  ],
  [{ t: "      name", c: synField }],
  [{ t: "      email", c: synField }],
  [{ t: "    }", c: synPunct }],
];
const TAIL: Seg[][] = [[{ t: "  }", c: synPunct }], [{ t: "}", c: synPunct }]];

const LH = 19;
const PAD = 10;
const FS = 12.5;
const GUTTER = 36;

function QueryEditor({ progress }: { progress: MotionValue<number> }) {
  // the inserted block reveals as the builder checkbox is ticked, over insertBlock
  const insertedOpacity = useTransform(
    progress,
    [TL.start("tickCustomer"), TL.at("insertBlock", 0.6)],
    [0, 1],
    { clamp: true },
  );
  const insertedHeight = useTransform(
    progress,
    [TL.start("tickCustomer"), TL.end("insertBlock")],
    [0, INSERTED.length * LH],
    { clamp: true },
  );

  const gutterStart = 1;
  const headRows = HEAD.length;
  const insertLineNo = headRows + 1;

  return (
    <div
      role="img"
      aria-label="GraphQL request editor"
      style={{
        fontFamily: token.mono,
        fontSize: FS,
        lineHeight: `${LH}px`,
        padding: `${PAD}px 0`,
        whiteSpace: "pre",
        overflow: "hidden",
        position: "absolute",
        inset: 0,
      }}
    >
      {HEAD.map((segs, i) => (
        <EditorLine key={`h${i}`} no={gutterStart + i} segs={segs} />
      ))}

      {/* inserted "customer { name email }" block — grows + fades in once ticked in the builder */}
      <motion.div
        style={{
          overflow: "hidden",
          height: insertedHeight,
          opacity: insertedOpacity,
        }}
      >
        {INSERTED.map((segs, i) => (
          <EditorLine key={`i${i}`} no={insertLineNo + i} segs={segs} />
        ))}
      </motion.div>

      {TAIL.map((segs, i) => (
        <EditorLine
          key={`t${i}`}
          no={insertLineNo + INSERTED.length + i}
          segs={segs}
        />
      ))}
    </div>
  );
}

function GutterCell({ no }: { no: number }) {
  return (
    <span
      style={{
        width: GUTTER,
        flex: "0 0 auto",
        textAlign: "right",
        paddingRight: 12,
        color: token.textDim,
        userSelect: "none",
      }}
    >
      {no}
    </span>
  );
}
function EditorLine({ no, segs }: { no: number; segs: Seg[] }) {
  return (
    <div style={{ display: "flex", minHeight: LH }}>
      <GutterCell no={no} />
      <span style={{ flex: 1, minWidth: 0 }}>
        {segs.map((s, i) => (
          <span key={i} style={{ color: s.c }}>
            {s.t}
          </span>
        ))}
      </span>
    </div>
  );
}

/* ── chrome helpers ── */

function ColumnHeader({
  title,
  right,
  children,
}: {
  title?: string;
  right?: React.ReactNode;
  children?: React.ReactNode;
}) {
  return (
    <div
      style={{
        height: 36,
        flex: "0 0 auto",
        display: "flex",
        alignItems: "center",
        gap: 8,
        padding: "0 10px",
        borderBottom: `1px solid ${token.border}`,
      }}
    >
      {title && (
        <span
          style={{
            position: "relative",
            fontSize: 14,
            fontWeight: 600,
            color: token.textStrong,
            display: "flex",
            alignItems: "center",
            height: "100%",
          }}
        >
          {title}
          <span
            style={{
              position: "absolute",
              left: 0,
              right: 0,
              bottom: 0,
              height: 2,
              background: ORANGE,
            }}
          />
        </span>
      )}
      <div
        style={{
          marginLeft: "auto",
          display: "flex",
          alignItems: "center",
          gap: 8,
        }}
      >
        {right}
        {children}
      </div>
    </div>
  );
}

function RunButton({
  progress,
  running,
}: {
  progress: MotionValue<number>;
  running: MotionValue<number>;
}) {
  const press = useTransform(
    progress,
    [TL.start("runClick"), TL.at("runClick", 0.5), TL.end("runClick")],
    [1, 0.94, 1],
    { clamp: true },
  );
  const spinnerOpacity = running;
  const playOpacity = useTransform(running, [0, 1], [1, 0]);
  const label = useTransform(running, (r): string =>
    r ? "Cancel" : "Run GetOrder",
  );
  return (
    <motion.div
      data-testid="run-button"
      style={{
        display: "flex",
        alignItems: "center",
        height: 26,
        borderRadius: 4,
        background: token.accent,
        color: "#fff",
        scale: press,
        overflow: "hidden",
      }}
    >
      <div
        style={{
          display: "flex",
          alignItems: "center",
          gap: 6,
          padding: "0 10px",
          height: "100%",
        }}
      >
        <span
          style={{
            position: "relative",
            width: 13,
            height: 13,
            display: "inline-block",
          }}
        >
          <motion.span
            style={{
              position: "absolute",
              inset: 0,
              opacity: playOpacity,
              display: "flex",
            }}
          >
            <IconRun size={13} color="#fff" />
          </motion.span>
          <motion.span
            style={{
              position: "absolute",
              inset: 0,
              opacity: spinnerOpacity,
              display: "flex",
            }}
          >
            <IconSpinner size={13} color="#fff" />
          </motion.span>
        </span>
        <motion.span
          style={{ fontSize: 12.5, fontWeight: 600, whiteSpace: "nowrap" }}
        >
          {label}
        </motion.span>
      </div>
      <div
        style={{
          width: 1,
          height: "100%",
          background: "rgba(255,255,255,0.25)",
        }}
      />
      <div style={{ display: "flex", alignItems: "center", padding: "0 6px" }}>
        <IconChevronDown size={12} color="#fff" />
      </div>
    </motion.div>
  );
}

function Divider() {
  return (
    <span
      style={{
        width: 1,
        height: 16,
        background: token.border,
        margin: "0 8px",
      }}
    />
  );
}
function StatusInfo({
  icon,
  value,
  label,
  valueColor,
}: {
  icon?: React.ReactNode;
  value: string;
  label: string;
  valueColor?: string;
}) {
  return (
    <div
      style={{
        display: "flex",
        alignItems: "center",
        gap: 6,
        padding: "0 2px",
      }}
    >
      {icon}
      <div
        style={{
          display: "flex",
          flexDirection: "column",
          alignItems: "flex-start",
          lineHeight: 1.1,
        }}
      >
        <span
          style={{
            fontSize: 13,
            fontWeight: 700,
            color: valueColor ?? token.textStrong,
          }}
        >
          {value}
        </span>
        <span style={{ fontSize: 10, color: token.textSecondary }}>
          {label}
        </span>
      </div>
    </div>
  );
}

function NoResponseYet({ progress }: { progress: MotionValue<number> }) {
  const opacity = useTransform(
    progress,
    [0, TL.start("runClick"), TL.start("response")],
    [1, 1, 0],
    { clamp: true },
  );
  return (
    <motion.div
      style={{
        position: "absolute",
        inset: 0,
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        opacity,
        color: token.textSecondary,
        fontSize: 13,
      }}
    >
      No responses yet
    </motion.div>
  );
}

function HistoryList({ progress }: { progress: MotionValue<number> }) {
  return (
    <div style={{ padding: "2px 0" }}>
      {D.history.map((h, i) => {
        const first = i === 0;
        return (
          <HistoryRow
            key={h.time}
            h={h}
            active={first}
            progress={progress}
            pending={first}
          />
        );
      })}
    </div>
  );
}
function HistoryRow({
  h,
  active,
  progress,
  pending,
}: {
  h: (typeof D.history)[number];
  active: boolean;
  progress: MotionValue<number>;
  pending: boolean;
}) {
  // the first (active) row spins in as pending on Run and resolves to a check once the status resolves.
  // The two older rows are already present at frame 0. (hooks called unconditionally)
  const runClick = TL.start("runClick");
  const resolve = TL.start("statusResolve");
  const rowOpacity = useTransform(
    progress,
    pending ? [runClick, TL.end("runClick")] : [0, 0.0001],
    [pending ? 0 : 1, 1],
    { clamp: true },
  );
  const checkOpacity = useTransform(progress, (p) =>
    pending ? (p >= resolve ? 1 : 0) : 1,
  );
  const spinnerOpacity = useTransform(progress, (p) =>
    pending && p >= runClick && p < resolve ? 1 : 0,
  );
  return (
    <motion.div
      style={{
        display: "flex",
        alignItems: "center",
        gap: 8,
        height: 26,
        padding: "0 12px",
        background: active ? token.highlight : "transparent",
        opacity: rowOpacity,
      }}
    >
      <span
        style={{
          position: "relative",
          width: 15,
          height: 15,
          display: "inline-block",
          flex: "0 0 auto",
        }}
      >
        <motion.span
          style={{
            position: "absolute",
            inset: 0,
            display: "flex",
            opacity: checkOpacity,
          }}
        >
          {h.ok ? <IconCheck size={15} /> : <IconErrorCircle size={15} />}
        </motion.span>
        {pending && (
          <motion.span
            style={{
              position: "absolute",
              inset: 0,
              display: "flex",
              opacity: spinnerOpacity,
            }}
          >
            <IconSpinner size={14} color={token.textSecondary} />
          </motion.span>
        )}
      </span>
      <span style={{ display: "flex", color: kindColor(h.kind as Kind) }}>
        <KindGlyph kind={h.kind as Kind} size={13} />
      </span>
      <span
        style={{
          fontSize: 12.5,
          color: active ? token.textStrong : token.text,
        }}
      >
        {h.name}
      </span>
      <span
        style={{
          marginLeft: "auto",
          fontSize: 11,
          color: token.textSecondary,
          fontFamily: token.mono,
        }}
      >
        {h.time}
      </span>
      <span
        style={{
          fontSize: 11,
          color: token.textSecondary,
          fontFamily: token.mono,
          width: 52,
          textAlign: "right",
        }}
      >
        {h.took}
      </span>
    </motion.div>
  );
}

/* ── toolbar / tree ── */

function DocTabs() {
  return (
    <div
      style={{
        display: "flex",
        alignItems: "flex-end",
        gap: 6,
        height: "100%",
      }}
    >
      <DocTab name="createOrder" kind="mutation" />
      <DocTab name="GetOrder" kind="query" activeTab />
      <DocTab name="SearchProducts" kind="query" />
      <span
        style={{
          display: "flex",
          gap: 8,
          marginLeft: 6,
          color: token.textSecondary,
        }}
      >
        <IconPlus size={16} color="currentColor" />
        <IconSave size={16} color="currentColor" />
      </span>
    </div>
  );
}
function DocTab({
  name,
  kind,
  activeTab,
}: {
  name: string;
  kind: Kind;
  activeTab?: boolean;
}) {
  return (
    <div
      style={{
        position: "relative",
        height: 28,
        display: "flex",
        alignItems: "center",
        gap: 6,
        padding: "0 10px",
        borderRadius: "4px 4px 0 0",
        border: `1px solid ${activeTab ? ORANGE : token.border}`,
        color: activeTab ? token.textStrong : token.text,
        background: token.surface,
        maxWidth: 170,
      }}
    >
      <span style={{ display: "flex", color: kindColor(kind) }}>
        <KindGlyph kind={kind} size={12} />
      </span>
      <span
        style={{
          fontSize: 12.5,
          whiteSpace: "nowrap",
          overflow: "hidden",
          textOverflow: "ellipsis",
        }}
      >
        {name}
      </span>
      <span style={{ color: token.textSecondary, display: "flex" }}>
        <IconClose size={11} color="currentColor" />
      </span>
    </div>
  );
}

/* ── Documents explorer tree — REAL operation-kind icons (the tree renders
 *    folders with api-collection / gateway / service icons; documents carry the operation-kind
 *    glyph: query Q-glyph, mutation diamond, subscription pulse). ── */

interface TreeNode {
  label: string;
  depth: number;
  kind?: Kind;
  sel?: boolean;
  folder?: boolean;
  expanded?: boolean;
  color?: string;
}
const TREE: TreeNode[] = [
  {
    label: "EShops",
    depth: 0,
    folder: true,
    expanded: true,
    color: token.icEnum,
  },
  {
    label: "Subgraphs",
    depth: 1,
    folder: true,
    expanded: true,
    color: token.pink,
  },
  { label: "createOrder", depth: 2, kind: "mutation" },
  { label: "GetOrder", depth: 2, kind: "query", sel: true },
  { label: "SearchProducts", depth: 2, kind: "query" },
  { label: "OrderUpdates", depth: 2, kind: "subscription" },
  { label: "Fusion", depth: 0, folder: true, color: token.pink },
  { label: "Netflix", depth: 0, folder: true, color: token.icEnum },
];

function FolderIcon({
  expanded,
  color,
}: {
  expanded?: boolean;
  color?: string;
}) {
  // api-collection style folder
  return expanded ? (
    <svg
      width={13}
      height={13}
      viewBox="0 0 24 24"
      fill="none"
      stroke={color ?? token.icEnum}
      strokeWidth={1.5}
    >
      <path d="M3 7a2 2 0 012-2h4l2 2h8a2 2 0 012 2v1H3z" />
      <path d="M3 10h18l-2 7a2 2 0 01-2 1.6H6.8A2 2 0 015 17z" />
    </svg>
  ) : (
    <svg
      width={13}
      height={13}
      viewBox="0 0 24 24"
      fill="none"
      stroke={color ?? token.icEnum}
      strokeWidth={1.5}
    >
      <path d="M3 7a2 2 0 012-2h4l2 2h8a2 2 0 012 2v8a2 2 0 01-2 2H5a2 2 0 01-2-2z" />
    </svg>
  );
}

function TreeRow({ node }: { node: TreeNode }) {
  const { label, depth, kind, sel, folder, expanded, color } = node;
  return (
    <div
      style={{
        display: "flex",
        alignItems: "center",
        gap: 5,
        height: 22,
        paddingLeft: 8 + depth * 13,
        paddingRight: 8,
        background: sel ? token.highlight : "transparent",
        borderLeft: sel ? `2px solid ${ORANGE}` : "2px solid transparent",
      }}
    >
      {folder ? (
        <>
          <span style={{ display: "flex", color: token.textSecondary }}>
            {expanded ? (
              <IconChevronDown size={9} color="currentColor" />
            ) : (
              <IconChevronRight size={9} color="currentColor" />
            )}
          </span>
          <FolderIcon expanded={expanded} color={color} />
        </>
      ) : (
        <>
          <span style={{ width: 9 }} />
          {kind && (
            <span style={{ display: "flex", color: kindColor(kind) }}>
              <KindGlyph kind={kind} size={12} />
            </span>
          )}
        </>
      )}
      <span
        style={{
          fontSize: 12.5,
          color: sel ? token.textStrong : token.text,
          whiteSpace: "nowrap",
          overflow: "hidden",
          textOverflow: "ellipsis",
        }}
      >
        {label}
      </span>
    </div>
  );
}

function DocTree() {
  return (
    <div style={{ display: "flex", flexDirection: "column", height: "100%" }}>
      <div
        style={{
          display: "flex",
          alignItems: "center",
          justifyContent: "space-between",
          padding: "11px 10px 7px",
        }}
      >
        <span
          style={{ fontSize: 14, fontWeight: 600, color: token.textStrong }}
        >
          Documents
        </span>
        <span style={{ display: "flex", gap: 8, color: token.textSecondary }}>
          <IconPlus size={16} color="currentColor" />
        </span>
      </div>
      <div style={{ margin: "0 8px 8px" }}>
        <div
          style={{
            height: 26,
            borderRadius: 4,
            background: token.bg,
            border: `1px solid ${token.border}`,
            display: "flex",
            alignItems: "center",
            gap: 6,
            padding: "0 8px",
            fontSize: 11,
            color: token.textSecondary,
          }}
        >
          <IconSearch size={12} color="currentColor" />
          Filter…
        </div>
      </div>
      {TREE.map((node, i) => (
        <TreeRow key={`${node.label}-${i}`} node={node} />
      ))}
      <div
        style={{
          marginTop: "auto",
          padding: 10,
          fontSize: 11,
          color: token.textDim,
          display: "flex",
          alignItems: "center",
          gap: 6,
        }}
      >
        <IconField size={12} color="currentColor" />
        EShops · GraphQL documents
      </div>
    </div>
  );
}
