import { Fragment } from "react";
import type { ReactElement, ReactNode } from "react";

import { CardGrid } from "@/src/components/CardGrid";
import { PageSection } from "@/src/components/PageSection";
import { Section } from "@/src/components/Section";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { Card } from "@/src/design-system/Card";
import { Eyebrow } from "@/src/design-system/Eyebrow";
import { Link } from "@/src/design-system/Link";

import type { CopyLink, CopySection } from "./content";
import { FEATURES, HERO, NITRO_BAND, SECTIONS } from "./content";
import LayeredDiagram from "./hero/LayeredDiagram";
import { CC } from "./tokens";
import {
  FLIGHT_RECORDER_RATIO,
  FlightRecorder,
} from "./visuals/FlightRecorder";
import {
  PREFLIGHT_CHECKLIST_RATIO,
  PreflightChecklist,
} from "./visuals/PreflightChecklist";
import { QUERY_TRACK_RATIO, QueryTrack } from "./visuals/QueryTrack";
import { Scene } from "./visuals/Scene";
import { SPEC_PATCHBAY_RATIO, SpecPatchbay } from "./visuals/SpecPatchbay";
import {
  TELEMETRY_STRIP_RATIO,
  TelemetryStrip,
} from "./visuals/TelemetryStrip";

/**
 * The Fusion product page: a hero over the layered architecture diagram,
 * one console row per claim in the body copy, the feature grid, and the
 * closing Nitro band. All words come from `./content`.
 */

const PANEL_CLASS =
  "border-cc-card-border bg-cc-card-bg rounded-xl border backdrop-blur-[2px]";

/** Hero scrim: the page background itself, so the copy sits on the page colour. */
const scrim = (percent: number) =>
  `color-mix(in srgb, ${CC.bg} ${percent}%, transparent)`;

const HERO_SCRIM = `radial-gradient(120% 80% at 12% 50%, ${scrim(94)} 0%, ${scrim(82)} 42%, ${scrim(20)} 78%)`;

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
  /** The visual's own `viewBox` ratio, so the scene box never letterboxes it. */
  readonly ratio: string;
}

/** One console panel per text block, in the order the copy declares them. */
const VISUALS: Readonly<Record<string, Panel>> = {
  "what-is-fusion": { visual: <QueryTrack />, ratio: QUERY_TRACK_RATIO },
  "both-specifications": {
    visual: <SpecPatchbay />,
    ratio: SPEC_PATCHBAY_RATIO,
  },
  "any-server": {
    visual: <PreflightChecklist />,
    ratio: PREFLIGHT_CHECKLIST_RATIO,
  },
  "client-safety": {
    visual: <FlightRecorder />,
    ratio: FLIGHT_RECORDER_RATIO,
  },
};

interface ConsoleRowProps {
  readonly index: number;
  readonly section: CopySection;
  readonly panel: Panel;
}

/** One text block and the console panel that demonstrates its claim. */
function ConsoleRow({ index, section, panel }: ConsoleRowProps) {
  const flip = index % 2 === 1;

  return (
    <section className="border-cc-card-border border-t">
      <div className="mx-auto max-w-6xl px-5 py-20 sm:px-12 sm:py-28">
        <div
          id={section.id}
          className="grid items-center gap-10 lg:grid-cols-2"
        >
          <div className={flip ? "lg:order-2" : undefined}>
            <Eyebrow color="ink-dim" size="2xs" className="mb-3">
              {String(index + 1).padStart(2, "0")}
            </Eyebrow>
            <h2 className="text-cc-heading font-heading text-h4 sm:text-h3 mb-5">
              {section.title}
            </h2>
            <div className="text-cc-ink text-body space-y-4">
              {section.paragraphs.map((paragraph) => (
                <p key={paragraph}>{withLinks(paragraph, section.links)}</p>
              ))}
              <InPractice links={section.inPractice} />
            </div>
          </div>

          <div
            className={`${PANEL_CLASS} overflow-hidden ${flip ? "lg:order-1" : ""}`}
          >
            <Scene ratio={panel.ratio}>{panel.visual}</Scene>
          </div>
        </div>
      </div>
    </section>
  );
}

export function FusionPage() {
  return (
    <div className="bg-cc-bg">
      <section className="relative flex min-h-[88svh] items-center overflow-hidden">
        <LayeredDiagram />
        <div
          aria-hidden="true"
          className="absolute inset-0"
          style={{ background: HERO_SCRIM }}
        />
        <PageSection maxWidth="7xl" className="relative w-full py-20 sm:py-28">
          <div className="max-w-xl">
            <Eyebrow color="accent" className="mb-5">
              {HERO.eyebrow}
            </Eyebrow>
            <h1 className="text-cc-heading font-heading text-h2 sm:text-h1 mb-6">
              {HERO.title}
            </h1>
            <p className="text-cc-prose lead mb-8">{HERO.teaser}</p>
            <div className="flex flex-wrap gap-3">
              <SolidButton href={HERO.buttons[0].href}>
                {HERO.buttons[0].label}
              </SolidButton>
              <OutlineButton href={HERO.buttons[1].href}>
                {HERO.buttons[1].label}
              </OutlineButton>
            </div>
          </div>
        </PageSection>
      </section>

      {SECTIONS.map((section, i) => (
        <ConsoleRow
          key={section.id}
          index={i}
          section={section}
          panel={VISUALS[section.id]}
        />
      ))}

      <section className="border-cc-card-border border-t">
        <div className="mx-auto max-w-6xl px-5 py-20 sm:px-12 sm:py-28">
          <Eyebrow color="ink-dim" size="2xs" className="mb-3">
            {String(SECTIONS.length + 1).padStart(2, "0")}
          </Eyebrow>
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
        </div>
      </section>

      <section className="border-cc-card-border border-t">
        <div className="mx-auto max-w-6xl px-5 py-20 sm:px-12 sm:py-28">
          <div id={NITRO_BAND.id} className={`${PANEL_CLASS} p-8 sm:p-10`}>
            <div className="grid items-center gap-8 lg:grid-cols-2">
              <div>
                <Eyebrow color="ink-dim" size="2xs" className="mb-3">
                  {String(SECTIONS.length + 2).padStart(2, "0")}
                </Eyebrow>
                <h2 className="text-cc-heading font-heading text-h4 mb-4">
                  {NITRO_BAND.title}
                </h2>
                <p className="text-cc-ink text-body mb-6">
                  {NITRO_BAND.description}
                </p>
                <div className="flex flex-wrap gap-3">
                  <SolidButton href={NITRO_BAND.buttons[0].href}>
                    {NITRO_BAND.buttons[0].label}
                  </SolidButton>
                  <OutlineButton href={NITRO_BAND.buttons[1].href}>
                    {NITRO_BAND.buttons[1].label}
                  </OutlineButton>
                </div>
              </div>
              <div className="border-cc-card-border overflow-hidden rounded-lg border">
                <Scene ratio={TELEMETRY_STRIP_RATIO}>
                  <TelemetryStrip />
                </Scene>
              </div>
            </div>
          </div>
        </div>
      </section>
    </div>
  );
}
