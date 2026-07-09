import { ButtonRow } from "@/src/components/ButtonRow";
import { MarketingHero } from "@/src/components/MarketingHero";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

/**
 * The training hero: the friendly "don't panic" headline, the two calls to
 * action, and the starting-line caption that frames the rest of the page.
 */
export function TrainingHero() {
  return (
    <MarketingHero
      eyebrow="ChilliCream Training"
      title={<>Beginner team. Advanced team. Mixed team. Don&apos;t panic.</>}
      lead={
        <>
          Our GraphQL curriculum is designed to teach in depth and works really
          well. It also isn&apos;t set in stone, so we shape every engagement to
          the team in the room.
        </>
      }
      actions={
        <ButtonRow align="center">
          <SolidButton href="mailto:contact@chillicream.com?subject=Training">
            Talk to a trainer
          </SolidButton>
          <OutlineButton href="#levels">
            Where is your team today?
          </OutlineButton>
        </ButtonRow>
      }
    />
  );
}
