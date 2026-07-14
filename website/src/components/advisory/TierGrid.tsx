import {
  CONSULTING_MAILTO,
  CONTRACTING_MAILTO,
  CONTACT_FORM,
} from "@/src/components/advisory/advisoryLinks";
import { CardGrid } from "@/src/components/CardGrid";
import { CheckListItem } from "@/src/components/CheckListItem";
import { HighlightCard } from "@/src/components/HighlightCard";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { Eyebrow } from "@/src/design-system/Eyebrow";

interface Tier {
  readonly id: "consulting" | "contracting";
  readonly eyebrow: string;
  readonly name: string;
  readonly tagline: string;
  readonly priceLine: string;
  readonly priceNote: string;
  readonly bestFor: string;
  readonly perks: readonly string[];
  readonly primaryCta: { readonly label: string; readonly href: string };
  readonly secondaryCta: { readonly label: string; readonly href: string };
  readonly highlight?: boolean;
}

const TIERS: readonly Tier[] = [
  {
    id: "consulting",
    eyebrow: "Packages of hours",
    name: "Consulting",
    tagline:
      "Consulting in packages of hours to get the help you need at any stage of your project. The best way to get started.",
    priceLine: "20h",
    priceNote: "increments",
    bestFor:
      "Teams that already own the build and need a senior GraphQL engineer on call for design, troubleshooting, and review.",
    perks: [
      "Mentoring and guidance",
      "Architecture",
      "Troubleshooting",
      "Code Review",
      "Best practices education",
    ],
    primaryCta: { label: "Talk to us", href: CONTACT_FORM },
    secondaryCta: { label: "Email us", href: CONSULTING_MAILTO },
    highlight: true,
  },
  {
    id: "contracting",
    eyebrow: "Scoped engagements",
    name: "Contracting",
    tagline:
      "Options for teams who do not have the time, bandwidth, and/or expertise to implement their own GraphQL solutions.",
    priceLine: "Custom",
    priceNote: "scope & timeline",
    bestFor:
      "Teams that want our engineers to deliver a working result, from a proof of concept to a production rollout.",
    perks: ["Proof of concept", "Implementation"],
    primaryCta: { label: "Talk to an Expert", href: CONTACT_FORM },
    secondaryCta: { label: "Email us", href: CONTRACTING_MAILTO },
  },
];

/**
 * The two advisory engagement tiers side by side: Consulting highlighted with a
 * rainbow border and a "Start here" pill, Contracting as a plain card.
 */
export function TierGrid() {
  return (
    <section aria-labelledby="tiers-heading" className="pt-6 pb-16 sm:pb-24">
      <h2 id="tiers-heading" className="sr-only">
        Engagement tiers
      </h2>
      <CardGrid cols={2} breakpoint="lg" gap={6} itemsStretch>
        {TIERS.map((tier) => (
          <TierCard key={tier.id} tier={tier} />
        ))}
      </CardGrid>
    </section>
  );
}

function TierCard({ tier }: { readonly tier: Tier }) {
  if (tier.highlight) {
    return (
      <HighlightCard highlight badgeLabel="Start here" gap="">
        <TierCardBody tier={tier} />
      </HighlightCard>
    );
  }

  return (
    <div className="bg-cc-card-bg border-cc-card-border hover:border-cc-card-border-hover flex h-full flex-col rounded-3xl border p-7 transition-colors sm:p-9">
      <TierCardBody tier={tier} />
    </div>
  );
}

function TierCardBody({ tier }: { readonly tier: Tier }) {
  return (
    <>
      <Eyebrow size="2xs">{tier.eyebrow}</Eyebrow>
      <h3 className="font-heading text-cc-heading text-h3 mt-3 font-semibold">
        {tier.name}
      </h3>
      <p className="text-cc-ink-dim mt-3 text-sm leading-relaxed sm:text-base">
        {tier.tagline}
      </p>

      <div className="mt-6 flex items-baseline gap-2">
        <span className="font-heading text-cc-heading text-h4 font-semibold">
          {tier.priceLine}
        </span>
        <span className="text-cc-nav-label font-mono text-xs">
          {tier.priceNote}
        </span>
      </div>

      <div
        aria-hidden="true"
        className="border-cc-ink-faint my-6 border-t border-dashed"
      />

      <Eyebrow size="2xs">Best for</Eyebrow>
      <p className="text-cc-ink mt-2 text-sm leading-relaxed">{tier.bestFor}</p>

      <Eyebrow size="2xs" className="mt-6">
        What is included
      </Eyebrow>
      <ul className="mt-3 flex flex-1 flex-col gap-3">
        {tier.perks.map((perk) => (
          <CheckListItem key={perk} iconClassName="text-cc-accent mt-[5px]">
            {perk}
          </CheckListItem>
        ))}
      </ul>

      <div className="mt-8 flex flex-col gap-3 sm:flex-row">
        <SolidButton href={tier.primaryCta.href} className="w-full sm:flex-1">
          {tier.primaryCta.label}
        </SolidButton>
        <OutlineButton
          href={tier.secondaryCta.href}
          className="w-full sm:flex-1"
        >
          {tier.secondaryCta.label}
        </OutlineButton>
      </div>
    </>
  );
}
