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

const ORANGE = token.graphEdgeActive;

const kindColor = (k: string) =>
  k === "mutation" ? token.icMutation : token.icQuery;
const renderKindGlyph = (k: string, size: number) =>
  k === "mutation" ? <IconMutation size={size} /> : <IconQuery size={size} />;

const TL = timeline([
  { name: "establish", ms: 600 },
  { name: "editorClick", ms: 200 },
  { name: "typeQuery", ms: 2000 },
  { name: "moveToRun", ms: 1400 },
  { name: "runClick", ms: 120 },
  { name: "runLoad", ms: 500 },
  { name: "response", ms: 1600 },
  { name: "moveToPlanTab", ms: 1400 },
  { name: "planTabClick", ms: 120 },
  { name: "planLoad", ms: 500 },
  { name: "planBuild", ms: 700 },
  { name: "planExec", ms: 2400 },
  { name: "zoomIn", ms: 1100 },
  { name: "panPlan", ms: 2600 },
  { name: "zoomOut", ms: 1100 },
  { name: "planDwell", ms: 1100 },
  { name: "viewClick", ms: 130 },
  { name: "subgraphLoad", ms: 500 },
  { name: "subgraphReveal", ms: 4800 },
]);

export const QUERYPLAN_MS = TL.total;
export const QUERYPLAN_TL = TL;

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
  camera?: StageCamera;
}

export function FusionQueryPlanScreen({
  progress,
  showCursor = true,
  camera,
}: FusionQueryPlanScreenProps) {
  const runClick = TL.start("runClick");
  const runDone = TL.end("runLoad");
  const running = useTransform(progress, (p): number =>
    p >= runClick && p < runDone ? 1 : 0,
  );

  const planTabClick = TL.start("planTabClick");
  const subTabAt = (p: number) => (p >= planTabClick ? 1 : 0);
  const [subTab, setSubTab] = useState(() => subTabAt(progress.get()));
  useMotionValueEvent(progress, "change", (p) => setSubTab(subTabAt(p)));

  const viewClick = TL.start("viewClick");
  const viewAt = (p: number) => (p >= viewClick ? 1 : 0);
  const [view, setView] = useState(() => viewAt(progress.get()));
  useMotionValueEvent(progress, "change", (p) => setView(viewAt(p)));

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
        <div
          style={{
            position: "absolute",
            inset: 0,
            display: view === 1 ? "none" : "flex",
          }}
        >
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

          <div style={{ width: 1, background: token.border }} />

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
              <ResponsePane progress={progress} visible={subTab === 0} />
              <PlanPane progress={progress} visible={subTab === 1} />
            </div>
          </div>
        </div>

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

function ResponseHeader({
  progress,
  subTab,
}: {
  progress: MotionValue<number>;
  subTab: number;
}) {
  const statusOpacity = useTransform(
    progress,
    [TL.at("response", 0.1), TL.at("response", 0.4)],
    [0, 1],
    { clamp: true },
  );
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
  const spinnerOpacity = useTransform(
    progress,
    [runClick, TL.at("runLoad", 0.1), TL.at("runLoad", 0.9), runDone],
    [0, 1, 1, 0],
    { clamp: true },
  );
  const spinnerRot = useTransform(progress, [runClick, runDone], [0, 540]);
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

function PlanPane({
  progress,
  visible,
}: {
  progress: MotionValue<number>;
  visible: boolean;
}) {
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

function SubgraphView({ progress }: { progress: MotionValue<number> }) {
  const products = F.nodes.find((n) => n.id === "products");
  const subOp = products?.subOp ?? "";
  const response = products?.response ?? "";
  const variables = products?.viewVariables ?? "";

  const viewClick = TL.start("viewClick");
  const loadEnd = TL.end("subgraphLoad");
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
