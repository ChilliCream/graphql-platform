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
        description="Join the community Slack to talk to other ChilliCream users, or get in touch to size a paid plan for your team."
      />
      <ButtonRow align="center" className="mt-8">
        <SolidButton href="/services/support/contact">
          Contact sales
        </SolidButton>
        <OutlineButton href="https://slack.chillicream.com/">
          Join community Slack
        </OutlineButton>
      </ButtonRow>
    </Band>
  );
}
