/**
 * FusionQueryPlanScreen — Fusion Scene 3: "Compose a query, then see its path."
 *
 * STORY (one continuous developer action): a NEW query document `GetOrderSummary` is open in the
 * Nitro IDE (document chrome, not the gateway view). The developer
 *   1. types the GraphQL query into the Request editor (typed reveal),
 *   2. clicks the green Run split-button → a short spinner,
 *   3. the federated order JSON streams into the Response pane,
 *   4. clicks the "Operation Plan" sub-tab → a short load,
 *   5. the OPERATION PLAN graph builds + executes (root → Orders → Resolve → parallel
 *      Products/Reviews/Accounts) and the cursor rests on the Products lineage at the end.
 *
 * All motion derives from `progress` via useTransform (no internal clocks). Reduced motion freezes
 * at progress=1, so the final frame is the fully-resolved, traced glass-box plan.
 *
 * All motion derives from a STAGE-BASED timeline (`src/lib/timeline.ts`): each interaction is a
 * named stage with its OWN duration in ms, and the screen's total (`QUERYPLAN_MS`) is DERIVED by
 * summing them. No magic-number progress fractions — every window is a `TL.span/start/end` call.
 */
import { useState } from "react";
import {
  motion,
  useMotionValueEvent,
  useTransform,
  type MotionValue,
} from "motion/react";
import { Stage, type StageCamera } from "../../../primitives/reel/Stage";
import { AppFrame } from "../../../primitives/reel/AppFrame";
import { Cursor } from "../../../primitives/reel/Cursor";
import { CodeBlock } from "../../../primitives/CodeBlock";
import { PlanGraph } from "../../../primitives/reel/PlanGraph";
import { TABREEL_CANVAS } from "../../../primitives/reel/TabReel";
import { token } from "../../../lib/tokens";
import { ease } from "../../../lib/motion";
import { timeline } from "../../../lib/timeline";
import { fusionData as F } from "../../../lib/data/tabs";
import {
  IconQuery,
  IconMutation,
  IconRun,
  IconSpinner,
  IconCheck,
  IconWarning,
  IconChevronDown,
  IconFormat,
  IconPlus,
  IconSave,
  IconClose,
  IconApiGateway,
} from "../../../primitives/icons";

const W = TABREEL_CANVAS.w;
const H = TABREEL_CANVAS.h;

// Nitro IDE active-accent (doc tabs / underlines / tree selection) is orange, not the pink token.active.
const ORANGE = token.graphEdgeActive;

const kindColor = (k: string) =>
  k === "mutation" ? token.icMutation : token.icQuery;
const renderKindGlyph = (k: string, size: number) =>
  k === "mutation" ? <IconMutation size={size} /> : <IconQuery size={size} />;

/* ── STAGE-BASED timeline: each interaction owns its ms; the total is DERIVED ──
 * Flow: establish → editorClick → typeQuery → moveToRun → runClick → runLoad → response →
 *       moveToPlanTab → planTabClick → planLoad → planBuild → planExec →
 *       zoomIn → panPlan → zoomOut → planDwell → viewClick → subgraphLoad → subgraphReveal.
 * The editor is EMPTY through establish+editorClick; typing only begins at typeQuery.
 * The camera (zoomIn/panPlan/zoomOut) is driven by the PARENT, not here.
 * Cursor MOVE stages are deliberately generous (the slow glides between targets). */
const TL = timeline([
  { name: "establish", ms: 600 }, // settle on the empty query doc
  { name: "editorClick", ms: 200 }, // cursor clicks INTO the empty editor to focus it
  { name: "typeQuery", ms: 2000 }, // THEN the query types into the Request editor
  { name: "moveToRun", ms: 1400 }, // editor → Run split-button (slow glide)
  { name: "runClick", ms: 120 }, // click Run
  { name: "runLoad", ms: 500 }, // in-flight spinner
  { name: "response", ms: 1600 }, // federated JSON streams into the Response pane
  { name: "moveToPlanTab", ms: 1400 }, // Run → Operation Plan sub-tab (slow glide)
  { name: "planTabClick", ms: 120 }, // click Operation Plan
  { name: "planLoad", ms: 500 }, // plan-tab load spinner
  { name: "planBuild", ms: 700 }, // graph builds (nodes/edges draw in)
  { name: "planExec", ms: 2400 }, // exec sweep root → Orders → parallel fetches
  { name: "zoomIn", ms: 1100 }, // camera zooms into the plan (driven by the parent)
  { name: "panPlan", ms: 2600 }, // camera pans along the plan
  { name: "zoomOut", ms: 1100 }, // camera zooms back out
  { name: "planDwell", ms: 1100 }, // rest on the whole traced plan (Products lineage isolates)
  { name: "viewClick", ms: 130 }, // cursor clicks "View Raw Data" on the Products node
  { name: "subgraphLoad", ms: 500 }, // brief load
  { name: "subgraphReveal", ms: 4800 }, // the subgraph request+response reveals & rests
]);

