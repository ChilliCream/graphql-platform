import { Band } from "@/src/components/Band";
import { ButtonRow } from "@/src/components/ButtonRow";
import { CheckList } from "@/src/components/CheckList";
import { SectionHeading } from "@/src/components/SectionHeading";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

const REGULATED_POINTS: readonly string[] = [
  "Procurement, MSA, and security review",
  "BYOC or fully on-prem deployments",
  "Dedicated onboarding & runbooks",
];

/**
 * The "regulated industry or air-gapped?" band: a short pitch for teams with
 * compliance constraints, with a checklist of what we handle and CTAs.
 */
export function RegulatedBand() {
  return (
    <Band
      skin="card"
      className="mt-24 sm:mt-28"
      labelledBy="regulated-heading"
      main={
        <div>
          <SectionHeading
            titleId="regulated-heading"
            eyebrow="Regulated & on-prem"
            title="Regulated industry or air-gapped?"
            description="We work directly with platform teams on procurement, data residency review, and dedicated onboarding. Bring us a constraint, we'll come back with an architecture."
          />
          <ButtonRow align="start" className="mt-6">
            <SolidButton href="/services/support/contact?subject=Sales">
              Talk to Sales
            </SolidButton>
            <OutlineButton href="/platform">See the platform</OutlineButton>
          </ButtonRow>
        </div>
      }
      aside={<CheckList variant="pill" items={REGULATED_POINTS} />}
    />
  );
}
