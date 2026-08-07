import { DotGridSurface } from "@/src/components/DotGridSurface";
import { NextStepsSection } from "@/src/components/NextStepsSection";

export function ClosingCta() {
  return (
    <DotGridSurface className="rounded-3xl">
      <NextStepsSection
        skin="accent"
        // The accent panel already carries its own p-8/p-12 padding, so no
        // extra section-level spacing is needed here.
        className="py-0"
        title="Know what breaks before your users do."
        text="Publish the operations each client uses, validate proposed schemas against the environment you plan to update, and merge with the answer in hand."
        primaryLink="https://nitro.chillicream.com"
        primaryLinkText="Start for free"
        secondaryLink="/docs/nitro/apis/client-registry"
        secondaryLinkText="Read client registry docs"
      />
    </DotGridSurface>
  );
}