/** DERIVED total duration in ms — feed to SoloScreen / the reel tab. */
export const QUERYPLAN_MS = TL.total;
export const QUERYPLAN_TL = TL;

// A realistic GetOrderSummary response (matches fusionData.rootOp shape: order/items/customer).
const RESPONSE = `{
  "data": {
    "order": {
      "id": "ord_8F2KQ7",
      "total": 129.97,
      "items": [
        { "product": { "id": "prod_KB01", "name": "Mechanical Keyboard", "price": 89.99 }, "quantity": 1 },
        { "product": { "id": "prod_UC02", "name": "USB-C Cable", "price": 19.99 }, "quantity": 2 }
      ],
      "customer": { "id": "acct_31", "name": "Ada Lovelace", "email": "ada@eshops.io" }
    }
  }
}`;

export interface FusionQueryPlanScreenProps {
  progress: MotionValue<number>;
  active?: boolean;
  showCursor?: boolean;
  /** pan/zoom camera supplied by the parent sequencer; undefined = identity (standalone story). */
  camera?: StageCamera;
}

export function FusionQueryPlanScreen({
  progress,
  showCursor = true,
  camera,
}: FusionQueryPlanScreenProps) {
  // run lifecycle: spinner runs from the Run click through the in-flight load.
  const runClick = TL.start("runClick");
  const runDone = TL.end("runLoad");
  const running = useTransform(progress, (p): number =>
    p >= runClick && p < runDone ? 1 : 0,
  );

  // which sub-tab is active in the Response column: 0 = Response (JSON), 1 = Operation Plan
  const planTabClick = TL.start("planTabClick");
  const subTabAt = (p: number) => (p >= planTabClick ? 1 : 0);
  const [subTab, setSubTab] = useState(() => subTabAt(progress.get()));
  useMotionValueEvent(progress, "change", (p) => setSubTab(subTabAt(p)));

  // which document is open: 0 = GetOrderSummary (request/response/plan), 1 = the "Fetch from
  // Products" subgraph request doc that opens once the developer clicks "View Raw Data".
  const viewClick = TL.start("viewClick");
  const viewAt = (p: number) => (p >= viewClick ? 1 : 0);
  const [view, setView] = useState(() => viewAt(progress.get()));
  useMotionValueEvent(progress, "change", (p) => setView(viewAt(p)));

  // cursor: rest → click INTO the empty editor → (typing) → Run button → settle → Operation Plan
  // sub-tab → Products node, where it RESTS through the camera zoom/pan + dwell.
  // Coordinates (canvas px), aimed at the full-width-from-rail layout:
  //   · click-to-focus inside the empty Request editor (left column)  ~x 300, y 210
  //   · Run split-button at the Request column header right            ~x 620, y 64
  //   · "Operation Plan" sub-tab in the Response header                ~x 830, y 64
  //   · Products rank-3 fetch node (plan graph full width)              x 1198, y 335 — CALIBRATED, do not change
  // The MOVE stages (moveToRun, moveToPlanTab) own generous ms, so the glides are slow and
  // unhurried; a long ease keeps the pointer drifting with no whip. After reaching Products the
  // cursor is parked while the parent's camera drives zoomIn/panPlan/zoomOut and the final dwell.
  const cx = useTransform(
    progress,
    [
      TL.start("establish"),
      TL.start("editorClick"),
      TL.start("moveToRun"),
      TL.start("runClick"),
      TL.end("runLoad"),
      TL.at("response", 0.5),
      TL.start("planTabClick"),
      TL.end("planLoad"),
      TL.start("zoomIn"),
      TL.start("planDwell"),
      TL.start("viewClick"),
      1,
    ],
    [220, 300, 320, 620, 620, 690, 830, 880, 1198, 1198, 710, 710],
    { ease: ease.glide },
  );
  const cy = useTransform(
    progress,
    [
      TL.start("establish"),
      TL.start("editorClick"),
      TL.start("moveToRun"),
      TL.start("runClick"),
      TL.end("runLoad"),
      TL.at("response", 0.5),
      TL.start("planTabClick"),
      TL.end("planLoad"),
      TL.start("zoomIn"),
      TL.start("planDwell"),
      TL.start("viewClick"),
      1,
    ],
    [230, 210, 230, 64, 64, 95, 64, 150, 335, 335, 453, 453],
    { ease: ease.glide },
  );

  // when the Operation Plan tab is active the Request column collapses so the plan graph gets the
  // full result width (matching the standalone plan framing) — the deepest fetch nodes then fit.
  const reqWidth = useTransform(
    progress,
    TL.span("planTabClick"),
    ["44%", "0%"],
    { clamp: true },
  );

  return (
    <Stage
      width={W}
      height={H}
      fit="fill"
      chrome={false}
      camera={camera}
      ariaLabel="Nitro Fusion — composing a GetOrderSummary query and revealing its operation plan"
      overlay={
        showCursor ? (
          <Cursor
            x={cx}
            y={cy}
            progress={progress}
            clickTimes={[
              TL.start("editorClick"),
              TL.start("runClick"),
              TL.start("planTabClick"),
              TL.start("viewClick"),
            ]}
            pointerWindows={[[TL.start("zoomIn"), 1]]}
          />
        ) : null
      }
    >
      <AppFrame railActive="documents" toolbar={<DocTabs view={view} />}>
        {/* GetOrderSummary document (request / response / operation-plan). Hidden once the
            developer opens the subgraph request doc, but kept mounted so the plan phase holds. */}
        <div
          style={{
            position: "absolute",
            inset: 0,
            display: view === 1 ? "none" : "flex",
          }}
        >
          {/* Request column (collapses when the Operation Plan tab is active) */}
          <motion.div
            style={{
              width: reqWidth,
              display: "flex",
              flexDirection: "column",
              minWidth: 0,
              overflow: "hidden",
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
              <CodeBlock
                code={F.rootOp}
                lang="graphql"
                progress={progress}
                playWindow={TL.span("typeQuery")}
                gutter
                caret
                fontSize={13}
                ariaLabel="GraphQL request editor"
                style={{ position: "absolute", inset: 0 }}
              />
            </div>
            {/* variables sub-pane — statically present */}
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
              <div style={{ height: 56, overflow: "hidden" }}>
                <CodeBlock
                  code={`{\n  "id": "ord_8F2KQ7"\n}`}
                  lang="json"
                  gutter
                  caret={false}
                  fontSize={12}
                />
              </div>
            </div>
          </motion.div>

          {/* separator */}
          <div style={{ width: 1, background: token.border }} />

          {/* Response column */}
          <div
            style={{
              flex: 1,
              display: "flex",
              flexDirection: "column",
              minWidth: 0,
              borderLeft: `1px solid ${token.surface}`,
            }}
          >
            <ResponseHeader progress={progress} subTab={subTab} />
            <div
              style={{
                flex: 1,
                minHeight: 0,
                overflow: "hidden",
                position: "relative",
              }}
            >
              {/* JSON response pane */}
              <ResponsePane progress={progress} visible={subTab === 0} />
              {/* Operation Plan pane */}
              <PlanPane progress={progress} visible={subTab === 1} />
            </div>
          </div>
        </div>

        {/* Subgraph request document — "Fetch from Products": its request + response. */}
        <div
          style={{
            position: "absolute",
            inset: 0,
            display: view === 1 ? "block" : "none",
          }}
        >
          <SubgraphView progress={progress} />
        </div>
      </AppFrame>
    </Stage>
  );
}

/* ── Response column header: Run status + the Response | Operation Plan sub-tabs ── */

function ResponseHeader({
  progress,
  subTab,
}: {
  progress: MotionValue<number>;
  subTab: number;
}) {
  // 200 · ms · KB status fades in just as the JSON begins to stream
  const statusOpacity = useTransform(
    progress,
    [TL.at("response", 0.1), TL.at("response", 0.4)],
    [0, 1],
    { clamp: true },
  );
  // plan-summary status (Success · 1 partial · 41 ms · 7 nodes) resolves once the exec sweep ends
  const planStatusOpacity = useTransform(
    progress,
    [TL.end("planExec"), TL.at("zoomIn", 0.1)],
    [0, 1],
    { clamp: true },
  );

  return (
    <div
      style={{
        height: 36,
        flex: "0 0 auto",
        display: "flex",
        alignItems: "center",
        gap: 16,
        padding: "0 12px",
        borderBottom: `1px solid ${token.border}`,
      }}
    >
      <SubTab label="Response" active={subTab === 0} />
      <SubTab
        label="Operation Plan"
        active={subTab === 1}
        testid="plan-subtab"
      />
      <div
        style={{ marginLeft: "auto", display: "flex", alignItems: "center" }}
      >
        {subTab === 0 ? (
          <motion.div
            style={{
              display: "flex",
              alignItems: "center",
              gap: 12,
              opacity: statusOpacity,
            }}
          >
            <span style={{ display: "flex", alignItems: "center", gap: 5 }}>
              <IconCheck size={14} />
              <span
                style={{
                  fontSize: 12.5,
                  fontWeight: 700,
                  color: token.successText,
                }}
              >
                200
              </span>
            </span>
            <span
              style={{
                fontSize: 12,
                fontFamily: token.mono,
                color: token.textSecondary,
              }}
            >
              38 ms
            </span>
            <span
              style={{
                fontSize: 12,
                fontFamily: token.mono,
                color: token.textSecondary,
              }}
            >
              1.1 KB
            </span>
          </motion.div>
        ) : (
          <motion.div
            style={{
              display: "flex",
              alignItems: "center",
              gap: 12,
              opacity: planStatusOpacity,
            }}
          >
            <span style={{ display: "flex", alignItems: "center", gap: 5 }}>
              <IconCheck size={14} />
              <span style={{ fontSize: 12.5, color: token.text }}>Success</span>
            </span>
            <span
              style={{
                display: "flex",
                alignItems: "center",
                gap: 5,
                fontSize: 12,
                color: token.warning,
              }}
            >
              <IconWarning size={12} /> 1 partial
            </span>
            <span
              style={{
                fontSize: 12,
                fontFamily: token.mono,
                color: token.textSecondary,
              }}
            >
              41 ms
            </span>
            <span style={{ fontSize: 12, color: token.textSecondary }}>
              7 nodes
            </span>
          </motion.div>
        )}
      </div>
    </div>
  );
}

function SubTab({
  label,
  active,
  testid,
}: {
  label: string;
  active?: boolean;
  testid?: string;
}) {
  return (
    <span
      data-testid={testid}
      style={{
        position: "relative",
        fontSize: 13.5,
        fontWeight: active ? 600 : 400,
        color: active ? token.textStrong : token.textSecondary,
        height: 36,
        display: "flex",
        alignItems: "center",
      }}
    >
      {label}
      {active && (
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
      )}
    </span>
  );
}

/* ── JSON response pane (streams after Run) ── */

function ResponsePane({
  progress,
  visible,
}: {
  progress: MotionValue<number>;
  visible: boolean;
}) {
  const runClick = TL.start("runClick");
  const runDone = TL.end("runLoad");
  const responseOpacity = useTransform(
    progress,
    [TL.start("response"), TL.at("response", 0.1)],
    [0, 1],
    { clamp: true },
  );
  // spinner stands in for the in-flight request (runLoad), before the JSON streams
  const spinnerOpacity = useTransform(
    progress,
    [runClick, TL.at("runLoad", 0.1), TL.at("runLoad", 0.9), runDone],
    [0, 1, 1, 0],
    { clamp: true },
  );
  const spinnerRot = useTransform(progress, [runClick, runDone], [0, 540]);
  // visible until Run is clicked, then fades out as the in-flight spinner takes over
  const emptyOpacity = useTransform(
    progress,
    [runClick, TL.end("runClick")],
    [1, 0],
    { clamp: true },
  );

  return (
    <div
      style={{
        position: "absolute",
        inset: 0,
        display: visible ? "block" : "none",
      }}
    >
      <motion.div
        style={{ position: "absolute", inset: 0, opacity: responseOpacity }}
      >
        <CodeBlock
          code={RESPONSE}
          lang="json"
          progress={progress}
          playWindow={TL.span("response")}
          gutter
          caret={false}
          fontSize={12.5}
          ariaLabel="JSON response"
          style={{ position: "absolute", inset: 0 }}
        />
      </motion.div>
      <motion.div
        style={{
          position: "absolute",
          inset: 0,
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          opacity: emptyOpacity,
          color: token.textSecondary,
          fontSize: 13,
          pointerEvents: "none",
        }}
      >
        No responses yet
      </motion.div>
      <motion.div
        style={{
          position: "absolute",
          inset: 0,
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          opacity: spinnerOpacity,
          pointerEvents: "none",
        }}
      >
        <motion.div style={{ rotate: spinnerRot, display: "flex" }}>
          <IconSpinner size={26} color={token.accent} />
        </motion.div>
      </motion.div>
    </div>
  );
}

/* ── Operation Plan pane (sub-tab load → graph builds/executes/hovers) ── */

function PlanPane({
  progress,
  visible,
}: {
  progress: MotionValue<number>;
  visible: boolean;
}) {
  // a short spinner stands in for computing/fetching the plan (planLoad), BEFORE the graph appears
  const planTabClick = TL.start("planTabClick");
  const planLoadEnd = TL.end("planLoad");
  const spinnerOpacity = useTransform(
    progress,
    [planTabClick, TL.at("planLoad", 0.1), TL.at("planLoad", 0.9), planLoadEnd],
    [0, 1, 1, 0],
    { clamp: true },
  );
  const spinnerRot = useTransform(
    progress,
    [planTabClick, planLoadEnd],
    [0, 540],
  );
  const graphOpacity = useTransform(
    progress,
    [TL.at("planLoad", 0.9), TL.start("planBuild")],
    [0, 1],
    { clamp: true },
  );

  return (
    <div
      style={{
        position: "absolute",
        inset: 0,
        display: visible ? "block" : "none",
      }}
    >
      <motion.div
        style={{ position: "absolute", inset: 0, opacity: graphOpacity }}
      >
        <PlanGraph
          nodes={F.nodes}
          edges={F.edges}
          progress={progress}
          hoverId="products"
          revealStart={TL.start("planBuild")}
          revealEnd={TL.end("planExec")}
          hoverStart={TL.start("planDwell")}
          hoverEnd={TL.end("planDwell")}
          fitScale={0.78}
        />
      </motion.div>
      <motion.div
        style={{
          position: "absolute",
          inset: 0,
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          opacity: spinnerOpacity,
          pointerEvents: "none",
          background: token.graphCanvas,
        }}
      >
        <motion.div style={{ rotate: spinnerRot, display: "flex" }}>
          <IconSpinner size={28} color={token.accent} />
        </motion.div>
      </motion.div>
    </div>
  );
}

/* ── chrome helpers ── */

function ColumnHeader({
  title,
  children,
}: {
  title?: string;
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
    r ? "Cancel" : "Run GetOrderSummary",
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

/* ── toolbar / tree ── */

// The "+" just opened a NEW query document tab: the existing gateway doc (inactive) sits to the
// left, the freshly-opened GetOrderSummary query tab (active, with a close ×) sits beside it.
// view 0: [EShops Gateway] [GetOrderSummary*]   — the query doc is active.
// view 1: [EShops Gateway] [GetOrderSummary] [Fetch from Products* ×] — clicking "View Raw Data"
//         opened the subgraph request doc, which becomes the active tab.
function DocTabs({ view }: { view: number }) {
  return (
    <div
      style={{
        display: "flex",
        alignItems: "flex-end",
        gap: 6,
        height: "100%",
      }}
    >
      <DocTab
        name="EShops Gateway"
        glyph={<IconApiGateway size={12} />}
        glyphColor={token.icObject}
      />
      <DocTab
        name="GetOrderSummary"
        glyph={renderKindGlyph("query", 12)}
        glyphColor={kindColor("query")}
        activeTab={view === 0}
        closable
      />
      {view === 1 && (
        <DocTab
          name="Fetch from Products"
          glyph={renderKindGlyph("query", 12)}
          glyphColor={kindColor("query")}
          activeTab
          closable
        />
      )}
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

/* ── Subgraph request document ("Fetch from Products"): request | response split ── */

function SubgraphView({ progress }: { progress: MotionValue<number> }) {
  const products = F.nodes.find((n) => n.id === "products");
  const subOp = products?.subOp ?? "";
  const response = products?.response ?? "";
  const variables = products?.viewVariables ?? "";

  const viewClick = TL.start("viewClick");
  const loadEnd = TL.end("subgraphLoad");
  // brief spinner over subgraphLoad, then the request + response reveal over subgraphReveal.
  const spinnerOpacity = useTransform(
    progress,
    [
      viewClick,
      TL.at("subgraphLoad", 0.1),
      TL.at("subgraphLoad", 0.9),
      loadEnd,
    ],
    [0, 1, 1, 0],
    { clamp: true },
  );
  const spinnerRot = useTransform(progress, [viewClick, loadEnd], [0, 540]);
  const contentOpacity = useTransform(
    progress,
    [TL.at("subgraphLoad", 0.9), TL.start("subgraphReveal")],
    [0, 1],
    { clamp: true },
  );

  return (
    <div style={{ position: "absolute", inset: 0 }}>
      <motion.div
        style={{
          position: "absolute",
          inset: 0,
          display: "flex",
          flexDirection: "column",
          opacity: contentOpacity,
        }}
      >
        {/* header label */}
        <div
          style={{
            height: 36,
            flex: "0 0 auto",
            display: "flex",
            alignItems: "center",
            gap: 8,
            padding: "0 12px",
            borderBottom: `1px solid ${token.border}`,
          }}
        >
          <span style={{ display: "flex", color: kindColor("query") }}>
            {renderKindGlyph("query", 13)}
          </span>
          <span
            style={{ fontSize: 13.5, fontWeight: 600, color: token.textStrong }}
          >
            Subgraph Request · Products
          </span>
        </div>
        <div style={{ flex: 1, minHeight: 0, display: "flex" }}>
          {/* Request pane (single-entity query) + the batched Variables sub-pane */}
          <div
            style={{
              width: "44%",
              display: "flex",
              flexDirection: "column",
              minWidth: 0,
            }}
          >
            <ColumnHeader title="Request" />
            <div
              style={{
                flex: 1,
                minHeight: 0,
                overflow: "hidden",
                position: "relative",
              }}
            >
              <CodeBlock
                code={subOp}
                lang="graphql"
                progress={progress}
                playWindow={[
                  TL.start("subgraphReveal"),
                  TL.at("subgraphReveal", 0.45),
                ]}
                gutter
                caret={false}
                fontSize={13}
                ariaLabel="Subgraph request"
                style={{ position: "absolute", inset: 0 }}
              />
            </div>
            {/* variable batching: an ARRAY of variable sets */}
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
                  gap: 8,
                  padding: "0 12px",
                  borderBottom: `1px solid ${token.border}`,
                  fontSize: 12,
                }}
              >
                <span style={{ position: "relative", color: token.textStrong }}>
                  Variables
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
                <span
                  style={{
                    fontSize: 10.5,
                    color: token.textSecondary,
                    border: `1px solid ${token.border}`,
                    borderRadius: 4,
                    padding: "1px 6px",
                  }}
                >
                  batched · 2
                </span>
              </div>
              <div
                style={{ height: 88, overflow: "hidden", position: "relative" }}
              >
                <CodeBlock
                  code={variables}
                  lang="json"
                  progress={progress}
                  playWindow={[
                    TL.start("subgraphReveal"),
                    TL.at("subgraphReveal", 0.45),
                  ]}
                  gutter
                  caret={false}
                  fontSize={12}
                  ariaLabel="Batched variables"
                  style={{ position: "absolute", inset: 0 }}
                />
              </div>
            </div>
          </div>
          <div style={{ width: 1, background: token.border }} />
          {/* Response pane */}
          <div
            style={{
              flex: 1,
              display: "flex",
              flexDirection: "column",
              minWidth: 0,
            }}
          >
            <ColumnHeader title="Response" />
            <div
              style={{
                flex: 1,
                minHeight: 0,
                overflow: "hidden",
                position: "relative",
              }}
            >
              <CodeBlock
                code={response}
                lang="json"
                progress={progress}
                playWindow={[
                  TL.start("subgraphReveal"),
                  TL.at("subgraphReveal", 0.45),
                ]}
                gutter
                caret={false}
                fontSize={12.5}
                ariaLabel="Subgraph response"
                style={{ position: "absolute", inset: 0 }}
              />
            </div>
          </div>
        </div>
      </motion.div>
      {/* brief load spinner */}
      <motion.div
        style={{
          position: "absolute",
          inset: 0,
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          opacity: spinnerOpacity,
          pointerEvents: "none",
          background: token.surface,
        }}
      >
        <motion.div style={{ rotate: spinnerRot, display: "flex" }}>
          <IconSpinner size={28} color={token.accent} />
        </motion.div>
      </motion.div>
    </div>
  );
}
function DocTab({
  name,
  glyph,
  glyphColor,
  activeTab,
  closable,
}: {
  name: string;
  glyph: React.ReactNode;
  glyphColor: string;
  activeTab?: boolean;
  closable?: boolean;
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
        maxWidth: 190,
      }}
    >
      <span style={{ display: "flex", color: glyphColor }}>{glyph}</span>
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
      {closable && (
        <span style={{ color: token.textSecondary, display: "flex" }}>
          <IconClose size={11} color="currentColor" />
        </span>
      )}
    </div>
  );
}
