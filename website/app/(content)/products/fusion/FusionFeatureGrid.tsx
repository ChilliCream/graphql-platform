import type { ReactElement } from "react";

import { CardGrid } from "@/src/components/CardGrid";
import { Card } from "@/src/design-system/Card";
import { ApiConnectIcon } from "@/src/icons/ApiConnectIcon";
import { MergingPathsIcon } from "@/src/icons/MergingPathsIcon";
import { PerformanceGaugeIcon } from "@/src/icons/PerformanceGaugeIcon";
import { ProductionPulseIcon } from "@/src/icons/ProductionPulseIcon";
import { SecurityShieldIcon } from "@/src/icons/SecurityShieldIcon";
import { StackedLayersIcon } from "@/src/icons/StackedLayersIcon";

import { FEATURES } from "./content";

/** One 80x80 icon per "Built for Distributed Graphs" card, keyed by the card's title. */
const FEATURE_ICONS: Record<string, ReactElement> = {
  "Connect every API": <ApiConnectIcon className="size-20" />,
  "Keep your existing stack": <StackedLayersIcon className="size-20" />,
  "Adopt without disruption": <MergingPathsIcon className="size-20" />,
  "Performance by design": <PerformanceGaugeIcon className="size-20" />,
  "Centralize API security": <SecurityShieldIcon className="size-20" />,
  "See what happens in production": <ProductionPulseIcon className="size-20" />,
};

interface FusionFeatureGridProps {
  /** Default "stacked"; the page renders "icon-left". */
  readonly layout?: "stacked" | "icon-left";
}

/** The "Built for Distributed Graphs" feature grid, stacked or icon-left. */
export function FusionFeatureGrid({
  layout = "stacked",
}: FusionFeatureGridProps) {
  return (
    <CardGrid cols={3} step="progressive" gap={6}>
      {FEATURES.map((feature) => (
        <Card
          key={feature.title}
          variant="tile"
          className={layout === "icon-left" ? "@container" : undefined}
        >
          {layout === "icon-left" ? (
            <div className="flex flex-col gap-4 @[16rem]:flex-row @[16rem]:items-start">
              <span className="flex-none">{FEATURE_ICONS[feature.title]}</span>
              <div className="min-w-0">
                <h3 className="text-cc-ink text-lg font-semibold">
                  {feature.title}
                </h3>
                <p className="text-cc-ink-dim mt-2 text-sm">
                  {feature.description}
                </p>
              </div>
            </div>
          ) : (
            <>
              {FEATURE_ICONS[feature.title]}
              <h3 className="text-cc-ink mt-5 text-lg font-semibold">
                {feature.title}
              </h3>
              <p className="text-cc-ink-dim mt-2 text-sm">
                {feature.description}
              </p>
            </>
          )}
        </Card>
      ))}
    </CardGrid>
  );
}
