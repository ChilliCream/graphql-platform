"use client";

import React, {
  FC,
  useMemo,
  useState,
  useEffect,
  useRef,
  useCallback,
} from "react";
import {
  ReactFlowProvider,
  useReactFlow,
  useStore,
  type Node,
  type Edge,
} from "@xyflow/react";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import type { IconDefinition } from "@fortawesome/fontawesome-svg-core";
import {
  faEnvelope,
  faArrowRightArrowLeft,
  faGear,
  faDiagramProject,
  faRightLeft,
  faLayerGroup,
  faHashtag,
  faCubes,
  faRoute,
  faLink,
  faEllipsis,
} from "@fortawesome/free-solid-svg-icons";
import {
  TopologyFlow,
  type DiagramData,
  type MessageTrace,
  type MessageActivity,
  type TraceTopologyMapping,
  type SidebarTab,
} from "@chillicream/mocha-visualizer";

/**
 * Minimal scoped styles for embedding the visualizer in the docs page.
 *
 * Unlike GlobalStyles from the mocha-visualizer package, this does NOT
 * touch html/body/#root — which would override the docs page layout and
 * cause ReactFlow to measure incorrect container dimensions.
 */
const ScopedStyles = () => (
  <style>{`
    .react-flow div,
    .react-flow span {
      overflow: visible;
    }
    .react-flow__node {
      font-family: inherit;
      overflow: visible;
    }
    .react-flow__edge-path {
      stroke-width: 1.5;
    }
    .react-flow__handle {
      width: 1px;
      height: 1px;
      min-width: 0;
      min-height: 0;
      background: transparent;
      border: none;
      opacity: 0;
      pointer-events: none;
    }
  `}</style>
);

// ---------------------------------------------------------------------------
// Trace step list – detailed sidebar panel
// ---------------------------------------------------------------------------

interface TraceStepListProps {
  trace: MessageTrace;
  activeActivityId: string | null;
  focusedNodeId: string | null;
  onActivityClick: (activity: MessageActivity) => void;
  onNodeClick: (nodeId: string) => void;
  traceMapping: TraceTopologyMapping | null;
  nodes: Node[];
  edges: Edge[];
}

const TIMELINE_LEFT = 23;

const ts = {
  container: {
    width: "100%",
    height: "100%",
    background: "#0d1117",
    flexShrink: 0,
    display: "flex",
    flexDirection: "column" as const,
  },
  header: {
    padding: "12px 14px",
    fontSize: 12,
    fontWeight: 600,
    color: "#8b949e",
    textTransform: "uppercase" as const,
    letterSpacing: "0.5px",
    borderBottom: "1px solid #21262d",
  },
  list: {
    flex: 1,
    overflowY: "auto" as const,
    padding: "4px 0",
  },
  row: (active: boolean, hasError: boolean) => ({
    display: "flex",
    alignItems: "flex-start" as const,
    gap: 10,
    padding: "8px 14px",
    cursor: "pointer",
    background: active ? "rgba(88, 166, 255, 0.1)" : "transparent",
    borderLeft: active ? "2px solid #58a6ff" : "2px solid transparent",
    transition: "background 0.1s ease",
    position: "relative" as const,
    ...(hasError && !active ? { background: "rgba(248, 81, 73, 0.05)" } : {}),
  }),
  badge: {
    width: 20,
    height: 20,
    borderRadius: "50%",
    background: "#58a6ff",
    color: "#fff",
    fontSize: 10,
    fontWeight: 700,
    display: "flex",
    alignItems: "center",
    justifyContent: "center",
    flexShrink: 0,
    marginTop: 1,
    zIndex: 1,
  },
  icon: (color: string) => ({
    width: 14,
    fontSize: 11,
    color,
    flexShrink: 0,
    marginTop: 3,
  }),
  content: {
    flex: 1,
    minWidth: 0,
    display: "flex",
    flexDirection: "column" as const,
    gap: 2,
  },
  topRow: {
    display: "flex",
    alignItems: "center",
    gap: 6,
  },
  label: {
    flex: 1,
    fontSize: 12,
    color: "#c9d1d9",
    overflow: "hidden",
    textOverflow: "ellipsis",
    whiteSpace: "nowrap" as const,
  },
  errorDot: {
    width: 8,
    height: 8,
    borderRadius: "50%",
    background: "#f85149",
    flexShrink: 0,
  },
  bottomRow: {
    display: "flex",
    alignItems: "center",
    gap: 6,
    fontSize: 11,
    color: "#6e7681",
    fontFamily: "monospace",
  },
  offset: {
    color: "#8b949e",
    fontSize: 10,
  },
  connector: {
    width: 0,
    height: 6,
    borderLeft: "1px dotted #30363d",
    marginLeft: TIMELINE_LEFT,
  },
  inferredRow: (active: boolean) => ({
    display: "flex",
    alignItems: "center",
    gap: 8,
    padding: "2px 14px 2px 14px",
    cursor: "pointer",
    background: active ? "rgba(88, 166, 255, 0.08)" : "transparent",
    transition: "background 0.1s ease",
    position: "relative" as const,
  }),
  inferredDot: (color: string) => ({
    width: 8,
    height: 8,
    borderRadius: "50%",
    border: `1.5px solid ${color}`,
    background: "transparent",
    flexShrink: 0,
    marginLeft: 6,
    marginRight: 6,
    zIndex: 1,
  }),
  inferredIcon: (color: string) => ({
    width: 12,
    fontSize: 10,
    color,
    opacity: 0.7,
    flexShrink: 0,
  }),
  inferredLabel: {
    flex: 1,
    fontSize: 11,
    color: "#484f58",
    overflow: "hidden",
    textOverflow: "ellipsis",
    whiteSpace: "nowrap" as const,
  },
} as const;

