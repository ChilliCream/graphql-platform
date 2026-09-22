import { Fragment } from "react";
import type { ReactElement, ReactNode } from "react";

import { Band } from "@/src/components/Band";
import { ButtonRow } from "@/src/components/ButtonRow";
import { CardGrid } from "@/src/components/CardGrid";
import { FeatureRow } from "@/src/components/FeatureRow";
import { Section } from "@/src/components/Section";
import { SectionHeading } from "@/src/components/SectionHeading";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { Card } from "@/src/design-system/Card";
import { Link } from "@/src/design-system/Link";

import type { CopyLink } from "./content";
import { FEATURES, NITRO_BAND, SECTIONS } from "./content";
import { FusionHero } from "./hero/FusionHero";
import LayeredDiagram from "./hero/LayeredDiagram";
import { CompositionWindow } from "./visuals/CompositionWindow";
import {
  FLIGHT_RECORDER_RATIO,
  FlightRecorder,
} from "./visuals/FlightRecorder";
import { ProtocolsWindow } from "./visuals/ProtocolsWindow";
import { Scene } from "./visuals/Scene";
import {
  TELEMETRY_STRIP_RATIO,
  TelemetryStrip,
} from "./visuals/TelemetryStrip";

/**
 * The Fusion product page: hero copy and buttons, then the diagram panel,
 * one feature row per claim, the feature grid, and the closing Nitro band.
 * All words come from `./content`.
 */

const PANEL_CLASS = "border-cc-card-border bg-cc-card-bg rounded-xl border";

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
    <p className="text-cc-ink-dim text-caption">
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
  /**
   * The visual's own `viewBox` ratio, so the scene box never letterboxes it.
   * Only meaningful for `kind: "svg"` (the default); an `"html"` visual
   * sizes itself and ignores this.
   */
  readonly ratio?: string;
  /**
   * `"svg"` (the default) wraps `visual` in a ratio-locked `Scene`, which
   * every console visual on this page is drawn for. `"html"` renders
   * `visual` directly in the panel box instead: for a plain HTML component
   * with its own motion gating, such as `LayeredDiagram`, that already
   * sizes itself to its container.
   */
  readonly kind?: "svg" | "html";
}

/** One panel per text section, in the order the copy declares them. */
const VISUALS: Readonly<Record<string, Panel>> = {
  "what-is-fusion": { visual: <LayeredDiagram />, kind: "html" },
  "both-specifications": { visual: <ProtocolsWindow />, kind: "html" },
  "any-server": { visual: <CompositionWindow />, kind: "html" },
  "client-safety": {
    visual: <FlightRecorder />,
    ratio: FLIGHT_RECORDER_RATIO,
  },
};

export function FusionPage() {
  return (
    <>
      <FusionHero />

      {SECTIONS.map((section, i) => {
        const panel = VISUALS[section.id];
        const [firstParagraph, ...restParagraphs] = section.paragraphs;

        return (
          <section key={section.id} id={section.id} className="py-16">
            <FeatureRow
              title={section.title}
              body={withLinks(firstParagraph, section.links)}
              visual={
                <div className={`${PANEL_CLASS} overflow-hidden`}>
                  {panel.kind === "html" ? (
                    panel.visual
                  ) : (
                    <Scene ratio={panel.ratio}>{panel.visual}</Scene>
                  )}
                </div>
              }
              reverse={i % 2 === 1}
            >
              {restParagraphs.length > 0 && (
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

      <div id={NITRO_BAND.id} className="scroll-mt-24">
        <Band
          className="py-16"
          skin="accent"
          layout="split"
          main={
            <div>
              <SectionHeading
                title={NITRO_BAND.title}
                description={NITRO_BAND.description}
              />
              <ButtonRow align="start" className="mt-9">
                <SolidButton href={NITRO_BAND.buttons[0].href}>
                  {NITRO_BAND.buttons[0].label}
                </SolidButton>
                <OutlineButton href={NITRO_BAND.buttons[1].href}>
                  {NITRO_BAND.buttons[1].label}
                </OutlineButton>
              </ButtonRow>
            </div>
          }
          aside={
            <div className="border-cc-card-border overflow-hidden rounded-lg border">
              <Scene ratio={TELEMETRY_STRIP_RATIO}>
                <TelemetryStrip />
              </Scene>
            </div>
          }
        />
      </div>
    </>
  );
}
