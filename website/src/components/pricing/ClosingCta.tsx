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
        description="The Free plan includes 1 million operations, 2 GB of ingest per month, schemas and environments, and 3-day log and trace retention."
      />
      <ButtonRow align="center" className="mt-8">
        <SolidButton href="https://nitro.chillicream.com">
          Start Nitro for Free
        </SolidButton>
        <OutlineButton href="/docs/nitro">Explore Nitro docs</OutlineButton>
      </ButtonRow>
      <p className="text-cc-ink-dim mt-6 font-mono text-xs">
        Need a private deployment? Compare Dedicated and Self-Hosted above.
      </p>
    </Band>
  );
}
