import type { ComponentType } from "react";

import { PlatformSectionV1 } from "@/src/components/home/platform/PlatformSectionV1";
import { PlatformSectionV2 } from "@/src/components/home/platform/PlatformSectionV2";
import { PlatformSectionV3 } from "@/src/components/home/platform/PlatformSectionV3";
import { PlatformSectionV4 } from "@/src/components/home/platform/PlatformSectionV4";
import { PlatformSectionV5 } from "@/src/components/home/platform/PlatformSectionV5";
import { MochaSectionV1 } from "@/src/components/home/mocha/MochaSectionV1";
import { MochaSectionV2 } from "@/src/components/home/mocha/MochaSectionV2";
import { MochaSectionV3 } from "@/src/components/home/mocha/MochaSectionV3";
import { MochaSectionV4 } from "@/src/components/home/mocha/MochaSectionV4";
import { MochaSectionV5 } from "@/src/components/home/mocha/MochaSectionV5";
import { MochaSectionV6 } from "@/src/components/home/mocha/MochaSectionV6";
import { MochaEventsAnimated } from "@/src/components/home/mocha/MochaEventsAnimated";
import { MochaSectionV7 } from "@/src/components/home/mocha/MochaSectionV7";
import { MochaSectionV8 } from "@/src/components/home/mocha/MochaSectionV8";
import { AgenticSectionV1 } from "@/src/components/home/agentic/AgenticSectionV1";
import { AgenticSectionV2 } from "@/src/components/home/agentic/AgenticSectionV2";
import { AgenticSectionV3 } from "@/src/components/home/agentic/AgenticSectionV3";
import { GovernanceSectionV1 } from "@/src/components/home/governance/GovernanceSectionV1";
import { GovernanceSectionV2 } from "@/src/components/home/governance/GovernanceSectionV2";
import { GovernanceSectionV3 } from "@/src/components/home/governance/GovernanceSectionV3";
import { ObservabilitySectionV1 } from "@/src/components/home/observability/ObservabilitySectionV1";
import { ObservabilitySectionV2 } from "@/src/components/home/observability/ObservabilitySectionV2";
import { ObservabilitySectionV3 } from "@/src/components/home/observability/ObservabilitySectionV3";
import { NitroSectionV1 } from "@/src/components/home/nitro/NitroSectionV1";
import { NitroSectionV2 } from "@/src/components/home/nitro/NitroSectionV2";
import { NitroSectionV3 } from "@/src/components/home/nitro/NitroSectionV3";
import { CombinedPlatform } from "@/src/components/home/combined/CombinedPlatform";
import { CombinedMocha } from "@/src/components/home/combined/CombinedMocha";
import { CombinedAgentic } from "@/src/components/home/combined/CombinedAgentic";
import { CombinedGovernance } from "@/src/components/home/combined/CombinedGovernance";
import { CombinedObservability } from "@/src/components/home/combined/CombinedObservability";
import { CombinedNitro } from "@/src/components/home/combined/CombinedNitro";

/** One selectable landing section: a take from one of the section tracks. */
export interface SectionEntry {
  readonly id: string;
  readonly track: string;
  readonly label: string;
  readonly Component: ComponentType;
}

