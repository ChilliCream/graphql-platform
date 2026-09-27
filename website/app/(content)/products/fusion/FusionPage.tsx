import { Fragment } from "react";
import type { ReactElement, ReactNode } from "react";

import { Band } from "@/src/components/Band";
import { BlogTeaserGrid } from "@/src/components/BlogTeaserGrid";
import { ButtonRow } from "@/src/components/ButtonRow";
import { CardGrid } from "@/src/components/CardGrid";
import { CheckList } from "@/src/components/CheckList";
import { FeatureRow } from "@/src/components/FeatureRow";
import LayeredDiagram from "@/src/components/LayeredDiagram/index";
import {
  FUSION_CLIENT_NODES,
  FUSION_REQUESTS,
  TIER_NODES,
} from "@/src/components/LayeredDiagram/diagram";
import { Section } from "@/src/components/Section";
import { SectionHeading } from "@/src/components/SectionHeading";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { Card } from "@/src/design-system/Card";
import { Link } from "@/src/design-system/Link";
import { GraphSphere } from "@/src/icons/GraphSphere";

import type { CopyLink } from "./content";
import { CLOSING_BAND, FEATURED_CONTENT, FEATURES, SECTIONS } from "./content";
import { FusionHero } from "./hero/FusionHero";
import { InsightsWindow } from "./visuals/InsightsWindow";
import { PerformanceWindow } from "./visuals/PerformanceWindow";
import { ProtectWindow } from "./visuals/ProtectWindow";
import { SecurityWindow } from "./visuals/SecurityWindow";

/**
 * The Fusion product page: hero copy and buttons, then the diagram panel,
 * one feature row per claim, the feature grid, the featured content
 * teasers, and the closing band. All words come from `./content`.
 */

const PANEL_CLASS = "border-cc-card-border bg-cc-card-bg rounded-xl border";

/** Full-bleed 1px section separator, breaking out of the content column like FusionHero. */
function Divider() {
  return (
    <div
      aria-hidden="true"
      className="border-cc-card-border relative left-1/2 w-screen -translate-x-1/2 border-t"
    />
  );
}

/** Re-links the phrases the production page links, leaving the words untouched. */
function withLinks(text: string, links: readonly CopyLink[]): ReactNode {
  let parts: (string | ReactElement)[] = [text];

  for (const link of links) {
    parts = parts.flatMap((part) => {
      if (typeof part !== "string") return [part];
      const at = part.indexOf(link.label);
      if (at < 0) return [part];
      return [
        part.slice(0, at),
        <Link key={link.href} href={link.href}>
          {link.label}
        </Link>,
        part.slice(at + link.label.length),
      ];
    });
  }

  return parts.map((part, i) => <Fragment key={i}>{part}</Fragment>);
}

interface InPracticeProps {
  readonly links: readonly CopyLink[];
}

/** The trailing "In practice: ..." line, or nothing when a section has none. */
function InPractice({ links }: InPracticeProps) {
  if (links.length === 0) return null;

  return (
    <p className="text-cc-ink mt-4 text-base">
      In practice:{" "}
      {links.map((link, i) => (
        <Fragment key={link.href}>
          {i > 0 ? " and " : null}
          <Link href={link.href}>{link.label}</Link>
        </Fragment>
      ))}
      .
    </p>
  );
}

interface Panel {
  readonly visual: ReactNode;
  readonly bare?: boolean;
}

/** One panel per text section, in the order the copy declares them. */
const VISUALS: Readonly<Record<string, Panel>> = {
  "what-is-fusion": {
    visual: (
      <LayeredDiagram
        gatewayLabel="Fusion"
        caption="Router"
        clients={FUSION_CLIENT_NODES}
        tiers={TIER_NODES}
        requests={FUSION_REQUESTS}
        dense
        busCentered
        shortSpecTags
      />
    ),
  },
  performance: { visual: <PerformanceWindow />, bare: true },
  security: { visual: <SecurityWindow /> },
  insights: { visual: <InsightsWindow />, bare: true },
  "client-safety": { visual: <ProtectWindow /> },
};

export function FusionPage() {
  return (
    <>
      <FusionHero />

      {SECTIONS.map((section, i) => {
        const panel = VISUALS[section.id];
        const { bullets } = section;
        const [firstParagraph, ...restParagraphs] = section.paragraphs;

        return (
          <Fragment key={section.id}>
            {i > 0 && <Divider />}
            <section id={section.id} className="py-16 sm:py-24">
              <FeatureRow
                title={section.title}
                body={
                  bullets ? undefined : withLinks(firstParagraph, section.links)
                }
                visual={
                  panel.bare ? (
                    panel.visual
                  ) : (
                    <div className={`${PANEL_CLASS} overflow-hidden`}>
                      {panel.visual}
                    </div>
                  )
                }
                reverse={i % 2 === 1}
              >
                {bullets && (
                  <CheckList items={bullets} size="base" className="mt-4" />
                )}
                {!bullets && restParagraphs.length > 0 && (
                  <div className="text-cc-ink mt-4 space-y-4 text-base">
                    {restParagraphs.map((paragraph) => (
                      <p key={paragraph}>
                        {withLinks(paragraph, section.links)}
                      </p>
                    ))}
                  </div>
                )}
                <InPractice links={section.inPractice} />
              </FeatureRow>
            </section>
          </Fragment>
        );
      })}

      <Divider />
      <Section title="Built for Distributed Graphs" className="sm:py-24">
        <CardGrid cols={3} step="progressive" gap={6}>
          {FEATURES.map((feature) => (
            <Card key={feature.title} variant="tile">
              <h3 className="text-cc-ink text-lg font-semibold">
                {feature.title}
              </h3>
              <p className="text-cc-ink-dim mt-2 text-sm">
                {feature.description}
              </p>
            </Card>
          ))}
        </CardGrid>
      </Section>

      <Divider />
      <Section title="Featured Content" className="sm:py-24">
        <BlogTeaserGrid
          posts={FEATURED_CONTENT}
          showMedia={false}
          clampDescription={false}
        />
      </Section>

      <Divider />
      <div id={CLOSING_BAND.id} className="scroll-mt-24">
        <Band
          className="py-16 sm:py-24"
          skin="accent"
          layout="split"
          main={
            <div>
              <SectionHeading
                title={CLOSING_BAND.title}
                description={CLOSING_BAND.description}
              />
              <ButtonRow align="start" className="mt-9">
                <SolidButton href={CLOSING_BAND.buttons[0].href}>
                  {CLOSING_BAND.buttons[0].label}
                </SolidButton>
                <OutlineButton href={CLOSING_BAND.buttons[1].href}>
                  {CLOSING_BAND.buttons[1].label}
                </OutlineButton>
              </ButtonRow>
            </div>
          }
          aside={
            <GraphSphere className="mx-auto aspect-square w-full max-w-[380px]" />
          }
        />
      </div>
    </>
  );
}