function getOperationIcon(activity: MessageActivity): {
  icon: IconDefinition;
  color: string;
} {
  switch (activity.operation) {
    case "publish":
    case "send":
    case "request":
    case "reply":
    case "subscribe":
      return { icon: faEnvelope, color: "#d29922" };
    case "dispatch":
      return { icon: faArrowRightArrowLeft, color: "#db61a2" };
    case "receive":
      return { icon: faArrowRightArrowLeft, color: "#58a6ff" };
    case "consume":
      return { icon: faGear, color: "#3fb950" };
    case "saga-transition":
      return { icon: faDiagramProject, color: "#a371f7" };
  }
}

function getOperationLabel(activity: MessageActivity): string {
  switch (activity.operation) {
    case "publish":
      return `Publish ${activity.messageType}`;
    case "send":
      return `Send ${activity.messageType}`;
    case "request":
      return `Request ${activity.messageType}`;
    case "dispatch":
      return `Dispatch \u2192 ${activity.endpointName}`;
    case "receive":
      return `Receive \u2190 ${activity.endpointName}`;
    case "consume":
      return `Consume ${activity.consumerName}`;
    case "saga-transition":
      return `Saga ${activity.sagaName}`;
    case "reply":
      return `Reply ${activity.messageType}`;
    case "subscribe":
      return `Subscribe ${activity.messageType}`;
  }
}

function getNodeIcon(node: Node): {
  icon: IconDefinition;
  color: string;
  label: string;
} {
  const data = node.data as Record<string, unknown>;
  const nodeType = data.nodeType as string | undefined;
  const entityKind = data.entityKind as string | undefined;
  const label = (data.label as string) || node.id;

  if (nodeType === "entity") {
    if (entityKind === "exchange")
      return { icon: faRightLeft, color: "#d29922", label };
    if (entityKind === "queue")
      return { icon: faLayerGroup, color: "#3fb950", label };
    if (entityKind === "topic")
      return { icon: faHashtag, color: "#58a6ff", label };
    return { icon: faCubes, color: "#a371f7", label };
  }
  if (nodeType === "route") return { icon: faRoute, color: "#6e7681", label };
  if (nodeType === "binding") return { icon: faLink, color: "#6e7681", label };
  if (nodeType === "endpoint") {
    const subType = data.subType as string | undefined;
    return {
      icon: faArrowRightArrowLeft,
      color: subType === "receive" ? "#58a6ff" : "#db61a2",
      label,
    };
  }
  if (nodeType === "consumer") return { icon: faGear, color: "#3fb950", label };
  if (nodeType === "message")
    return { icon: faEnvelope, color: "#d29922", label };
  if (nodeType === "saga")
    return { icon: faDiagramProject, color: "#a371f7", label };
  return { icon: faEllipsis, color: "#484f58", label };
}

