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
        <SolidButton
          href="https://nitro.chillicream.com"
          track={{ name: "nitro_signup_click", params: { location: "platform_analytics_closing" } }}
        >
          Start for Free
        </SolidButton>
        <OutlineButton
          href="/docs/nitro/open-telemetry/operation-monitoring"
          track={{ name: "docs_cta_click", params: { location: "platform_analytics_closing" } }}
        >
          Read the Docs
        </OutlineButton>
      </ButtonRow>
    </Band>
  );
}
