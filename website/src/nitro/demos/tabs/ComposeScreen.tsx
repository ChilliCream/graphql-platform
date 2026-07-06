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

const TL = timeline([
  { name: "establish", ms: 700 },
  { name: "moveToBuilder", ms: 1400 },
  { name: "tickCustomer", ms: 130 },
  { name: "insertBlock", ms: 700 },
  { name: "moveToRun", ms: 1300 },
  { name: "runClick", ms: 120 },
  { name: "runLoad", ms: 450 },
  { name: "response", ms: 1400 },
  { name: "statusResolve", ms: 550 },
  { name: "moveToLens", ms: 1300 },
  { name: "lensClick", ms: 120 },
  { name: "lensReveal", ms: 650 },
  { name: "lensDwell", ms: 2000 },
]);

export const COMPOSE_MS = TL.total;
export const COMPOSE_TL = TL;

export interface ComposeScreenProps {
  progress: MotionValue<number>;
  active?: boolean;
}

export function ComposeScreen({ progress }: ComposeScreenProps) {
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

  const tick = TL.start("tickCustomer");
  const checkedAt = (p: number) => (p >= tick ? 1 : 0);
  const [custChecked, setCustChecked] = useState(() =>
    checkedAt(progress.get()),
  );
  useMotionValueEvent(progress, "change", (p) => setCustChecked(checkedAt(p)));

  const lensClickP = TL.start("lensClick");
  const lensAt = (p: number) => (p >= lensClickP ? 1 : 0);
  const [lensOn, setLensOn] = useState(() => lensAt(progress.get()));
  useMotionValueEvent(progress, "change", (p) => setLensOn(lensAt(p)));

  const CUST_X = 396;
  const CUST_Y = 327;
  const RUN_X = 980;
  const RUN_Y = 64;
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

          <div style={{ width: 1, background: token.border }} />

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

function CompanionPanel({
  lensOn,
  custChecked,
  progress,
}: {
  lensOn: boolean;
  custChecked: boolean;
  progress: MotionValue<number>;
}) {
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

type FieldKind = "scalar" | "object" | "enum";
interface BField {
  name: string;
  type: string;
  kind: FieldKind;
  depth: number;
  checked: boolean;
  op?: boolean;
  expandable?: boolean;
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
  const sel = f.cust && custChecked;
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

type Seg = { t: string; c: string };

const synField = token.synField;
const synType = token.synType;
const synKw = token.synKeyword;
const synVar = token.blue;
const synPunct = token.synPunct;

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
