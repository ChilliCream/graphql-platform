import { Band } from "@/src/components/Band";
import { ButtonRow } from "@/src/components/ButtonRow";
import { SectionHeading } from "@/src/components/SectionHeading";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

/**
 * The closing call to action: community Slack on one side, sales contact on the
 * other, for visitors who reach the end of the support page.
 */
export function ClosingCta() {
  return (
    <Band skin="bare" layout="centered" className="py-20">
      <SectionHeading
        align="center"
        size="lg"
        title="Ready when you are."
        description="Join the public Slack for best-effort community help, or tell us which products, teams, and incident response needs a paid plan should cover."
      />
      <ButtonRow align="center" className="mt-8">
        <SolidButton href="/services/support/contact?subject=Pricing%20%26%20Plans&context=GraphQL%20Support">
          Discuss a support plan
        </SolidButton>
        <OutlineButton href="https://slack.chillicream.com/">
          Join community Slack
        </OutlineButton>
      </ButtonRow>
    </Band>
  );
}
