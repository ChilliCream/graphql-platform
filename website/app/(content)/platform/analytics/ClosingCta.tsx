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
        description="Follow an incident from the latency chart to the failing operation to the exact span that caused it, without leaving Nitro."
      />
      <ButtonRow align="center" className="mt-9">
        <SolidButton href="https://nitro.chillicream.com">Start for Free</SolidButton>
        <OutlineButton href="/docs/nitro/open-telemetry/operation-monitoring">Read the Docs</OutlineButton>
      </ButtonRow>
    </Band>
  );
}
