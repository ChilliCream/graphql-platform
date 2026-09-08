import Link from "next/link";
import type { ReactNode } from "react";

import { ButtonRow } from "@/src/components/ButtonRow";
import { CardGrid } from "@/src/components/CardGrid";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { Card } from "@/src/design-system/Card";
import { Eyebrow } from "@/src/design-system/Eyebrow";

import { COMPARISONS } from "../comparisons";
import { Intro, Section, SubHeading } from "../sections/shared";
import { comparisonHref, FEDERATION_PATH } from "./comparisonMetadata";

export interface ComparisonSection {
  /** Anchor of the section, e.g. `"how-it-works"`. */
  readonly id: string;
  readonly heading: ReactNode;
  readonly body: ReactNode;
}

export interface ComparisonVerdict {
  /** Cases where GraphQL Federation is the better buy. */
  readonly federationWins: readonly string[];
  /** Cases where the alternative is the better buy. */
  readonly alternativeWins: readonly string[];
  /**
   * How the alternative is named above its column. Defaults to the part of the
   * page title after "vs".
   */
  readonly alternativeLabel?: string;
}

export interface ComparisonLink {
  readonly label: string;
  readonly href: string;
}

interface ComparisonPageProps {
  /** Slug of this comparison in {@link COMPARISONS}; kept out of the related links. */
  readonly slug: string;
  readonly eyebrow: string;
  readonly title: string;
  readonly intro: ReactNode;
  readonly sections: readonly ComparisonSection[];
  readonly verdict: ComparisonVerdict;
  /**
   * Links shown alongside the way back to the explainer. Defaults to the other
   * comparisons.
   */
  readonly related?: readonly ComparisonLink[];
}

function alternativeName(title: string, label?: string): string {
  return label ?? title.split(/\s+vs\.?\s+/i)[1] ?? "the alternative";
}

function otherComparisons(slug: string): readonly ComparisonLink[] {
  return COMPARISONS.filter((comparison) => comparison.slug !== slug).map(
    (comparison) => ({
      label: comparison.title,
      href: comparisonHref(comparison),
    }),
  );
}

function VerdictColumn({
  id,
  heading,
  cases,
}: {
  readonly id: string;
  readonly heading: string;
  readonly cases: readonly string[];
}) {
  return (
    <Card variant="tile" className="flex h-full flex-col">
      <SubHeading id={id}>{heading}</SubHeading>
      <ul className="text-cc-ink marker:text-cc-ink-faint mt-4 list-disc space-y-3 pl-5 text-sm">
        {cases.map((item) => (
          <li key={item}>{item}</li>
        ))}
      </ul>
    </Card>
  );
}

function RelatedLink({ label, href }: ComparisonLink) {
  return (
    <li>
      <Link
        href={href}
        className="border-cc-card-border hover:border-cc-card-border-hover text-cc-ink inline-flex rounded-full border px-4 py-2 text-sm no-underline transition-colors"
      >
        {label}
      </Link>
    </li>
  );
}

/**
 * The layout every GraphQL Federation comparison page shares: a hero, the
 * body sections, the "When to pick which" verdict, and the links on to the
 * other comparisons. Each route supplies only its copy; the FAQ stays on the
 * explainer page.
 */
export function ComparisonPage({
  slug,
  eyebrow,
  title,
  intro,
  sections,
  verdict,
  related,
}: ComparisonPageProps) {
  const alternative = alternativeName(title, verdict.alternativeLabel);
  const links = related ?? otherComparisons(slug);

  return (
    <div className="bg-cc-bg relative left-1/2 -mt-26 w-screen -translate-x-1/2">
      <section className="border-cc-card-border flex flex-col items-center border-b px-5 pt-24 pb-16 text-center sm:px-12 sm:pt-28 sm:pb-20">
        <Eyebrow color="ink-dim">{eyebrow}</Eyebrow>
        <h1 className="font-heading text-cc-heading text-h3 sm:text-h2 mx-auto mt-4 w-full max-w-3xl text-balance">
          {title}
        </h1>
        <div className="text-cc-ink mx-auto mt-7 max-w-2xl space-y-4 text-lg">
          {intro}
        </div>
        <ButtonRow className="mt-7">
          <SolidButton href="/docs/fusion/getting-started">
            Get Started
          </SolidButton>
          <OutlineButton href="/services/support/contact?subject=Sales&context=GraphQL%20Federation">
            Contact an Expert
          </OutlineButton>
        </ButtonRow>
      </section>

      {sections.map((section) => (
        <Section key={section.id} id={section.id}>
          <Intro title={section.heading}>{section.body}</Intro>
        </Section>
      ))}

      <Section id="when-to-pick-which">
        <Intro title="When to pick which" />
        <div className="mt-10">
          <CardGrid cols={2} itemsStretch>
            <VerdictColumn
              id="federation-wins"
              heading="Pick GraphQL Federation when"
              cases={verdict.federationWins}
            />
            <VerdictColumn
              id="alternative-wins"
              heading={`Pick ${alternative} when`}
              cases={verdict.alternativeWins}
            />
          </CardGrid>
        </div>
      </Section>

      <Section id="keep-comparing">
        <Intro title="Keep comparing" />
        <ul className="mt-8 flex flex-col gap-3 sm:flex-row sm:flex-wrap">
          {links.map((link) => (
            <RelatedLink key={link.href} label={link.label} href={link.href} />
          ))}
          <RelatedLink
            label="Back to GraphQL Federation"
            href={FEDERATION_PATH}
          />
        </ul>
      </Section>
    </div>
  );
}
