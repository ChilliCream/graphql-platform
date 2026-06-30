import type { Metadata } from "next";

import { ClosingCta } from "@/src/components/pricing/ClosingCta";
import { CompareTable } from "@/src/components/pricing/CompareTable";
import { PlanSelector } from "@/src/components/pricing/PlanSelector";
import { PricingFaq } from "@/src/components/pricing/PricingFaq";
import { RegulatedBand } from "@/src/components/pricing/RegulatedBand";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "Pricing",
  description:
    "Nitro pricing: start free on the shared cloud, pay as you go at $20/mo, run a dedicated single-tenant instance from $400, or self-host. Compare every plan.",
  keywords: [
    "ChilliCream pricing",
    "Nitro pricing",
    "GraphQL platform pricing",
    "GraphQL plans",
    "dedicated instance",
    "self-hosted",
    "schema registry pricing",
    "GraphQL observability pricing",
  ],
  openGraph: {
    title: "Nitro Pricing",
    description:
      "Start free on the shared cloud, pay as you go at $20/mo, run a dedicated single-tenant instance from $400, or self-host on your own infrastructure.",
  },
};

export default function PricingPage() {
  return (
    <>
      <Hero />
      <PlanSelector />
      <CompareTable />
      <PricingFaq />
      <RegulatedBand />
      <ClosingCta />
    </>
  );
}

function Hero() {
  return (
    <section className="pt-10 pb-14 text-center sm:pt-16 sm:pb-20">
      <p className="text-cc-ink-dim font-mono text-xs tracking-[0.18em] uppercase">
        Nitro pricing
      </p>
      <h1 className="font-heading text-cc-heading sm:text-h2 mt-5 text-4xl font-semibold text-balance">
        Pricing that scales with your platform.
      </h1>
      <p className="text-cc-ink mx-auto mt-6 max-w-2xl text-base text-pretty sm:text-lg">
        Start free on the shared cloud. Pay as you go as traffic grows, run a
        dedicated single-tenant instance when you need your own region and
        isolation, or self-host on your own infrastructure.
      </p>
      <div className="mt-9 flex flex-wrap items-center justify-center gap-3">
        <SolidButton href="https://nitro.chillicream.com">
          Start for Free
        </SolidButton>
        <OutlineButton href="/services/support/contact?subject=Sales">
          Talk to Sales
        </OutlineButton>
      </div>
    </section>
  );
}
