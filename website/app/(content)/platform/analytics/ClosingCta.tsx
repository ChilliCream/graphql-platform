import { Band } from "@/src/components/Band";
import { ButtonRow } from "@/src/components/ButtonRow";
import { SectionHeading } from "@/src/components/SectionHeading";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export function ClosingCta() {
  return (
    <Band className="mt-12" skin="accent" layout="centered">
      <SectionHeading
        align="center"
        title="The whole story, from spike to span."
        description="Follow an incident from the latency chart to the affected operation and down to the spans behind it, without leaving Nitro."
      />
      <ButtonRow align="center" className="mt-9">
        <SolidButton href="https://nitro.chillicream.com">
          Start Nitro for Free
        </SolidButton>
        <OutlineButton href="/docs/nitro/open-telemetry/operation-monitoring">
          Read Analytics Docs
        </OutlineButton>
      </ButtonRow>
    </Band>
  );
}
