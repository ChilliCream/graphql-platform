import { ButtonRow } from "@/src/components/ButtonRow";
import { SLACK } from "@/src/components/help/helpLinks";
import { MarketingHero } from "@/src/components/MarketingHero";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

/** The help hero with paths to self-serve, advisory, and paid support. */
export function HelpHero() {
  return (
    <MarketingHero
      eyebrow="GraphQL help"
      title="Get unblocked, on your timeline."
      lead="Use the documentation and open community for everyday questions, bring a defined technical problem to an advisory engagement, or choose a support plan for ongoing production coverage."
      actions={
        <ButtonRow align="center">
          <SolidButton href="/services/advisory">
            Explore GraphQL advisory
          </SolidButton>
          <OutlineButton href={SLACK}>Join community Slack</OutlineButton>
        </ButtonRow>
      }
    />
  );
}
