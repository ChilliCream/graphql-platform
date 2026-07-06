import { TabReel, type TabReelTab } from "../../primitives/reel/TabReel";
import {
  IconGraphql,
  IconTelemetry,
  IconWarning,
  IconSchema,
  IconQueryPlan,
} from "../../primitives/icons";
import { ComposeScreen, COMPOSE_MS } from "./ComposeScreen";
import { TraceScreen, TRACE_MS } from "./TraceScreen";
import { DiagnoseScreen, DIAGNOSE_MS } from "./DiagnoseScreen";
import { SchemaScreen } from "./SchemaScreen";
import { FusionScreen, FUSION_MS } from "./FusionScreen";

export const NITRO_TABS: TabReelTab[] = [
  {
    id: "trace",
    label: "Observe",
    durationMs: TRACE_MS,
    icon: <IconTelemetry size={16} color="currentColor" />,
    headline: "See everything your gateway is doing",
    subhead:
      "Monitoring overview to operation breakdown to distributions to the exact slow trace span.",
    Screen: TraceScreen,
  },
  {
    id: "diagnose",
    label: "Diagnose",
    durationMs: DIAGNOSE_MS,
    icon: <IconWarning size={16} color="currentColor" />,
    headline: "From error spike to root cause",
    subhead:
      "Two clicks turn a production error spike into the exact failing operation and its server stack trace.",
    Screen: DiagnoseScreen,
  },
  {
    id: "fusion",
    label: "Fusion",
    durationMs: FUSION_MS,
    icon: <IconQueryPlan size={16} color="currentColor" />,
    headline: "See your query's path",
    subhead:
      "Fusion turns one request into a traced, parallel, batched fetch plan across every subgraph.",
    Screen: FusionScreen,
  },
  {
    id: "schema",
    label: "Schema",
    durationMs: 22000,
    icon: <IconSchema size={16} color="currentColor" />,
    headline: "Change fields without breaking anyone",
    subhead:
      "Filter to deprecated fields, sort by traffic, and drill into the exact operations still calling them.",
    Screen: SchemaScreen,
  },
  {
    id: "compose",
    label: "Author",
    durationMs: COMPOSE_MS,
    icon: <IconGraphql size={16} color="currentColor" />,
    headline: "Write GraphQL at the speed of thought",
    subhead:
      "Schema-aware autocomplete finishes your query, then real federated data streams back.",
    Screen: ComposeScreen,
  },
];

export interface NitroTabReelProps {
  staticTab?: number;
  staticProgress?: number;
  tabsOverlay?: boolean;
}

export function NitroTabReel({
  staticTab,
  staticProgress,
  tabsOverlay,
}: NitroTabReelProps = {}) {
  return (
    <TabReel
      tabs={NITRO_TABS}
      staticTab={staticTab}
      staticProgress={staticProgress}
      tabsOverlay={tabsOverlay}
      ariaLabel="Nitro — 5 product capabilities"
    />
  );
}
