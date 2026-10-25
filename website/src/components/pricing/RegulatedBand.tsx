import { Band } from "@/src/components/Band";
import { ButtonRow } from "@/src/components/ButtonRow";
import { CheckList } from "@/src/components/CheckList";
import { SectionHeading } from "@/src/components/SectionHeading";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

const REGULATED_POINTS: readonly string[] = [
  "Procurement and security requirements",
  "Dedicated, BYOC, or on-prem deployment",
  "Data location and network constraints",
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
            eyebrow="Private deployment"
            title="Regulated industry or air-gapped?"
            description="Bring us your infrastructure, network, data-location, and procurement constraints. We will help map them to the right Nitro deployment architecture and commercial plan."
          />
          <ButtonRow align="start" className="mt-6">
            <SolidButton href="/services/support/contact?subject=Sales&context=Private%20Nitro%20Deployment">
              Discuss deployment requirements
            </SolidButton>
            <OutlineButton href="/platform">Explore the platform</OutlineButton>
          </ButtonRow>
        </div>
      }
      aside={<CheckList variant="pill" items={REGULATED_POINTS} />}
    />
  );
}
