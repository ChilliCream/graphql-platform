import { Band } from "@/src/components/Band";
import { ButtonRow } from "@/src/components/ButtonRow";
import { SectionHeading } from "@/src/components/SectionHeading";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

/**
 * The closing call to action: a bordered band with a spectrum hairline and a
 * soft teal glow, restating the free offer and pointing to sign-up and docs.
 */
export function ClosingCta() {
  return (
    <Band skin="spectrum" layout="centered" className="mt-24 mb-10 sm:mt-28">
      <SectionHeading
        align="center"
        size="lg"
        title="Start free. Scale when you do."
        description="1M operations, 2 GB of ingest, schemas and environments, and the full Nitro control plane, free on the shared cloud. Upgrade only when you outgrow it."
      />
      <ButtonRow align="center" className="mt-8">
        <SolidButton href="https://nitro.chillicream.com">
          Start for free
        </SolidButton>
        <OutlineButton href="/docs">Read the docs</OutlineButton>
      </ButtonRow>
      <p className="text-cc-nav-label mt-6 font-mono text-xs">
        No credit card. Free on the shared cloud.
      </p>
    </Band>
  );
}
