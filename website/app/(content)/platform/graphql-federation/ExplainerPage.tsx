import { ButtonRow } from "@/src/components/ButtonRow";
import { FaqSection } from "@/src/components/FaqSection";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

import { FEDERATION_FAQ_ITEMS } from "./faq";
import { FusionCta } from "./sections/FusionCta";
import { HowItWorksSection } from "./sections/HowItWorksSection";
import { ProblemSection } from "./sections/ProblemSection";
import { GRADIENT, Section } from "./sections/shared";
import { WhatIsSection } from "./sections/WhatIsSection";
import { WhySection } from "./sections/WhySection";

export function ExplainerPage() {
  return (
    <div className="bg-cc-bg relative left-1/2 -mt-26 w-screen -translate-x-1/2">
      <section className="border-cc-card-border relative flex flex-col items-center overflow-hidden border-b px-5 pt-24 pb-16 text-center sm:px-12 sm:pt-28 sm:pb-20">
        <h1 className="font-heading text-cc-heading text-h3 sm:text-h2 mx-auto w-full max-w-3xl text-balance">
          GraphQL{" "}
          <span
            className="bg-clip-text text-transparent sm:whitespace-nowrap"
            style={{ backgroundImage: GRADIENT }}
          >
            Federation
          </span>
        </h1>
        <p className="text-cc-ink mx-auto mt-7 max-w-2xl text-lg">
          Learn what GraphQL Federation is and whether it fits your setup, or
          jump straight into Fusion. It speaks both standards, so you can start
          where the ecosystem is today with Apollo Federation and move with it
          as GraphQL Federation becomes the open standard at the GraphQL
          Foundation.
        </p>
        <ButtonRow className="mt-7">
          <SolidButton href="/docs/fusion/getting-started">
            Get Started
          </SolidButton>
          <OutlineButton href="/services/support/contact?subject=Sales&context=GraphQL%20Federation">
            Contact an Expert
          </OutlineButton>
        </ButtonRow>
      </section>

      <ProblemSection />

      <WhatIsSection />
      <WhySection />
      <HowItWorksSection />
      <FusionCta />

      <Section id="faq">
        <FaqSection
          id="federation-faq"
          align="left"
          heading="Common questions about GraphQL Federation."
          items={FEDERATION_FAQ_ITEMS}
        />
      </Section>
    </div>
  );
}
