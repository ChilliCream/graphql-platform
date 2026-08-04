import { ReactNode } from "react";
import { Saga } from "../types/diagram";
import { SearchableEntry } from "../utils/searchEngine";
import { Node } from "@xyflow/react";
export type SidebarTab = "sagas" | "trace" | "search" | "details" | "developer";
export interface LeftSidebarProps {
  sagas: Saga[];
  onSagaClick: (saga: Saga) => void;
  /** Content to render in the Trace tab. When provided, the Trace tab becomes available. */
  traceContent?: ReactNode;
  /** Called when trace focus mode changes (trace tab active + focus toggle on). */
  onTraceFocusChange?: (enabled: boolean) => void;
  /** Search index for the Search tab. When provided with onSearchResultSelect, the Search tab becomes available. */
  searchIndex?: SearchableEntry[];
  /** Called when a search result is selected in the Search tab. */
  onSearchResultSelect?: (nodeId: string) => void;
  /** Selected node to display in the Details tab. When set, the Details tab becomes available. */
  selectedNode?: Node | null;
  /** Called when the user closes the Details tab / deselects the node. */
  onNodeDeselect?: () => void;
  /** Called when the user clicks the Focus button on a saga detail view. */
  onFocusSaga?: (saga: Saga) => void;
  /** Controlled active tab. When provided with onActiveTabChange, the parent owns the tab state. */
  activeTab?: SidebarTab;
  /** Called when the active tab changes. */
  onActiveTabChange?: (tab: SidebarTab) => void;
  /** Controlled collapsed state. When provided with onCollapsedChange, the parent owns the collapsed state. */
  collapsed?: boolean;
  /** Called when the collapsed state changes. */
  onCollapsedChange?: (collapsed: boolean) => void;
  /** Content to render in the Developer tab panel. When provided, the Developer icon appears. */
  developerContent?: ReactNode;
  /**
   * Allowlist of tabs to display. When provided, only these tabs appear
   * regardless of whether their data props are set. When omitted, the
   * automatic detection logic applies (tabs appear when their data is
   * provided).
   */
  visibleTabs?: SidebarTab[];
}
export declare function LeftSidebar({
  sagas,
  onSagaClick,
  traceContent,
  onTraceFocusChange,
  searchIndex,
  onSearchResultSelect,
  selectedNode,
  onNodeDeselect,
  onFocusSaga,
  activeTab: controlledActiveTab,
  onActiveTabChange,
  collapsed: controlledCollapsed,
  onCollapsedChange,
  developerContent,
  visibleTabs,
}: LeftSidebarProps): import("react/jsx-runtime").JSX.Element | null;
