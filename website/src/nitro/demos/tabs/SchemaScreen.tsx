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
import { TABREEL_CANVAS } from "../../primitives/reel/TabReel";
import { Badge } from "../../primitives/Badge";
import { UnderlineTab } from "../../primitives/UnderlineTab";
import { PanelTile } from "../../primitives/PanelTile";
import { token } from "../../lib/tokens";
import { ease } from "../../lib/motion";
import { smoothLinePath, areaFromLine, type Pt } from "../../lib/scale";
import { smoothSeries } from "../../lib/data/tabs";
import {
  IconField,
  IconWarning,
  IconCheck,
  IconSearch,
  IconChevronDown,
  IconChevronUp,
  IconApiGateway,
  IconClose,
  IconPlus,
  IconSave,
  IconMore,
  IconDownload,
  IconCalendar,
  IconSpinner,
  IconLock,
  IconSettings,
} from "../../primitives/icons";

const W = TABREEL_CANVAS.w;
const H = TABREEL_CANVAS.h;
const ORANGE = token.graphEdgeActive;

interface Coord {
  coord: string;
  usage: string;
  requests: number;
  deprecated?: boolean;
}
const ALL_COORDS: Coord[] = [
  { coord: "Query.products", usage: "48.2K", requests: 48200 },
  { coord: "Product.reviews", usage: "31.7K", requests: 31700 },
  { coord: "Product.price", usage: "29.4K", requests: 29400 },
  { coord: "Query.productById", usage: "18.9K", requests: 18900 },
  { coord: "Review.rating", usage: "12.4K", requests: 12400 },
  { coord: "Mutation.createOrder", usage: "9.8K", requests: 9800 },
  { coord: "Product.inStock", usage: "4.1K", requests: 4108, deprecated: true },
  {
    coord: "Order.legacyTotal",
    usage: "1.2K",
    requests: 1240,
    deprecated: true,
  },
  { coord: "Account.fullName", usage: "612", requests: 612, deprecated: true },
  {
    coord: "Review.legacyScore",
    usage: "180",
    requests: 180,
    deprecated: true,
  },
  { coord: "Cart.couponCode", usage: "44", requests: 44, deprecated: true },
];
const DEPRECATED = ALL_COORDS.filter((c) => c.deprecated);
const byName = (a: Coord, b: Coord) => a.coord.localeCompare(b.coord);
const ALL_BY_NAME = [...ALL_COORDS].sort(byName);
const DEPRECATED_BY_NAME = [...DEPRECATED].sort(byName);
const DEPRECATED_BY_REQ = [...DEPRECATED].sort(
  (a, b) => b.requests - a.requests,
);
const HERO = "Product.inStock";

const HERO_DETAIL: [string, string][] = [
  ["Requests", "4,108"],
  ["Error rate", "0.04%"],
  ["Mean duration", "9.2 ms"],
  ["Last seen", "6 days ago"],
];
const CLIENTS = [
  { name: "Admin Dashboard", ops: "7 operations", requests: "1.9K" },
  { name: "Partner API", ops: "3 operations", requests: "312" },
  { name: "iOS App (legacy)", ops: "1 operation", requests: "41" },
];
const OPERATIONS = [
  {
    name: "InventoryReport",
    requests: "1,210",
    latency: "12 ms",
    error: "0.0%",
    impact: 0.9,
  },
  {
    name: "LowStockAlert",
    requests: "540",
    latency: "9 ms",
    error: "0.0%",
    impact: 0.4,
  },
  {
    name: "DashboardSummary",
    requests: "150",
    latency: "18 ms",
    error: "0.2%",
    impact: 0.12,
  },
];
const SERIES = {
  throughput: smoothSeries(31, 40, 60, 26),
  latency: smoothSeries(32, 40, 9, 2.4),
  errors: smoothSeries(33, 40, 0.6, 0.5),
};

const RAIL = 50;
const COL_W = 300;
const H_DOCTABS = 38;
const H_VIEWNAV = 36;
const H_TOOLBAR = 36;
const HEADER_H = H_DOCTABS + H_VIEWNAV + H_TOOLBAR;
const COORD_HEADER = 32;
const COORD_SEARCH = 36;
const COORD_CONTROLS = 36;
const LIST_TOP = HEADER_H + COORD_HEADER + COORD_SEARCH + COORD_CONTROLS;
const ROW_H = 30;
const rowY = (i: number) => LIST_TOP + i * ROW_H + ROW_H / 2;
const CONTROLS_Y = HEADER_H + COORD_HEADER + COORD_SEARCH + COORD_CONTROLS / 2;
const MENU_TOP = LIST_TOP + 2;

