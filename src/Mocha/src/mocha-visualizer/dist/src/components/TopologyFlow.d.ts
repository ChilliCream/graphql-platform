import { ReactNode } from "react";
import { Node, Edge } from "@xyflow/react";
import { DiagramData } from "../types/diagram";
import { MessageTrace, MessageActivity } from "../types/trace";
import { SidebarTab } from "./LeftSidebar";
export type ViewMode = "detail" | "overview";
export type EdgeRouting = "smart" | "simple";
interface TopologyFlowProps {
  data: DiagramData;
  trace?: MessageTrace | null;
  /** @deprecated No longer used - trace tab is now part of the LeftSidebar. */
  enableTraceTimeline?: boolean;
  /** Content to render in the left sidebar's Trace tab. When provided, the Trace tab appears. */
  traceContent?: ReactNode;
  activeTraceActivityId?: string;
  /** Focus a specific topology node by ID (takes priority over activeTraceActivityId) */
  focusedTopologyNodeId?: string;
  onTraceActivityClick?: (activity: MessageActivity) => void;
  onTraceActivityHover?: (activity: MessageActivity | null) => void;
  onTraceMappingChange?: (info: {
    traceMapping: import("../utils/traceMapping").TraceTopologyMapping | null;
    nodes: Node[];
    edges: Edge[];
  }) => void;
  traceDisplayMode?: "dim" | "hide";
  /** Edge routing mode: "smart" uses A* pathfinding, "simple" uses default smoothstep */
  edgeRouting?: EdgeRouting;
  /** Controlled view mode. 'detail' shows full topology, 'overview' shows summary cards. */
  viewMode?: ViewMode;
  /** Callback when view mode changes. */
  onViewModeChange?: (mode: ViewMode) => void;
  /** Controlled active sidebar tab. */
  sidebarTab?: SidebarTab;
  /** Called when the sidebar tab changes. */
  onSidebarTabChange?: (tab: SidebarTab) => void;
  /** Controlled sidebar collapsed state. */
  sidebarCollapsed?: boolean;
  /** Called when the sidebar collapsed state changes. */
  onSidebarCollapsedChange?: (collapsed: boolean) => void;
  /** Show the developer pane icon in the left sidebar. */
  showDeveloperPane?: boolean;
  /** Custom content to inject into the developer pane (rendered above zoom controls). */
  developerPaneContent?: ReactNode;
  /** Completely hide the left sidebar. Default: false */
  hideSidebar?: boolean;
  /** Hide the minimap. Default: false */
  hideMinimap?: boolean;
  /** Hide the controls (zoom +/-, fit, lock). Default: false */
  hideControls?: boolean;
  /**
   * Allowlist of sidebar tabs to show. When provided, only these tabs
   * appear in the sidebar icon rail. Overrides the automatic visibility
   * logic (e.g. hasTrace, hasSagas). The sidebar is still hidden entirely
   * if `hideSidebar` is true.
   *
   * Example: `sidebarTabs={["trace", "details"]}` shows only the Trace
   * and Details tabs.
   */
  sidebarTabs?: SidebarTab[];
}
export declare function TopologyFlow({
  data,
  trace,
  traceContent,
  activeTraceActivityId,
  focusedTopologyNodeId,
  onTraceActivityHover,
  onTraceMappingChange,
  traceDisplayMode,
  edgeRouting: _edgeRouting,
  viewMode: controlledViewMode,
  onViewModeChange,
  sidebarTab,
  onSidebarTabChange,
  sidebarCollapsed,
  onSidebarCollapsedChange,
  showDeveloperPane,
  developerPaneContent,
  hideSidebar,
  hideMinimap,
  hideControls,
  sidebarTabs,
}: TopologyFlowProps): import("react/jsx-runtime").JSX.Element;
export {};
