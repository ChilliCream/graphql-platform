import { Band } from "@/src/components/Band";
import { ButtonRow } from "@/src/components/ButtonRow";
import { CheckList } from "@/src/components/CheckList";
import { SectionHeading } from "@/src/components/SectionHeading";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

const FACTS: readonly string[] = [
  "Tailored response times",
  "Dedicated account manager",
  "Phone support",
  "Status reviews",
  "Unlimited critical incidents",
  "Private issue tracking board",
];

/**
 * The enterprise band: an accent-bordered panel for whole-org coverage, with the
 * tailored terms on the left and the call to action on the right.
 */
export function EnterpriseBand() {
  return (
    <Band
      skin="accent"
      className="py-16 sm:py-20"
      main={
        <div>
          <SectionHeading
            eyebrow="Enterprise"
            title="Your coverage, your teams, your platform."
            description="Enterprise is for organizations running Hot Chocolate, Fusion, or Nitro across many teams and business units. When you need a 24-hour critical response time, phone support, and regular status reviews, we tailor the contract, the response times, and your named contacts to fit how you operate."
          />
          <CheckList className="mt-6" columns={2} items={FACTS} />
        </div>
      }
      aside={
        <ButtonRow align="stacked">
          <SolidButton
            href="/services/support/contact?plan=Enterprise"
            className="w-full"
          >
            Talk to us about Enterprise
          </SolidButton>
          <OutlineButton href="/services/advisory" className="w-full">
            See advisory engagements
          </OutlineButton>
        </ButtonRow>
      }
    />
  );
}