const FILTER_OPEN = 0.16;
const FILTER_PICK = 0.24;
const ORDER_OPEN = 0.33;
const ORDER_PICK = 0.42;
const SELECT = 0.57;
const CLIENT_SELECT = 0.8;
const LOAD = 0.05;
const CLIENT_X = 1466;
const CLIENT_Y = 384;

export interface SchemaScreenProps {
  progress: MotionValue<number>;
  active?: boolean;
}

export function SchemaScreen({ progress }: SchemaScreenProps) {
  const phaseAt = (p: number) =>
    p >= ORDER_PICK ? 2 : p >= FILTER_PICK ? 1 : 0;
  const listPhaseAt = (p: number) =>
    p >= ORDER_PICK + LOAD ? 2 : p >= FILTER_PICK + LOAD ? 1 : 0;
  const stageAt = (p: number) => (p >= CLIENT_SELECT ? 2 : p >= SELECT ? 1 : 0);
  const [phase, setPhase] = useState(() => phaseAt(progress.get()));
  const [listPhase, setListPhase] = useState(() => listPhaseAt(progress.get()));
  const [stage, setStage] = useState(() => stageAt(progress.get()));
  useMotionValueEvent(progress, "change", (p) => {
    setPhase(phaseAt(p));
    setListPhase(listPhaseAt(p));
    setStage(stageAt(p));
  });

  const T = [
    0, 0.1, 0.14, 0.16, 0.22, 0.24, 0.31, 0.33, 0.4, 0.42, 0.52, 0.56, 0.58,
    0.78, 0.8, 1,
  ];
  const cx = useTransform(
    progress,
    T,
    [
      320,
      320,
      102,
      102,
      135,
      135,
      288,
      288,
      255,
      255,
      255,
      200,
      200,
      CLIENT_X,
      CLIENT_X,
      CLIENT_X,
    ],
    { ease: ease.inOut },
  );
  const cy = useTransform(
    progress,
    T,
    [
      150,
      150,
      CONTROLS_Y,
      CONTROLS_Y,
      282,
      282,
      CONTROLS_Y,
      CONTROLS_Y,
      367,
      367,
      367,
      rowY(0),
      rowY(0),
      CLIENT_Y,
      CLIENT_Y,
      CLIENT_Y,
    ],
    { ease: ease.inOut },
  );

  return (
    <Stage
      width={W}
      height={H}
      fit="fill"
      chrome={false}
      ariaLabel="Nitro — filtering schema insights to a deprecated field and finding which clients and operations still use it"
      overlay={
        <Cursor
          x={cx}
          y={cy}
          progress={progress}
          clickTimes={[
            FILTER_OPEN,
            FILTER_PICK,
            ORDER_OPEN,
            ORDER_PICK,
            SELECT,
            CLIENT_SELECT,
          ]}
          pointerWindows={[[0.7, CLIENT_SELECT]]}
        />
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
          <DocTabStrip />
          <GatewayViewNav />
          <SchemaToolbar />
          <div style={{ flex: 1, minHeight: 0, display: "flex" }}>
            <CoordinatesColumn
              phase={phase}
              listPhase={listPhase}
              progress={progress}
            />
            {stage === 0 ? (
              <NoCoordinate />
            ) : (
              <div style={{ flex: 1, minWidth: 0, display: "flex" }}>
                <DetailsColumn />
                <UsageView progress={progress} />
              </div>
            )}
          </div>
        </div>

        <FilterMenu progress={progress} />
        <OrderMenu progress={progress} />
      </AppFrame>
    </Stage>
  );
}

function DocTab({ name }: { name: string }) {
  return (
    <div
      style={{
        position: "relative",
        height: 30,
        display: "flex",
        alignItems: "center",
        gap: 6,
        padding: "0 10px",
        borderRadius: "5px 5px 0 0",
        border: `1px solid ${token.border}`,
        background: token.surface,
        color: token.text,
        maxWidth: 180,
      }}
    >
      <IconApiGateway size={12} color={token.icObject} />
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

function DocTabStrip() {
  return (
    <div
      style={{
        height: H_DOCTABS,
        flex: "0 0 auto",
        display: "flex",
        alignItems: "flex-end",
        gap: 6,
        padding: "0 8px",
        background: token.bg,
        borderBottom: `1px solid ${token.border}`,
      }}
    >
      <DocTab name="EShops Gateway" />
      <span
        style={{
          display: "flex",
          gap: 8,
          marginLeft: 6,
          paddingBottom: 6,
          color: token.textSecondary,
        }}
      >
        <IconPlus size={15} color="currentColor" />
        <IconSave size={15} color="currentColor" />
        <IconMore size={15} color="currentColor" />
      </span>
    </div>
  );
}

function GatewayViewNav() {
  const views = [
    "Overview",
    "Monitoring",
    "Logs",
    "Schema",
    "Deployments",
    "Changelog",
    "Operations",
    "Clients",
    "Stages",
  ];
  return (
    <div
      style={{
        height: H_VIEWNAV,
        flex: "0 0 auto",
        display: "flex",
        alignItems: "center",
        gap: 20,
        padding: "0 12px",
        borderBottom: `1px solid ${token.border}`,
      }}
    >
      {views.map((v) => {
        const on = v === "Schema";
        return <UnderlineTab key={v} label={v} active={on} height="100%" />;
      })}
      <span
        style={{
          marginLeft: "auto",
          display: "flex",
          alignItems: "center",
          gap: 6,
          fontSize: 12,
          color: token.text,
          border: `1px solid ${token.border}`,
          borderRadius: 5,
          padding: "4px 8px",
        }}
      >
        Production <IconChevronDown size={12} color={token.textSecondary} />
      </span>
      <span
        style={{
          display: "flex",
          alignItems: "center",
          gap: 5,
          fontSize: 12,
          color: token.text,
        }}
      >
        <IconLock size={12} color={token.textSecondary} />
        eshops.fusion.cloud
      </span>
      <span style={{ color: token.textSecondary, display: "flex" }}>
        <IconSettings size={14} color="currentColor" />
      </span>
    </div>
  );
}

function SchemaToolbar() {
  const tab = (t: string, on?: boolean) => (
    <UnderlineTab label={t} active={!!on} height="100%" />
  );
  return (
    <div
      style={{
        height: H_TOOLBAR,
        flex: "0 0 auto",
        display: "flex",
        alignItems: "center",
        gap: 18,
        padding: "0 12px",
        borderBottom: `1px solid ${token.border}`,
      }}
    >
      {tab("Reference")}
      {tab("SDL")}
      {tab("Insights", true)}
      <span
        style={{
          marginLeft: "auto",
          display: "flex",
          alignItems: "center",
          gap: 6,
          fontSize: 12,
          color: token.text,
          border: `1px solid ${token.border}`,
          borderRadius: 5,
          padding: "4px 8px",
        }}
      >
        <IconCalendar size={12} color={token.textSecondary} /> Last 7 days
      </span>
      <span style={{ color: token.textSecondary, display: "flex" }}>
        <IconDownload size={15} color="currentColor" />
      </span>
      <div style={{ display: "flex", alignItems: "center", gap: 0 }}>
        {(
          [
            ["184", "Types"],
            ["1,243", "Fields"],
            ["12", "Directives"],
          ] as [string, string][]
        ).map(([v, k], i) => (
          <div
            key={k}
            style={{
              display: "flex",
              flexDirection: "column",
              alignItems: "center",
              textAlign: "center",
              padding: "0 12px",
              borderLeft: i ? `1px solid ${token.border}` : "none",
            }}
          >
            <span
              style={{ fontSize: 13, fontWeight: 600, color: token.textStrong }}
            >
              {v}
            </span>
            <span
              style={{
                fontSize: 10,
                color: token.textSecondary,
                textTransform: "uppercase",
              }}
            >
              {k}
            </span>
          </div>
        ))}
      </div>
    </div>
  );
}

function CoordinatesColumn({
  phase,
  listPhase,
  progress,
}: {
  phase: number;
  listPhase: number;
  progress: MotionValue<number>;
}) {
  const list =
    listPhase === 0
      ? ALL_BY_NAME
      : listPhase === 1
        ? DEPRECATED_BY_NAME
        : DEPRECATED_BY_REQ;
  const loadWindows: [number, number][] = [
    [FILTER_PICK, FILTER_PICK + LOAD],
    [ORDER_PICK, ORDER_PICK + LOAD],
  ];
  const listDim = useTransform(progress, (p): number =>
    loadWindows.some(([a, b]) => p >= a && p <= b) ? 0.4 : 1,
  );
  return (
    <div
      style={{
        width: COL_W,
        flex: "0 0 auto",
        borderRight: `1px solid ${token.border}`,
        display: "flex",
        flexDirection: "column",
        background: token.surface,
      }}
    >
      <div
        style={{
          height: COORD_HEADER,
          display: "flex",
          alignItems: "center",
          padding: "0 12px",
          fontSize: 13,
          fontWeight: 600,
          color: token.textStrong,
        }}
      >
        Coordinates
      </div>
      <div style={{ borderTop: `1px solid ${token.border}` }} />
      <div
        style={{
          height: COORD_SEARCH,
          display: "flex",
          alignItems: "center",
          padding: "0 10px",
        }}
      >
        <div
          style={{
            flex: 1,
            height: 26,
            borderRadius: 4,
            background: token.bg,
            border: `1px solid ${token.border}`,
            display: "flex",
            alignItems: "center",
            gap: 6,
            padding: "0 8px",
            fontSize: 11.5,
            color: token.textSecondary,
          }}
        >
          <IconSearch size={12} color="currentColor" /> Filter coordinates…
        </div>
      </div>
      <div style={{ borderTop: `1px solid ${token.border}` }} />
      <div
        style={{
          height: COORD_CONTROLS,
          display: "flex",
          alignItems: "center",
          gap: 8,
          padding: "0 10px",
        }}
      >
        <ControlButton
          active={phase >= 1}
          icon={
            <IconWarning
              size={13}
              color={phase >= 1 ? ORANGE : "currentColor"}
            />
          }
          label={phase === 0 ? "View all" : "Deprecated"}
        />
        <span style={{ flex: 1 }} />
        <ControlButton
          active={phase >= 2}
          icon={
            phase >= 2 ? (
              <IconChevronDown size={13} color={ORANGE} />
            ) : (
              <IconChevronUp size={13} color="currentColor" />
            )
          }
          label={phase === 2 ? "Requests" : "Name"}
        />
      </div>
      <div style={{ borderTop: `1px solid ${token.border}` }} />
      <div
        style={{
          flex: 1,
          minHeight: 0,
          overflow: "hidden",
          position: "relative",
        }}
      >
        <ListLoadingBar progress={progress} windows={loadWindows} />
        <motion.div style={{ opacity: listDim }}>
          {list.map((c) => {
            const hero = c.coord === HERO;
            return (
              <div
                key={c.coord}
                data-testid={hero ? "schema-hero-row" : undefined}
                style={{
                  display: "flex",
                  alignItems: "center",
                  gap: 8,
                  height: ROW_H,
                  padding: "0 12px",
                  background:
                    hero && listPhase === 2 ? token.highlight : "transparent",
                }}
              >
                <IconField size={14} />
                <span
                  style={{
                    fontSize: 12.5,
                    color:
                      hero && listPhase === 2 ? token.textStrong : token.text,
                    flex: 1,
                    whiteSpace: "nowrap",
                    overflow: "hidden",
                    textOverflow: "ellipsis",
                  }}
                >
                  {c.coord}
                </span>
                {c.deprecated && (
                  <IconWarning size={12} color={token.warning} />
                )}
                <span
                  style={{
                    fontSize: 12,
                    fontWeight: 500,
                    fontFamily: token.mono,
                    color: token.text,
                  }}
                >
                  {c.usage}
                </span>
              </div>
            );
          })}
        </motion.div>
      </div>
    </div>
  );
}

function ListLoadingBar({
  progress,
  windows,
}: {
  progress: MotionValue<number>;
  windows: [number, number][];
}) {
  const opacity = useTransform(progress, (p): number =>
    windows.some(([a, b]) => p >= a && p <= b) ? 1 : 0,
  );
  const left = useTransform(progress, (p): string => {
    for (const [a, b] of windows)
      if (p >= a && p <= b) return `${((p - a) / (b - a)) * 130 - 30}%`;
    return "-30%";
  });
  return (
    <motion.div
      style={{
        position: "absolute",
        top: 0,
        left: 0,
        right: 0,
        height: 2,
        overflow: "hidden",
        background: token.grid,
        opacity,
        zIndex: 2,
      }}
    >
      <motion.div
        style={{
          position: "absolute",
          top: 0,
          bottom: 0,
          width: "34%",
          left,
          background: token.accentHover,
        }}
      />
    </motion.div>
  );
}

function ControlButton({
  active,
  icon,
  label,
}: {
  active?: boolean;
  icon: React.ReactNode;
  label: string;
}) {
  return (
    <span
      style={{
        display: "flex",
        alignItems: "center",
        gap: 5,
        height: 24,
        padding: "0 8px",
        borderRadius: 4,
        border: `1px solid ${active ? ORANGE : token.border}`,
        background: token.bg,
        fontSize: 12,
        color: active ? token.textStrong : token.textSecondary,
        whiteSpace: "nowrap",
      }}
    >
      {icon}
      {label}
    </span>
  );
}

function Menu({
  progress,
  show,
  hide,
  x,
  width,
  children,
}: {
  progress: MotionValue<number>;
  show: number;
  hide: number;
  x: number;
  width: number;
  children: React.ReactNode;
}) {
  const opacity = useTransform(
    progress,
    [show, show + 0.015, hide - 0.015, hide],
    [0, 1, 1, 0],
    { clamp: true },
  );
  const sy = useTransform(progress, [show, show + 0.02], [-4, 0], {
    clamp: true,
  });
  return (
    <motion.div
      style={{
        position: "absolute",
        left: x,
        top: MENU_TOP,
        width,
        opacity,
        y: sy,
        zIndex: 60,
        background: token.surface,
        border: `1px solid ${token.borderStrong}`,
        borderRadius: 6,
        boxShadow: "0 8px 28px rgba(1,4,9,0.5)",
        padding: "5px 0",
      }}
    >
      {children}
    </motion.div>
  );
}
function MenuHeader({ text }: { text: string }) {
  return (
    <div
      style={{
        padding: "6px 12px 3px",
        fontSize: 10.5,
        fontWeight: 600,
        letterSpacing: 0.4,
        textTransform: "uppercase",
        color: token.textSecondary,
      }}
    >
      {text}
    </div>
  );
}
function MenuItem({
  text,
  checked,
  highlight,
  icon,
}: {
  text: string;
  checked?: boolean;
  highlight?: boolean;
  icon?: React.ReactNode;
}) {
  return (
    <div
      style={{
        display: "flex",
        alignItems: "center",
        gap: 8,
        height: 28,
        padding: "0 12px",
        background: highlight ? token.highlight : "transparent",
        fontSize: 12.5,
        color: token.text,
      }}
    >
      <span style={{ width: 14, display: "flex" }}>
        {checked ? <IconCheck size={13} color={ORANGE} /> : icon}
      </span>
      {text}
    </div>
  );
}
function FilterMenu({ progress }: { progress: MotionValue<number> }) {
  const [picked, setPicked] = useState(() => progress.get() >= FILTER_PICK);
  useMotionValueEvent(progress, "change", (p) => setPicked(p >= FILTER_PICK));
  return (
    <Menu
      progress={progress}
      show={FILTER_OPEN - 0.01}
      hide={FILTER_PICK + 0.03}
      x={RAIL + 12}
      width={186}
    >
      <MenuHeader text="Deprecation" />
      <MenuItem
        text="Hide deprecated"
        icon={<IconWarning size={12} color={token.textSecondary} />}
      />
      <MenuItem
        text="Show deprecated"
        checked={picked}
        highlight
        icon={<IconWarning size={12} color={token.warning} />}
      />
      <div
        style={{ borderTop: `1px solid ${token.border}`, margin: "4px 0" }}
      />
      <MenuHeader text="Coordinate Kinds" />
      <MenuItem text="Fields" icon={<IconField size={12} />} />
    </Menu>
  );
}
function OrderMenu({ progress }: { progress: MotionValue<number> }) {
  const [picked, setPicked] = useState(() => progress.get() >= ORDER_PICK);
  useMotionValueEvent(progress, "change", (p) => setPicked(p >= ORDER_PICK));
  return (
    <Menu
      progress={progress}
      show={ORDER_OPEN - 0.01}
      hide={ORDER_PICK + 0.03}
      x={RAIL + 120}
      width={180}
    >
      <MenuHeader text="Order Direction" />
      <MenuItem text="Ascending" />
      <MenuItem text="Descending" checked={picked} />
      <div
        style={{ borderTop: `1px solid ${token.border}`, margin: "4px 0" }}
      />
      <MenuHeader text="Order by Metric" />
      <MenuItem text="Name" />
      <MenuItem text="Requests" checked={picked} highlight />
      <MenuItem text="Throughput" />
      <MenuItem text="Error Rate" />
      <MenuItem text="Clients" />
    </Menu>
  );
}

function NoCoordinate() {
  return (
    <div
      style={{
        flex: 1,
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        justifyContent: "center",
        gap: 8,
        color: token.textSecondary,
      }}
    >
      <IconField size={28} color={token.textDim} />
      <div style={{ fontSize: 14, fontWeight: 600, color: token.text }}>
        No coordinate selected
      </div>
      <div style={{ fontSize: 12.5 }}>
        Select a coordinate to see its production usage.
      </div>
    </div>
  );
}

function DetailsColumn() {
  return (
    <div
      style={{
        width: 286,
        flex: "0 0 auto",
        borderRight: `1px solid ${token.border}`,
        padding: 14,
        display: "flex",
        flexDirection: "column",
        gap: 12,
      }}
    >
      <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
        <IconField size={16} />
        <span
          style={{ fontSize: 15, fontWeight: 600, color: token.textStrong }}
        >
          Product.inStock
        </span>
      </div>
      <div
        style={{
          display: "flex",
          alignItems: "center",
          gap: 6,
          fontSize: 11,
          padding: "3px 8px",
          borderRadius: 4,
          alignSelf: "flex-start",
          background: "rgba(210,153,34,0.16)",
          color: token.warning,
        }}
      >
        <IconWarning size={12} color={token.warning} /> Deprecated
      </div>
      <div style={{ fontSize: 12.5, fontFamily: token.mono, lineHeight: 1.5 }}>
        <span style={{ color: token.synField }}>inStock</span>
        <span style={{ color: token.synPunct }}>: </span>
        <span style={{ color: token.synKeyword }}>Boolean</span>
        <span style={{ color: token.synPunct }}>!</span>
      </div>
      <div
        style={{ fontSize: 12, color: token.textSecondary, lineHeight: 1.5 }}
      >
        Deprecation reason: use{" "}
        <span style={{ fontFamily: token.mono, color: token.text }}>
          availability
        </span>{" "}
        instead.
      </div>
      <div style={{ marginTop: 4 }}>
        {HERO_DETAIL.map(([k, v], i) => (
          <div
            key={k}
            style={{
              display: "flex",
              justifyContent: "space-between",
              padding: "6px 0",
              borderTop: i ? `1px solid ${token.border}` : "none",
              fontSize: 12.5,
            }}
          >
            <span style={{ color: token.textSecondary }}>{k}</span>
            <span style={{ color: token.text, fontFamily: token.mono }}>
              {v}
            </span>
          </div>
        ))}
      </div>
    </div>
  );
}

function UsageView({ progress }: { progress: MotionValue<number> }) {
  const fade = useTransform(progress, [SELECT, SELECT + 0.015], [0, 1], {
    clamp: true,
  });
  const reveal = useTransform(
    progress,
    [SELECT + 0.05, SELECT + 0.13],
    [0, 1],
    { clamp: true },
  );
  return (
    <motion.div
      style={{
        flex: 1,
        minWidth: 0,
        display: "flex",
        flexDirection: "column",
        opacity: fade,
      }}
    >
      <div
        style={{
          height: 32,
          flex: "0 0 auto",
          display: "flex",
          alignItems: "center",
          gap: 8,
          padding: "0 14px",
          borderBottom: `1px solid ${token.border}`,
          fontSize: 12.5,
        }}
      >
        <span style={{ color: token.textStrong }}>Usage</span>
      </div>
      <div style={{ flex: 1, minHeight: 0, position: "relative" }}>
        <motion.div
          style={{
            position: "absolute",
            inset: 0,
            overflow: "hidden",
            padding: 14,
            opacity: reveal,
          }}
        >
          <CoordinateUsage progress={progress} />
        </motion.div>
        <LoadingSpinner
          progress={progress}
          show={SELECT + 0.005}
          hide={SELECT + 0.045}
        />
      </div>
    </motion.div>
  );
}

function LoadingSpinner({
  progress,
  show,
  hide,
}: {
  progress: MotionValue<number>;
  show: number;
  hide: number;
}) {
  const opacity = useTransform(
    progress,
    [show, show + 0.006, hide - 0.006, hide],
    [0, 1, 1, 0],
    { clamp: true },
  );
  const rotate = useTransform(progress, [show, hide], [0, 540]);
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
      }}
    >
      <motion.div style={{ rotate, display: "flex" }}>
        <IconSpinner size={24} color={token.accent} />
      </motion.div>
    </motion.div>
  );
}

