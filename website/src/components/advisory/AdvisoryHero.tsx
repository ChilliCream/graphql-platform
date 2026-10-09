import {
  CONSULTING_MAILTO,
  CONTACT_FORM,
} from "@/src/components/advisory/advisoryLinks";
import { ButtonRow } from "@/src/components/ButtonRow";
import { MarketingHero } from "@/src/components/MarketingHero";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

/**
 * The advisory hero: who you talk to and how engagements work, with two contact
 * CTAs.
 */
export function AdvisoryHero() {
  return (
    <MarketingHero
      eyebrow="ChilliCream Advisory"
      title="Talk to the engineers who built your GraphQL stack."
      lead="Get focused consulting in packages of hours, or bring in the team behind Hot Chocolate, Fusion, and Nitro for a scoped implementation. Bring a question, a design, or a deadline. We meet you where the work is."
      actions={
        <ButtonRow align="center">
          <SolidButton href={CONTACT_FORM}>
            Discuss your GraphQL project
          </SolidButton>
          <OutlineButton href={CONSULTING_MAILTO}>
            Email the advisory team
          </OutlineButton>
        </ButtonRow>
      }
    />
  );
}
