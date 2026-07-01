import { ButtonRow } from "@/src/components/ButtonRow";
import { SLACK } from "@/src/components/help/helpLinks";
import { MarketingHero } from "@/src/components/MarketingHero";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

/**
 * The help hero: the three paths to GraphQL help framed up front, with a
 * consultancy session and a Slack call to action.
 */
export function HelpHero() {
  return (
    <MarketingHero
      eyebrow="Help"
      title="Get unblocked, on your timeline."
      lead="Three ways to get GraphQL help: a free community of 7000+ practitioners, expert consultancy by the hour, and tailored support plans for production teams. Pick the one that matches the urgency."
      actions={
        <ButtonRow align="center">
          <SolidButton href="/services/advisory">Explore advisory</SolidButton>
          <OutlineButton href={SLACK}>Ask in Slack</OutlineButton>
        </ButtonRow>
      }
    />
  );
}
