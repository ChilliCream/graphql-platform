import { ButtonRow } from "@/src/components/ButtonRow";
import { FaqSection } from "@/src/components/FaqSection";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

import { FEDERATION_FAQ_ITEMS } from "./faq";
import { GraphBackdrop } from "./hero/GraphBackdrop";
import { AiDevelopmentSection } from "./sections/AiDevelopmentSection";
import { BuildingBlocksSection } from "./sections/BuildingBlocksSection";
import { ComparisonsSection } from "./sections/ComparisonsSection";
import { FromApolloSection } from "./sections/FromApolloSection";
import { FusionCta } from "./sections/FusionCta";
import { ProblemSection } from "./sections/ProblemSection";
import { GRADIENT, Section } from "./sections/shared";
import { WhatIsSection } from "./sections/WhatIsSection";

export function ExplainerPage() {
  return (
    <div className="bg-cc-bg relative left-1/2 -mt-26 w-screen -translate-x-1/2">
      <section className="border-cc-card-border relative flex min-h-[100svh] flex-col items-center justify-center overflow-hidden border-b px-5 py-24 text-center sm:px-12">
        <GraphBackdrop />
        <div
          data-hero-copy
          className="relative z-10 flex flex-col items-center"
        >
          <h1 className="font-heading text-cc-heading text-h3 sm:text-h2 mx-auto w-full max-w-3xl text-balance">
            GraphQL{" "}
            <span
              className="bg-clip-text text-transparent sm:whitespace-nowrap"
              style={{ backgroundImage: GRADIENT }}
            >
              Federation
            </span>
          </h1>
          <p
            data-hero-paragraph
            className="text-cc-ink mx-auto mt-7 max-w-2xl text-lg"
          >
            GraphQL Federation brings independent GraphQL services together into
            a single, unified API. It enables teams to develop, deploy, and
            scale their services independently while giving clients one coherent
            graph to query.
          </p>
          <div data-hero-cta>
            <ButtonRow className="mt-7">
              <SolidButton href="/docs/fusion/getting-started">
                Get Started
              </SolidButton>
              <OutlineButton href="/services/support/contact?subject=Sales&context=GraphQL%20Federation">
                Contact an Expert
              </OutlineButton>
            </ButtonRow>
          </div>
        </div>
      </section>

      <WhatIsSection />
      <ProblemSection />
      <BuildingBlocksSection />
      <FromApolloSection />
      <AiDevelopmentSection />
      <ComparisonsSection />
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
