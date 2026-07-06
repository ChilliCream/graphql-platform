"use client";

import type { MotionValue } from "motion/react";
import { ThemeProvider } from "./lib/theme";
import { useMasterClock } from "./lib/useInViewLoop";

import { NitroTabReel } from "./demos/tabs/NitroTabReel";

import { ComposeScreen } from "./demos/tabs/ComposeScreen";
import { TraceScreen } from "./demos/tabs/TraceScreen";
import { DiagnoseScreen } from "./demos/tabs/DiagnoseScreen";
import { SchemaScreen } from "./demos/tabs/SchemaScreen";
import { FusionScreen } from "./demos/tabs/FusionScreen";
import { TABREEL_CANVAS } from "./primitives/reel/TabReel";

interface NitroWrapperProps {
  readonly className?: string;
  readonly durationMs?: number;
}

interface NitroReelProps extends NitroWrapperProps {
  readonly tabsOverlay?: boolean;
}

export function NitroReel({ className, tabsOverlay }: NitroReelProps) {
  return (
    <ThemeProvider
      theme="dark"
      reducedMotion="never"
      className={className}
      style={tabsOverlay ? { background: "transparent" } : undefined}
    >
      <NitroTabReel tabsOverlay={tabsOverlay} />
    </ThemeProvider>
  );
}

interface StandaloneScreenProps extends NitroWrapperProps {
  readonly Screen: (props: {
    progress: MotionValue<number>;
    active?: boolean;
  }) => React.ReactElement;
}

function StandaloneScreen({
  Screen,
  className,
  durationMs,
}: StandaloneScreenProps) {
  const { ref, progress } = useMasterClock({ durationMs });
  return (
    <ThemeProvider theme="dark" reducedMotion="never" className={className}>
      <div
        ref={ref}
        style={{
          position: "relative",
          width: "100%",
          aspectRatio: `${TABREEL_CANVAS.w} / ${TABREEL_CANVAS.h}`,
          overflow: "hidden",
          background: "var(--t-bg)",
        }}
      >
        <Screen progress={progress} active />
      </div>
    </ThemeProvider>
  );
}

export function NitroCompose({ className, durationMs }: NitroWrapperProps) {
  return (
    <StandaloneScreen
      Screen={ComposeScreen}
      className={className}
      durationMs={durationMs}
    />
  );
}

export function NitroTrace({ className, durationMs }: NitroWrapperProps) {
  return (
    <StandaloneScreen
      Screen={TraceScreen}
      className={className}
      durationMs={durationMs}
    />
  );
}

export function NitroDiagnose({ className, durationMs }: NitroWrapperProps) {
  return (
    <StandaloneScreen
      Screen={DiagnoseScreen}
      className={className}
      durationMs={durationMs}
    />
  );
}

export function NitroSchema({ className, durationMs }: NitroWrapperProps) {
  return (
    <StandaloneScreen
      Screen={SchemaScreen}
      className={className}
      durationMs={durationMs}
    />
  );
}

export function NitroFusion({ className, durationMs }: NitroWrapperProps) {
  return (
    <StandaloneScreen
      Screen={FusionScreen}
      className={className}
      durationMs={durationMs}
    />
  );
}

export { ThemeProvider as NitroTheme } from "./lib/theme";

export { LineAreaChart } from "./primitives/LineAreaChart";
export { BarSeries } from "./primitives/BarSeries";
export { HBarSeries } from "./primitives/HBarSeries";
export { Sparkline } from "./primitives/Sparkline";
export { TraceWaterfall } from "./primitives/TraceWaterfall";
export { ChartPanel } from "./primitives/ChartPanel";
export { InsightsTable } from "./primitives/InsightsTable";
export { CountUp } from "./primitives/CountUp";
export { Tile } from "./primitives/Tile";
export { DashboardFrame } from "./primitives/DashboardFrame";
export { CodeBlock } from "./primitives/CodeBlock";
export { token } from "./lib/tokens";
