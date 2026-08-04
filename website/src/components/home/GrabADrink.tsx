import { PageSection } from "@/src/components/PageSection";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

/**
 * Closing call-to-action. The oversized display headline echoes the coffee
 * theme that runs through the page; the buttons give the otherwise open canvas
 * a clear next step.
 */
export function GrabADrink() {
  return (
    <PageSection maxWidth="5xl" className="py-24 text-center sm:py-32">
      <h2 className="font-heading text-cc-heading text-h2 sm:text-h1 leading-[1.05] font-semibold text-balance">
        Fancy a drink?
      </h2>
      <p className="text-cc-ink mx-auto mt-6 max-w-4xl text-lg text-pretty sm:text-xl">
        Pick a starting point and we&rsquo;ll help you from there. Explore the
        platform on your own, or talk to us about the API stack you&rsquo;re
        building.
      </p>
      <div className="mt-10 flex flex-wrap items-center justify-center gap-4">
        <SolidButton href="https://nitro.chillicream.com">
          Start for Free
        </SolidButton>
        <OutlineButton href="/services/support/contact">
          Talk to Us
        </OutlineButton>
      </div>
    </PageSection>
  );
}