function CoordinateUsage({ progress }: { progress: MotionValue<number> }) {
  const chevRot = useTransform(
    progress,
    [CLIENT_SELECT - 0.02, CLIENT_SELECT + 0.03],
    [-90, 0],
    { clamp: true },
  );
  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 14 }}>
      <div style={{ display: "flex", gap: 12 }}>
        <ChartTile
          title="Throughput"
          values={SERIES.throughput}
          color={token.chThroughput}
          unit="opm"
          value="1,058"
          area
        />
        <ChartTile
          title="Latency"
          values={SERIES.latency}
          color={token.chLatency}
          unit="ms"
          value="9.2"
          area
        />
        <ChartTile
          title="Errors"
          values={SERIES.errors}
          color={token.error}
          unit="%"
          value="0.04"
        />
      </div>
      <PanelTile title="Clients" borderStrong bodyPadding="0">
        {CLIENTS.map((c, i) => (
          <div key={c.name}>
            <div
              data-testid={i === 0 ? "schema-client-row" : undefined}
              style={{
                display: "flex",
                alignItems: "center",
                padding: "11px 14px",
                borderTop: i ? `1px solid ${token.border}` : "none",
                background: i === 0 ? token.surface : "transparent",
              }}
            >
              <div style={{ flex: 1 }}>
                <div
                  style={{
                    fontSize: 13.5,
                    fontWeight: 600,
                    color: token.textStrong,
                  }}
                >
                  {c.name}
                </div>
                <div style={{ fontSize: 11.5, color: token.textSecondary }}>
                  {c.ops}
                </div>
              </div>
              <div style={{ textAlign: "right" }}>
                <div
                  style={{
                    fontSize: 16,
                    fontWeight: 700,
                    color: token.textStrong,
                  }}
                >
                  {c.requests}
                </div>
                <div
                  style={{
                    fontSize: 10.5,
                    color: token.textSecondary,
                    textTransform: "uppercase",
                  }}
                >
                  Requests
                </div>
              </div>
              <motion.span
                data-testid={i === 0 ? "schema-client-chevron" : undefined}
                style={{
                  marginLeft: 14,
                  color: i === 0 ? token.text : token.textDim,
                  display: "flex",
                  rotate: i === 0 ? chevRot : -90,
                }}
              >
                <IconChevronDown size={14} color="currentColor" />
              </motion.span>
            </div>
            {i === 0 && <ExpandedOps progress={progress} />}
          </div>
        ))}
      </PanelTile>
    </div>
  );
}

