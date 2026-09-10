import { Fragment } from "react";
import type { ReactElement, ReactNode } from "react";

import { PageSection } from "@/src/components/PageSection";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { Link } from "@/src/design-system/Link";

import { Scene } from "../../Primitives";
import type { CopyLink, CopySection } from "../../copy";
import { HERO, NITRO_BAND, SECTIONS } from "../../copy";
import { FlightRecorder } from "./FlightRecorder";
import { MC } from "./palette";
import { PreflightChecklist } from "./PreflightChecklist";
import { QueryTrack } from "./QueryTrack";
import { SpecPatchbay } from "./SpecPatchbay";
import { TelemetryStrip } from "./TelemetryStrip";
import { WallMap } from "./WallMap";

/**
 * Fusion product page, concept v1: Mission Control.
 *
 * A telemetry room. The hero is the wall map with its radar sweep, each text
 * block sits next to the console panel that demonstrates it, and the page
 * closes on the Nitro flight recorder. All words come from `../../copy`.
 */

const PANEL_CLASS = "rounded-xl border backdrop-blur-[2px]";

const panelStyle = {
  background: MC.panel,
  borderColor: MC.panelEdge,
} as const;

const monoStyle = { fontFamily: MC.mono } as const;

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
    <p className="text-cc-ink-dim text-sm">
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

/** One console panel per text block, in the order the copy declares them. */
const VISUALS: Readonly<Record<string, ReactNode>> = {
  "what-is-fusion": <QueryTrack />,
  "both-specifications": <SpecPatchbay />,
  "any-server": <PreflightChecklist />,
  "client-safety": <FlightRecorder />,
};

interface ConsoleRowProps {
  readonly index: number;
  readonly section: CopySection;
  readonly visual: ReactNode;
}

/** One text block and the console panel that demonstrates its claim. */
function ConsoleRow({ index, section, visual }: ConsoleRowProps) {
  const flip = index % 2 === 1;

  return (
    <PageSection maxWidth="6xl" className="py-16 sm:py-24">
      <div id={section.id} className="grid items-center gap-10 lg:grid-cols-2">
        <div className={flip ? "lg:order-2" : undefined}>
          <p
            className="mb-3 text-[10px] tracking-[0.28em]"
            style={{ ...monoStyle, color: MC.dim }}
          >
            {String(index + 1).padStart(2, "0")}
          </p>
          <h2 className="text-cc-heading text-h3 font-heading mb-5">
            {section.title}
          </h2>
          <div className="text-cc-ink-dim space-y-4 text-base">
            {section.paragraphs.map((paragraph) => (
              <p key={paragraph}>{withLinks(paragraph, section.links)}</p>
            ))}
            <InPractice links={section.inPractice} />
          </div>
        </div>

        <div
          className={`${PANEL_CLASS} overflow-hidden ${flip ? "lg:order-1" : ""}`}
          style={panelStyle}
        >
          <Scene ratio="16 / 11">{visual}</Scene>
        </div>
      </div>
    </PageSection>
  );
}

export function MissionControl() {
  return (
    <div style={{ background: MC.bg }}>
      <section className="relative flex min-h-[88svh] items-center overflow-hidden">
        <WallMap />
        <div
          aria-hidden="true"
          className="absolute inset-0"
          style={{
            background:
              "radial-gradient(120% 80% at 12% 50%, rgba(5,10,18,0.94) 0%, rgba(5,10,18,0.82) 42%, rgba(5,10,18,0.2) 78%)",
          }}
        />
        <PageSection maxWidth="6xl" className="relative py-24">
          <div className="max-w-xl">
            <p
              className="mb-5 text-[11px] tracking-[0.28em] uppercase"
              style={{ ...monoStyle, color: MC.phosphor }}
            >
              {HERO.eyebrow}
            </p>
            <h1 className="text-cc-heading text-hero font-heading mb-6">
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
          visual={VISUALS[section.id]}
        />
      ))}

      <PageSection maxWidth="6xl" className="pb-24">
        <div
          id={NITRO_BAND.id}
          className={`${PANEL_CLASS} p-8 sm:p-10`}
          style={panelStyle}
        >
          <div className="grid items-center gap-8 lg:grid-cols-2">
            <div>
              <p
                className="mb-3 text-[10px] tracking-[0.28em]"
                style={{ ...monoStyle, color: MC.dim }}
              >
                {String(SECTIONS.length + 1).padStart(2, "0")}
              </p>
              <h2 className="text-cc-heading text-h4 font-heading mb-4">
                {NITRO_BAND.title}
              </h2>
              <p className="text-cc-ink-dim mb-6 text-base">
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
            <div
              className="overflow-hidden rounded-lg border"
              style={panelStyle}
            >
              <Scene ratio="16 / 9">
                <TelemetryStrip />
              </Scene>
            </div>
          </div>
        </div>
      </PageSection>
    </div>
  );
}
