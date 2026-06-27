import type { Metadata } from "next";

import {
  SceneHookGallery,
  type SceneComponents,
} from "@/src/components/home/act2/SceneHookGallery";
import { V2_COPY } from "@/src/components/home/act2/versions/v2/copy";
import { BuildVariant1 } from "@/src/components/home/act2/versions/v2/build/BuildVariant1";
import { BuildVariant2 } from "@/src/components/home/act2/versions/v2/build/BuildVariant2";
import { BuildVariant3 } from "@/src/components/home/act2/versions/v2/build/BuildVariant3";
import { BuildVariant4 } from "@/src/components/home/act2/versions/v2/build/BuildVariant4";
import { BuildVariant5 } from "@/src/components/home/act2/versions/v2/build/BuildVariant5";
import { FeedbackVariant1 } from "@/src/components/home/act2/versions/v2/feedback/FeedbackVariant1";
import { FeedbackVariant2 } from "@/src/components/home/act2/versions/v2/feedback/FeedbackVariant2";
import { FeedbackVariant3 } from "@/src/components/home/act2/versions/v2/feedback/FeedbackVariant3";
import { FeedbackVariant4 } from "@/src/components/home/act2/versions/v2/feedback/FeedbackVariant4";
import { FeedbackVariant5 } from "@/src/components/home/act2/versions/v2/feedback/FeedbackVariant5";
import { ObserveVariant1 } from "@/src/components/home/act2/versions/v2/observe/ObserveVariant1";
import { ObserveVariant2 } from "@/src/components/home/act2/versions/v2/observe/ObserveVariant2";
import { ObserveVariant3 } from "@/src/components/home/act2/versions/v2/observe/ObserveVariant3";
import { ObserveVariant4 } from "@/src/components/home/act2/versions/v2/observe/ObserveVariant4";
import { ObserveVariant5 } from "@/src/components/home/act2/versions/v2/observe/ObserveVariant5";
import { WorkflowsVariant1 } from "@/src/components/home/act2/versions/v2/workflows/WorkflowsVariant1";
import { WorkflowsVariant2 } from "@/src/components/home/act2/versions/v2/workflows/WorkflowsVariant2";
import { WorkflowsVariant3 } from "@/src/components/home/act2/versions/v2/workflows/WorkflowsVariant3";
import { WorkflowsVariant4 } from "@/src/components/home/act2/versions/v2/workflows/WorkflowsVariant4";
import { WorkflowsVariant5 } from "@/src/components/home/act2/versions/v2/workflows/WorkflowsVariant5";
import { GuardrailsVariant1 } from "@/src/components/home/act2/versions/v2/guardrails/GuardrailsVariant1";
import { GuardrailsVariant2 } from "@/src/components/home/act2/versions/v2/guardrails/GuardrailsVariant2";
import { GuardrailsVariant3 } from "@/src/components/home/act2/versions/v2/guardrails/GuardrailsVariant3";
import { GuardrailsVariant4 } from "@/src/components/home/act2/versions/v2/guardrails/GuardrailsVariant4";
import { GuardrailsVariant5 } from "@/src/components/home/act2/versions/v2/guardrails/GuardrailsVariant5";

export const metadata: Metadata = {
  title: "Scene Illustrations v2 · Flow Diagrams",
  robots: { index: false, follow: false },
};

const COMPONENTS: SceneComponents = {
  build: {
    1: BuildVariant1,
    2: BuildVariant2,
    3: BuildVariant3,
    4: BuildVariant4,
    5: BuildVariant5,
  },
  feedback: {
    1: FeedbackVariant1,
    2: FeedbackVariant2,
    3: FeedbackVariant3,
    4: FeedbackVariant4,
    5: FeedbackVariant5,
  },
  observe: {
    1: ObserveVariant1,
    2: ObserveVariant2,
    3: ObserveVariant3,
    4: ObserveVariant4,
    5: ObserveVariant5,
  },
  workflows: {
    1: WorkflowsVariant1,
    2: WorkflowsVariant2,
    3: WorkflowsVariant3,
    4: WorkflowsVariant4,
    5: WorkflowsVariant5,
  },
  guardrails: {
    1: GuardrailsVariant1,
    2: GuardrailsVariant2,
    3: GuardrailsVariant3,
    4: GuardrailsVariant4,
    5: GuardrailsVariant5,
  },
};

export default function SceneIllustrationsV2Page() {
  return (
    <SceneHookGallery version={2} components={COMPONENTS} copy={V2_COPY} />
  );
}