function ChartTile({
  title,
  values,
  color,
  unit,
  value,
  area,
}: {
  title: string;
  values: number[];
  color: string;
  unit: string;
  value: string;
  area?: boolean;
}) {
  const maxV = Math.max(...values);
  const max = maxV * 1.18 || 1;
  const pts: Pt[] = values.map((v, i) => [
    (i / (values.length - 1)) * 100,
    100 - (v / max) * 84 - 10,
  ]);
  const line = smoothLinePath(pts, 0.5);
  const areaD = areaFromLine(line, pts, 100);
  const gid = `cg-${title}`;
  const last = pts[pts.length - 1];
  const GRID = [0.18, 0.5, 0.82];
  return (
    <PanelTile
      title={title}
      borderStrong
      flex="1"
      bodyPadding="12px"
      headerExtra={
        <span style={{ display: "flex", alignItems: "baseline", gap: 3 }}>
          <span
            style={{
              fontSize: 16,
              fontWeight: 700,
              color: token.textStrong,
              fontFamily: token.mono,
            }}
          >
            {value}
          </span>
          <span style={{ fontSize: 10.5, color: token.textSecondary }}>
            {unit}
          </span>
        </span>
      }
      style={{ minWidth: 0 }}
    >
      <div
        style={{ fontSize: 10, color: token.textSecondary, marginBottom: 6 }}
      >
        avg over 7 days
      </div>
      <div style={{ position: "relative", height: 88 }}>
        <span
          style={{
            position: "absolute",
            left: 0,
            top: 2,
            fontSize: 9,
            color: token.textDim,
            fontFamily: token.mono,
          }}
        >
          {Math.round(maxV)}
        </span>
        <span
          style={{
            position: "absolute",
            left: 0,
            bottom: 14,
            fontSize: 9,
            color: token.textDim,
            fontFamily: token.mono,
          }}
        >
          0
        </span>
        <div
          style={{
            position: "absolute",
            left: 22,
            right: 0,
            top: 0,
            bottom: 16,
          }}
        >
          {GRID.map((f) => (
            <div
              key={f}
              style={{
                position: "absolute",
                left: 0,
                right: 0,
                top: `${f * 100}%`,
                borderTop: `1px solid ${token.grid}`,
              }}
            />
          ))}
          <svg
            viewBox="0 0 100 100"
            preserveAspectRatio="none"
            style={{
              position: "absolute",
              inset: 0,
              width: "100%",
              height: "100%",
            }}
          >
            {area && (
              <>
                <defs>
                  <linearGradient id={gid} x1="0" y1="0" x2="0" y2="1">
                    <stop offset="0%" stopColor={color} stopOpacity={0.32} />
                    <stop offset="100%" stopColor={color} stopOpacity={0} />
                  </linearGradient>
                </defs>
                <path d={areaD} fill={`url(#${gid})`} />
              </>
            )}
            <path
              d={line}
              fill="none"
              stroke={color}
              strokeWidth={1.5}
              vectorEffect="non-scaling-stroke"
            />
          </svg>
          <span
            style={{
              position: "absolute",
              left: `${last[0]}%`,
              top: `${last[1]}%`,
              width: 5,
              height: 5,
              borderRadius: "50%",
              background: color,
              border: `1.5px solid ${token.card}`,
              transform: "translate(-50%,-50%)",
            }}
          />
          <div
            style={{
              position: "absolute",
              left: 0,
              right: 0,
              bottom: -14,
              display: "flex",
              justifyContent: "space-between",
              fontSize: 9,
              color: token.textDim,
            }}
          >
            <span>7d ago</span>
            <span>now</span>
          </div>
        </div>
      </div>
    </PanelTile>
  );
}

