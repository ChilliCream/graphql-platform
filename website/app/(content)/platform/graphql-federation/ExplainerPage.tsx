import type { ReactNode } from "react";

import { ButtonRow } from "@/src/components/ButtonRow";
import { FaqSection } from "@/src/components/FaqSection";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

import { FEDERATION_FAQ_ITEMS } from "./faq";
import { HowItWorksSection } from "./sections/HowItWorksSection";
import { GRADIENT, Section } from "./sections/shared";
import { WhatIsSection } from "./sections/WhatIsSection";
import { WhyFusionSection } from "./sections/WhyFusionSection";
import { WhySection } from "./sections/WhySection";
import { TransitStory } from "./TransitStory";

const HERO_SERVICES: readonly {
  readonly name: string;
  readonly color: string;
}[] = [
  { name: "Catalog", color: "#f27765" },
  { name: "Billing", color: "#eabd21" },
  { name: "Shipping", color: "#00bce5" },
];

function HeroChip({
  label,
  sub,
  dotColor,
}: {
  readonly label: string;
  readonly sub?: string;
  readonly dotColor?: string;
}) {
  return (
    <div className="border-cc-card-border rounded-lg border bg-[rgba(12,19,34,0.72)] px-4 py-2.5 text-left">
      <div className="text-cc-heading flex items-center gap-2 font-mono text-[11px] tracking-[0.14em] uppercase">
        {dotColor && (
          <span
            aria-hidden="true"
            className="inline-block size-2 rounded-[3px]"
            style={{ backgroundColor: dotColor }}
          />
        )}
        {label}
      </div>
      {sub && (
        <div className="text-cc-ink-dim mt-0.5 font-mono text-[10.5px]">
          {sub}
        </div>
      )}
    </div>
  );
}

function HeroLink() {
  return (
    <div
      aria-hidden="true"
      className="bg-cc-card-border h-6 w-px sm:h-px sm:w-8"
    />
  );
}

function HeroFan() {
  return (
    <>
      <div
        aria-hidden="true"
        className="bg-cc-card-border h-6 w-px sm:hidden"
      />
      <svg
        aria-hidden="true"
        viewBox="0 0 32 187"
        className="text-cc-ink-faint hidden h-[187px] w-8 sm:block"
      >
        <path
          d="M0 94 C 18 94, 14 29, 32 29 M0 94 H 32 M0 94 C 18 94, 14 158, 32 158"
          fill="none"
          stroke="currentColor"
          strokeWidth="1"
        />
      </svg>
    </>
  );
}

function HeroServices({ sub }: { readonly sub: string }) {
  return (
    <div className="flex flex-col items-stretch gap-2 sm:gap-1.5">
      {HERO_SERVICES.map((svc) => (
        <HeroChip
          key={svc.name}
          label={svc.name}
          sub={sub}
          dotColor={svc.color}
        />
      ))}
    </div>
  );
}

function HeroPanel({
  label,
  children,
}: {
  readonly label: string;
  readonly children: ReactNode;
}) {
  return (
    <figure className="m-0 flex flex-col items-center gap-4">
      <figcaption className="text-cc-nav-label font-mono text-[11px] tracking-[0.16em] uppercase">
        {label}
      </figcaption>
      {children}
    </figure>
  );
}

function HeroDiagram() {
  return (
    <div className="mt-10 flex flex-col items-center gap-10 lg:flex-row lg:items-start lg:gap-14">
      <HeroPanel label="Before · every app merges by hand">
        <div className="flex flex-col items-center gap-0 sm:flex-row sm:items-center">
          <HeroChip label="Client" sub="three calls · merged by hand" />
          <HeroFan />
          <HeroServices sub="own API · own team" />
        </div>
      </HeroPanel>
      <HeroPanel label="Federated · one gateway, one query">
        <div className="flex flex-col items-center gap-0 sm:flex-row sm:items-center">
          <HeroChip label="Client" sub="one query" />
          <HeroLink />
          <HeroChip label="Gateway" sub="one schema · one endpoint" />
          <HeroFan />
          <HeroServices sub="own schema · own team" />
        </div>
      </HeroPanel>
    </div>
  );
}

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
          jump straight into Fusion. It speaks both: GraphQL Federation, the
          vendor neutral standard built in the open at the GraphQL Foundation,
          and Apollo Federation, the one controlled by a single company.
        </p>
        <ButtonRow className="mt-7">
          <SolidButton href="/docs/fusion/getting-started">
            Get Started
          </SolidButton>
          <OutlineButton href="/services/support/contact?subject=Sales&context=GraphQL%20Federation">
            Contact an Expert
          </OutlineButton>
        </ButtonRow>
        <HeroDiagram />
        <p className="font-heading text-cc-heading mx-auto mt-10 text-xl text-balance">
          One gateway, one{" "}
          <span
            className="bg-clip-text text-transparent"
            style={{ backgroundImage: GRADIENT }}
          >
            schema
          </span>
          . The services stay exactly where they are.
        </p>
      </section>

      <TransitStory />

      <WhatIsSection />
      <WhySection />
      <HowItWorksSection />
      <WhyFusionSection />

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