function formatDuration(ms: number): string {
  if (ms < 1) return "<1ms";
  if (ms < 1000) return `${Math.round(ms)}ms`;
  return `${(ms / 1000).toFixed(1)}s`;
}

function formatTime(iso: string): string {
  const d = new Date(iso);
  const h = d.getUTCHours().toString().padStart(2, "0");
  const m = d.getUTCMinutes().toString().padStart(2, "0");
  const sec = d.getUTCSeconds().toString().padStart(2, "0");
  const ms = d.getUTCMilliseconds().toString().padStart(3, "0");
  return `${h}:${m}:${sec}.${ms}`;
}

function formatOffset(ms: number): string {
  if (ms === 0) return "";
  if (ms < 1000) return `+${Math.round(ms)}ms`;
  return `+${(ms / 1000).toFixed(1)}s`;
}

function findInferredNodesBetween(
  sourceNodeId: string,
  targetNodeId: string,
  traceMapping: TraceTopologyMapping,
  allNodes: Node[],
  allEdges: Edge[]
): Node[] {
  if (!sourceNodeId || !targetNodeId || sourceNodeId === targetNodeId)
    return [];

  const traceNodeIds = traceMapping.nodeIds;
  const visited = new Set<string>([sourceNodeId]);
  const queue: { id: string; path: string[] }[] = [
    { id: sourceNodeId, path: [] },
  ];

  while (queue.length > 0) {
    const current = queue.shift()!;
    for (const edge of allEdges) {
      if (edge.source !== current.id) continue;
      const next = edge.target;
      if (visited.has(next)) continue;
      visited.add(next);

      if (next === targetNodeId) {
        return current.path
          .map((id) => allNodes.find((n) => n.id === id))
          .filter((n): n is Node => !!n);
      }
      if (traceNodeIds.has(next)) {
        queue.push({ id: next, path: [...current.path, next] });
      }
    }
  }
  return [];
}