function GridCol({
  w,
  children,
  right,
}: {
  w: number | string;
  children: React.ReactNode;
  right?: boolean;
}) {
  return (
    <span
      style={{
        flex: typeof w === "number" ? `0 0 ${w}px` : w,
        textAlign: right ? "right" : "left",
        minWidth: 0,
      }}
    >
      {children}
    </span>
  );
}

function ExpandedOps({ progress }: { progress: MotionValue<number> }) {
  const HEAD = 28;
  const ROWH = 36;
  const OPS_H = HEAD + OPERATIONS.length * ROWH;
  const height = useTransform(
    progress,
    [CLIENT_SELECT, CLIENT_SELECT + 0.05],
    [0, OPS_H],
    { clamp: true },
  );
  const spinOp = useTransform(
    progress,
    [
      CLIENT_SELECT + 0.005,
      CLIENT_SELECT + 0.02,
      CLIENT_SELECT + 0.05,
      CLIENT_SELECT + 0.06,
    ],
    [0, 1, 1, 0],
    { clamp: true },
  );
  const spin = useTransform(
    progress,
    [CLIENT_SELECT, CLIENT_SELECT + 0.06],
    [0, 400],
  );
  const rowsOp = useTransform(
    progress,
    [CLIENT_SELECT + 0.06, CLIENT_SELECT + 0.11],
    [0, 1],
    { clamp: true },
  );
  return (
    <motion.div style={{ height, overflow: "hidden", background: token.bg }}>
      <div style={{ position: "relative", minHeight: OPS_H }}>
        <motion.div
          style={{
            position: "absolute",
            inset: 0,
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            opacity: spinOp,
            pointerEvents: "none",
          }}
        >
          <motion.div style={{ rotate: spin, display: "flex" }}>
            <IconSpinner size={18} color={token.accent} />
          </motion.div>
        </motion.div>
        <motion.div style={{ opacity: rowsOp }}>
          <div
            style={{
              display: "flex",
              alignItems: "center",
              height: HEAD,
              padding: "0 16px 0 44px",
              fontSize: 10,
              fontWeight: 600,
              letterSpacing: 0.5,
              color: token.textSecondary,
              textTransform: "uppercase",
            }}
          >
            <GridCol w="1">Operation</GridCol>
            <GridCol w={96} right>
              Requests
            </GridCol>
            <GridCol w={84} right>
              Latency
            </GridCol>
            <GridCol w={72} right>
              Error
            </GridCol>
          </div>
          {OPERATIONS.map((o) => (
            <div
              key={o.name}
              style={{
                display: "flex",
                alignItems: "center",
                height: ROWH,
                padding: "0 16px 0 44px",
                borderTop: `1px solid ${token.border}`,
              }}
            >
              <GridCol w="1">
                <span style={{ display: "flex", alignItems: "center", gap: 9 }}>
                  <Badge
                    square
                    letter="Q"
                    size="xs"
                    border={token.icQuery}
                    color={token.icQuery}
                  />
                  <span
                    style={{
                      fontSize: 13,
                      color: token.blue,
                      whiteSpace: "nowrap",
                      overflow: "hidden",
                      textOverflow: "ellipsis",
                    }}
                  >
                    {o.name}
                  </span>
                </span>
              </GridCol>
              <GridCol w={96} right>
                <span
                  style={{
                    fontSize: 13,
                    fontFamily: token.mono,
                    color: token.textStrong,
                  }}
                >
                  {o.requests}
                </span>
              </GridCol>
              <GridCol w={84} right>
                <span
                  style={{
                    fontSize: 13,
                    fontFamily: token.mono,
                    color: token.text,
                  }}
                >
                  {o.latency}
                </span>
              </GridCol>
              <GridCol w={72} right>
                <span
                  style={{
                    fontSize: 13,
                    fontFamily: token.mono,
                    color: token.text,
                  }}
                >
                  {o.error}
                </span>
              </GridCol>
            </div>
          ))}
        </motion.div>
      </div>
    </motion.div>
  );
}