/** Every section take available to the landing customizer (individual takes + combined). */
export const SECTIONS: readonly SectionEntry[] = [
  {
    id: "platform-v1",
    track: "Platform",
    label: "Alternating Rows",
    Component: PlatformSectionV1,
  },
  {
    id: "platform-v2",
    track: "Platform",
    label: "The Loop",
    Component: PlatformSectionV2,
  },
  {
    id: "platform-v3",
    track: "Platform",
    label: "Bento",
    Component: PlatformSectionV3,
  },
  {
    id: "platform-v4",
    track: "Platform",
    label: "Spec Sheet",
    Component: PlatformSectionV4,
  },
  {
    id: "platform-v5",
    track: "Platform",
    label: "Architecture Map",
    Component: PlatformSectionV5,
  },
  {
    id: "mocha-v1",
    track: "Messaging",
    label: "Sequence",
    Component: MochaSectionV1,
  },
  {
    id: "mocha-v2",
    track: "Messaging",
    label: "In-process / across",
    Component: MochaSectionV2,
  },
  {
    id: "mocha-v3",
    track: "Messaging",
    label: "One message",
    Component: MochaSectionV3,
  },
  {
    id: "mocha-v4",
    track: "Messaging",
    label: "Capabilities",
    Component: MochaSectionV4,
  },
  {
    id: "mocha-v5",
    track: "Messaging",
    label: "At a glance",
    Component: MochaSectionV5,
  },
  {
    id: "mocha-v6",
    track: "Messaging",
    label: "Every app runs on events",
    Component: MochaSectionV6,
  },
  {
    id: "messaging-events-animated",
    track: "Messaging",
    label: "Every app runs on events (animated)",
    Component: MochaEventsAnimated,
  },
  {
    id: "mocha-v7",
    track: "Messaging",
    label: "Mostly side effects",
    Component: MochaSectionV7,
  },
  {
    id: "mocha-v8",
    track: "Messaging",
    label: "Simple, and it scales",
    Component: MochaSectionV8,
  },
  {
    id: "agentic-v1",
    track: "Agentic coding",
    label: "Keep the time",
    Component: AgenticSectionV1,
  },
  {
    id: "agentic-v2",
    track: "Agentic coding",
    label: "Best practices",
    Component: AgenticSectionV2,
  },
  {
    id: "agentic-v3",
    track: "Agentic coding",
    label: "Built for any agent",
    Component: AgenticSectionV3,
  },
  {
    id: "governance-v1",
    track: "Governance",
    label: "Break the build",
    Component: GovernanceSectionV1,
  },
  {
    id: "governance-v2",
    track: "Governance",
    label: "One style",
    Component: GovernanceSectionV2,
  },
  {
    id: "governance-v3",
    track: "Governance",
    label: "Nothing without a trace",
    Component: GovernanceSectionV3,
  },
  {
    id: "observability-v1",
    track: "Observability",
    label: "Fix the right thing",
    Component: ObservabilitySectionV1,
  },
  {
    id: "observability-v2",
    track: "Observability",
    label: "Where time is lost",
    Component: ObservabilitySectionV2,
  },
  {
    id: "observability-v3",
    track: "Observability",
    label: "Symptom to cause",
    Component: ObservabilitySectionV3,
  },
  {
    id: "nitro-v1",
    track: "Nitro",
    label: "Wheels attached",
    Component: NitroSectionV1,
  },
  {
    id: "nitro-v2",
    track: "Nitro",
    label: "Every surface",
    Component: NitroSectionV2,
  },
  {
    id: "nitro-v3",
    track: "Nitro",
    label: "Gauges on",
    Component: NitroSectionV3,
  },
  {
    id: "combined-platform",
    track: "Combined",
    label: "Platform",
    Component: CombinedPlatform,
  },
  {
    id: "combined-mocha",
    track: "Combined",
    label: "Messaging",
    Component: CombinedMocha,
  },
  {
    id: "combined-agentic",
    track: "Combined",
    label: "Agentic coding",
    Component: CombinedAgentic,
  },
  {
    id: "combined-governance",
    track: "Combined",
    label: "Governance",
    Component: CombinedGovernance,
  },
  {
    id: "combined-observability",
    track: "Combined",
    label: "Observability",
    Component: CombinedObservability,
  },
  {
    id: "combined-nitro",
    track: "Combined",
    label: "Nitro",
    Component: CombinedNitro,
  },
];

/** Track display order for grouping in the customizer. */
export const TRACK_ORDER: readonly string[] = [
  "Combined",
  "Platform",
  "Messaging",
  "Agentic coding",
  "Governance",
  "Observability",
  "Nitro",
];