function TraceStepList({
  trace,
  activeActivityId,
  focusedNodeId,
  onActivityClick,
  onNodeClick,
  traceMapping,
  nodes,
  edges,
}: TraceStepListProps) {
  const sorted = [...trace.activities].sort(
    (a, b) => new Date(a.startTime).getTime() - new Date(b.startTime).getTime()
  );

  const timestamps = sorted.map((a) => new Date(a.startTime).getTime());

  const inferredBetween: Node[][] = [];
  if (traceMapping && edges.length > 0) {
    for (let i = 0; i < sorted.length - 1; i++) {
      const sourceNodeId = traceMapping.activityToNodeId.get(sorted[i].id);
      const targetNodeId = traceMapping.activityToNodeId.get(sorted[i + 1].id);
      if (sourceNodeId && targetNodeId && sourceNodeId !== targetNodeId) {
        inferredBetween.push(
          findInferredNodesBetween(
            sourceNodeId,
            targetNodeId,
            traceMapping,
            nodes,
            edges
          )
        );
      } else {
        inferredBetween.push([]);
      }
    }
  }

  type ListItem =
    | { kind: "activity"; activity: MessageActivity; index: number }
    | { kind: "inferred"; node: Node }
    | { kind: "connector" };

  const items: ListItem[] = [];
  for (let i = 0; i < sorted.length; i++) {
    if (i > 0) items.push({ kind: "connector" });
    items.push({ kind: "activity", activity: sorted[i], index: i });

    const inferred = inferredBetween[i] ?? [];
    for (const node of inferred) {
      items.push({ kind: "connector" });
      items.push({ kind: "inferred", node });
    }
  }

  return (
    <div style={ts.container}>
      <div style={ts.header}>Trace Steps</div>
      <div style={ts.list}>
        {items.map((item, idx) => {
          if (item.kind === "connector") {
            return <div key={`c-${idx}`} style={ts.connector} />;
          }

          if (item.kind === "inferred") {
            const info = getNodeIcon(item.node);
            const isActive = focusedNodeId === item.node.id;
            return (
              <div
                key={item.node.id}
                style={ts.inferredRow(isActive)}
                onClick={() => onNodeClick(item.node.id)}
                onMouseEnter={(e) => {
                  if (!isActive)
                    (e.currentTarget as HTMLElement).style.background =
                      "rgba(88, 166, 255, 0.05)";
                }}
                onMouseLeave={(e) => {
                  if (!isActive)
                    (e.currentTarget as HTMLElement).style.background =
                      "transparent";
                }}
              >
                <div style={ts.inferredDot(info.color)} />
                <span style={ts.inferredIcon(info.color)}>
                  <FontAwesomeIcon icon={info.icon} />
                </span>
                <span style={ts.inferredLabel} title={info.label}>
                  {info.label}
                </span>
              </div>
            );
          }

          const { activity, index } = item;
          const isActive = activity.id === activeActivityId;
          const hasError = activity.status === "error";
          const offset =
            index > 0 ? timestamps[index] - timestamps[index - 1] : 0;
          const { icon, color } = getOperationIcon(activity);

          return (
            <div
              key={activity.id}
              style={ts.row(isActive, hasError)}
              onClick={() => onActivityClick(activity)}
              onMouseEnter={(e) => {
                if (!isActive)
                  (e.currentTarget as HTMLElement).style.background =
                    "rgba(88, 166, 255, 0.06)";
              }}
              onMouseLeave={(e) => {
                if (!isActive)
                  (e.currentTarget as HTMLElement).style.background = hasError
                    ? "rgba(248, 81, 73, 0.05)"
                    : "transparent";
              }}
            >
              <div style={ts.badge}>{index + 1}</div>
              <span style={ts.icon(color)}>
                <FontAwesomeIcon icon={icon} />
              </span>
              <div style={ts.content}>
                <div style={ts.topRow}>
                  <span style={ts.label} title={getOperationLabel(activity)}>
                    {getOperationLabel(activity)}
                  </span>
                  {hasError && <div style={ts.errorDot} title="Error" />}
                </div>
                <div style={ts.bottomRow}>
                  <span>{formatTime(activity.startTime)}</span>
                  <span>{formatDuration(activity.durationMs)}</span>
                  {offset > 0 && (
                    <span style={ts.offset}>{formatOffset(offset)}</span>
                  )}
                </div>
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}

/**
 * Re-fits the viewport after ELK layout completes and when the container
 * is resized. Fires with delays because styled-components injection can
 * change node sizes after the first paint.
 */
function FitViewHelper() {
  const { fitView } = useReactFlow();
  const nodeCount = useStore((s) => s.nodeLookup.size);
  const width = useStore((s) => s.width);
  const height = useStore((s) => s.height);
  const prevNodeCount = useRef(0);
  const prevSize = useRef({ w: 0, h: 0 });

  const doFit = useCallback(() => {
    fitView({ padding: 0.1, duration: 300 });
  }, [fitView]);

  useEffect(() => {
    if (nodeCount > 0 && nodeCount !== prevNodeCount.current) {
      prevNodeCount.current = nodeCount;
      const timers = [100, 350, 800].map((ms) => setTimeout(doFit, ms));
      return () => timers.forEach(clearTimeout);
    }
  }, [nodeCount, doFit]);

  useEffect(() => {
    if (
      nodeCount > 0 &&
      width > 0 &&
      height > 0 &&
      (width !== prevSize.current.w || height !== prevSize.current.h)
    ) {
      prevSize.current = { w: width, h: height };
      const id = setTimeout(doFit, 50);
      return () => clearTimeout(id);
    }
  }, [width, height, nodeCount, doFit]);

  return null;
}

export interface TopologyVisualizationInnerProps {
  data?: string;
  trace?: string;
  expanded?: boolean;
}

export const TopologyVisualizationInner: FC<
  TopologyVisualizationInnerProps
> = ({ data, trace, expanded = false }) => {
  const diagramData = useMemo<DiagramData | null>(() => {
    if (data) {
      try {
        return JSON.parse(data);
      } catch {
        return null;
      }
    }
    return null;
  }, [data]);

  const traceData = useMemo<MessageTrace | undefined>(() => {
    if (trace) {
      try {
        return JSON.parse(trace);
      } catch {
        return undefined;
      }
    }
    return undefined;
  }, [trace]);

  // When maximized with a trace, auto-expand the sidebar on the trace tab
  const hasTrace = !!traceData;
  const [sidebarCollapsed, setSidebarCollapsed] = useState(
    !expanded || !hasTrace
  );
  const [sidebarTab, setSidebarTab] = useState<SidebarTab>(
    hasTrace ? "trace" : "search"
  );

  // React to expand/collapse transitions
  useEffect(() => {
    if (expanded && hasTrace) {
      setSidebarCollapsed(false);
      setSidebarTab("trace");
    } else {
      setSidebarCollapsed(true);
    }
  }, [expanded, hasTrace]);

  // State captured from TopologyFlow via onTraceMappingChange
  const [traceMapping, setTraceMapping] = useState<TraceTopologyMapping | null>(
    null
  );
  const [flowNodes, setFlowNodes] = useState<Node[]>([]);
  const [flowEdges, setFlowEdges] = useState<Edge[]>([]);
  const [activeActivityId, setActiveActivityId] = useState<string | null>(null);
  const [focusedNodeId, setFocusedNodeId] = useState<string | null>(null);

  const handleTraceMappingChange = useCallback(
    (info: {
      traceMapping: TraceTopologyMapping | null;
      nodes: Node[];
      edges: Edge[];
    }) => {
      setTraceMapping(info.traceMapping);
      setFlowNodes(info.nodes);
      setFlowEdges(info.edges);
    },
    []
  );

  const handleActivityClick = useCallback((activity: MessageActivity) => {
    setActiveActivityId((prev) => (prev === activity.id ? null : activity.id));
  }, []);

  const handleNodeClick = useCallback((nodeId: string) => {
    setFocusedNodeId((prev) => (prev === nodeId ? null : nodeId));
  }, []);

  const traceContent = useMemo(() => {
    if (!traceData) return undefined;
    return (
      <TraceStepList
        trace={traceData}
        activeActivityId={activeActivityId}
        focusedNodeId={focusedNodeId}
        onActivityClick={handleActivityClick}
        onNodeClick={handleNodeClick}
        traceMapping={traceMapping}
        nodes={flowNodes}
        edges={flowEdges}
      />
    );
  }, [
    traceData,
    activeActivityId,
    focusedNodeId,
    handleActivityClick,
    handleNodeClick,
    traceMapping,
    flowNodes,
    flowEdges,
  ]);

  const hasSagas = useMemo(
    () =>
      diagramData?.services.some(
        (s: { sagas: unknown[] }) => s.sagas.length > 0
      ) ?? false,
    [diagramData]
  );

  const sidebarTabs = useMemo<SidebarTab[]>(
    () =>
      hasSagas
        ? ["trace", "sagas", "details", "search"]
        : ["trace", "details", "search"],
    [hasSagas]
  );

  if (!diagramData) return null;

  return (
    <div style={{ width: "100%", height: "100%" }}>
      <ScopedStyles />
      <ReactFlowProvider>
        <TopologyFlow
          data={diagramData}
          trace={traceData}
          traceContent={traceContent}
          activeTraceActivityId={activeActivityId ?? undefined}
          focusedTopologyNodeId={focusedNodeId ?? undefined}
          onTraceMappingChange={handleTraceMappingChange}
          edgeRouting="simple"
          hideSidebar={!expanded}
          hideMinimap
          hideControls={!expanded}
          sidebarTabs={sidebarTabs}
          sidebarCollapsed={sidebarCollapsed}
          onSidebarCollapsedChange={setSidebarCollapsed}
          sidebarTab={sidebarTab}
          onSidebarTabChange={setSidebarTab}
        />
        <FitViewHelper />
      </ReactFlowProvider>
    </div>
  );
};
