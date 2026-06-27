"use client";

/**
 * Public client API for the vendored Nitro telemetry animation system.
 *
 * This is the single client boundary: marketing pages are server components and
 * import the prop-less, ready-to-drop wrappers below. Every wrapper mounts its own
 * `ThemeProvider` (theme="dark", reducedMotion="never") so the `--t-*` token vars
 * resolve under `.nt-root` and the theme CSS is injected once (idempotent).
 *
 * `reducedMotion="never"` makes the demos ALWAYS animate, ignoring the OS
 * `prefers-reduced-motion` setting: these are looping product visuals, not UI
 * affordances, so they should play for everyone.
 *
 * Each wrapper takes an optional `className` for sizing the container.
 */
import type { MotionValue } from "motion/react";
import { ThemeProvider } from "./lib/theme";
import { useMasterClock } from "./lib/useInViewLoop";

import { NitroTabReel } from "./demos/tabs/NitroTabReel";
import { MonitoringOverview } from "./demos/monitoring/MonitoringOverview";
import { OperationDetail } from "./demos/operation/OperationDetail";
import { MonitoringReel } from "./demos/reel/MonitoringReel";

import { ComposeScreen } from "./demos/tabs/ComposeScreen";
import { TraceScreen } from "./demos/tabs/TraceScreen";
import { DiagnoseScreen } from "./demos/tabs/DiagnoseScreen";
import { SchemaScreen } from "./demos/tabs/SchemaScreen";
import { FusionScreen } from "./demos/tabs/FusionScreen";
import { TABREEL_CANVAS } from "./primitives/reel/TabReel";

interface NitroWrapperProps {
  readonly className?: string;
  /** Standalone-screen loop length in ms (defaults to the master clock's 11000). */
  readonly durationMs?: number;
}

/** The full 5-tab Nitro product reel (Author / Observe / Diagnose / Schema / Fusion). */
export function NitroReel({ className }: NitroWrapperProps) {
  return (
    <ThemeProvider theme="dark" reducedMotion="never" className={className}>
      <NitroTabReel />
    </ThemeProvider>
  );
}

/** Monitoring overview dashboard. */
export function NitroMonitoring({ className }: NitroWrapperProps) {
  return (
    <ThemeProvider theme="dark" reducedMotion="never" className={className}>
      <MonitoringOverview />
    </ThemeProvider>
  );
}

/** Single-operation detail view (latency distribution, trace waterfall, ...). */
export function NitroOperation({ className }: NitroWrapperProps) {
  return (
    <ThemeProvider theme="dark" reducedMotion="never" className={className}>
      <OperationDetail />
    </ThemeProvider>
  );
}

/** Looping monitoring reel that cycles through the dashboard scenes. */
export function NitroMonitoringReel({ className }: NitroWrapperProps) {
  return (
    <ThemeProvider theme="dark" reducedMotion="never" className={className}>
      <MonitoringReel />
    </ThemeProvider>
  );
}

/**
 * Wraps a single reel tab Screen so it loops standalone. The screen takes a shared
 * `progress` MotionValue; here we mint a local in-view-gated master clock, attach its
 * ref to the container, and render the screen as `active`.
 */
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
      {/* The reel tab screens render into a fixed design canvas absolutely
          positioned inside the reel's stage; standalone they need that same
          sized, position:relative parent or they collapse to nothing. */}
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

/** Standalone "Author" screen: schema-aware GraphQL authoring. */
export function NitroCompose({ className, durationMs }: NitroWrapperProps) {
  return (
    <StandaloneScreen
      Screen={ComposeScreen}
      className={className}
      durationMs={durationMs}
    />
  );
}

/** Standalone "Observe" screen: monitoring to operation to slow trace span. */
export function NitroTrace({ className, durationMs }: NitroWrapperProps) {
  return (
    <StandaloneScreen
      Screen={TraceScreen}
      className={className}
      durationMs={durationMs}
    />
  );
}

/** Standalone "Diagnose" screen: error spike to failing operation to stack trace. */
export function NitroDiagnose({ className, durationMs }: NitroWrapperProps) {
  return (
    <StandaloneScreen
      Screen={DiagnoseScreen}
      className={className}
      durationMs={durationMs}
    />
  );
}

/** Standalone "Schema" screen: deprecated-field usage drill-down. */
export function NitroSchema({ className, durationMs }: NitroWrapperProps) {
  return (
    <StandaloneScreen
      Screen={SchemaScreen}
      className={className}
      durationMs={durationMs}
    />
  );
}

/** Standalone "Fusion" screen: federated query plan walkthrough. */
export function NitroFusion({ className, durationMs }: NitroWrapperProps) {
  return (
    <StandaloneScreen
      Screen={FusionScreen}
      className={className}
      durationMs={durationMs}
    />
  );
}

// Theme provider for custom composition.
export { ThemeProvider as NitroTheme } from "./lib/theme";

// Chart primitives that support standalone clocks, for building custom animated visuals.
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
