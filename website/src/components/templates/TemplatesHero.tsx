import { ButtonRow } from "@/src/components/ButtonRow";
import { MarketingHero } from "@/src/components/MarketingHero";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { GitHubIcon } from "@/src/icons/GitHub";

export function TemplatesHero() {
  return (
    <MarketingHero
      title="Start with a template."
      lead="GraphQL services, federations, and clients with the architecture already in place. Find the combination you need, clone it, and ship."
      actions={
        <ButtonRow>
          <SolidButton href="#catalog">Browse the catalog</SolidButton>
          <OutlineButton href="https://github.com/ChilliCream/templates">
            <GitHubIcon className="mr-2 size-4 fill-current" />
            View on GitHub
          </OutlineButton>
        </ButtonRow>
      }
    />
  );
}
