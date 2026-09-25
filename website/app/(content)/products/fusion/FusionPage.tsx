import { Fragment } from "react";
import type { ReactElement, ReactNode } from "react";

import { Band } from "@/src/components/Band";
import { ButtonRow } from "@/src/components/ButtonRow";
import { CardGrid } from "@/src/components/CardGrid";
import { CheckList } from "@/src/components/CheckList";
import { ClientImpactMatrix } from "@/src/components/ClientImpactMatrix";
import { FeatureRow } from "@/src/components/FeatureRow";
import LayeredDiagram from "@/src/components/LayeredDiagram/index";
import {
  CLIENT_NODES,
  COMPOSITE_LINE,
  REQUESTS,
  TIER_NODES,
} from "@/src/components/LayeredDiagram/diagram";
import { Section } from "@/src/components/Section";
import { SectionHeading } from "@/src/components/SectionHeading";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { Card } from "@/src/design-system/Card";
import { Link } from "@/src/design-system/Link";

import type { CopyLink } from "./content";
import { CLOSING_BAND, FEATURES, SECTIONS } from "./content";
import { FusionHero } from "./hero/FusionHero";
import { InsightsWindow } from "./visuals/InsightsWindow";
import { PerformanceWindow } from "./visuals/PerformanceWindow";
import { ProtocolsWindow } from "./visuals/ProtocolsWindow";
import { SecurityWindow } from "./visuals/SecurityWindow";

/**
 * The Fusion product page: hero copy and buttons, then the diagram panel,
 * one feature row per claim, the feature grid, and the closing band.
 * All words come from `./content`.
 */

const PANEL_CLASS = "border-cc-card-border bg-cc-card-bg rounded-xl border";

/** client-safety row: impact of removing `Product.rating` on real clients. */
const CLIENT_IMPACT_ROWS = [
  { client: "web", environment: "production", ok: 5, total: 5, status: "ok" },
  {
    client: "mobile",
    environment: "production",
    ok: 3,
    total: 5,
    status: "risk",
  },
  {
    client: "partner-api",
    environment: "sandbox",
    ok: 0,
    total: 0,
    status: "outside",
  },
  {
    client: "internal-admin",
    environment: "staging",
    ok: 6,
    total: 6,
    status: "ok",
  },
] as const;

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
}

/** One panel per text section, in the order the copy declares them. */
const VISUALS: Readonly<Record<string, Panel>> = {
  "what-is-fusion": {
    visual: (
      <LayeredDiagram
        gatewayLabel="Fusion"
        compositionLine={COMPOSITE_LINE}
        clients={CLIENT_NODES}
        tiers={TIER_NODES}
        requests={REQUESTS}
      />
    ),
  },
  performance: { visual: <PerformanceWindow /> },
  security: { visual: <SecurityWindow /> },
  insights: { visual: <InsightsWindow /> },
  "client-safety": {
    visual: (
      <ClientImpactMatrix
        density="comfortable"
        title="client registry · impact of removing Product.rating"
        rows={CLIENT_IMPACT_ROWS}
      />
    ),
  },
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
          <section key={section.id} id={section.id} className="py-16">
            <FeatureRow
              title={section.title}
              body={
                bullets ? undefined : withLinks(firstParagraph, section.links)
              }
              visual={
                <div className={`${PANEL_CLASS} overflow-hidden`}>
                  {panel.visual}
                </div>
              }
              reverse={i % 2 === 1}
            >
              {bullets && (
                <CheckList items={bullets} size="base" className="mt-4" />
              )}
              {!bullets && restParagraphs.length > 0 && (
                <div className="text-cc-ink mt-4 space-y-4 text-base">
                  {restParagraphs.map((paragraph) => (
                    <p key={paragraph}>{withLinks(paragraph, section.links)}</p>
                  ))}
                </div>
              )}
              <InPractice links={section.inPractice} />
            </FeatureRow>
          </section>
        );
      })}

      <Section title="Built for Distributed Graphs">
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

      <div id={CLOSING_BAND.id} className="scroll-mt-24">
        <Band
          className="py-16"
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
            <div className={`${PANEL_CLASS} overflow-hidden`}>
              <ProtocolsWindow />
            </div>
          }
        />
      </div>
    </>
  );
}
