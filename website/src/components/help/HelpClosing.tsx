import { Band } from "@/src/components/Band";
import { ButtonRow } from "@/src/components/ButtonRow";
import { SectionHeading } from "@/src/components/SectionHeading";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

/**
 * The closing call to action for visitors who reach the end of the help page:
 * explore advisory, or step over to the support plans.
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
        description="Explore advisory for a scoped GraphQL problem or compare support plans when your production team needs ongoing channels and response terms. If we are not the right help, we will say so."
      />
      <ButtonRow align="center" className="mt-8">
        <SolidButton href="/services/advisory">
          Explore GraphQL advisory
        </SolidButton>
        <OutlineButton href="/services/support">
          Explore support plans
        </OutlineButton>
      </ButtonRow>
    </Band>
  );
}
