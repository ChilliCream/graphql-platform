import { Band } from "@/src/components/Band";
import { ButtonRow } from "@/src/components/ButtonRow";
import { CALENDLY } from "@/src/components/help/helpLinks";
import { SectionHeading } from "@/src/components/SectionHeading";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

/**
 * The closing call to action for visitors who reach the end of the help page:
 * book a consultancy session, or step over to the support plans.
 */
export function HelpClosing() {
  return (
    <Band
      skin="spectrum"
      layout="centered"
      className="py-16 sm:py-20"
      labelledBy="help-closing-heading"
    >
      <SectionHeading
        align="center"
        size="lg"
        title="Still not sure where to start?"
        titleId="help-closing-heading"
        description="Book a consultancy session, bring whatever you have, and leave with a clear next step. If we are not the right help, we will say so."
      />
      <ButtonRow align="center" className="mt-8">
        <SolidButton href={CALENDLY}>Book a consultancy session</SolidButton>
        <OutlineButton href="/services/support">
          Explore support plans
        </OutlineButton>
      </ButtonRow>
    </Band>
  );
}
