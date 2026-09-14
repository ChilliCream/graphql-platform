import { Fragment } from "react";
import type { ReactElement, ReactNode } from "react";

import { PageSection } from "@/src/components/PageSection";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { Eyebrow } from "@/src/design-system/Eyebrow";
import { Link } from "@/src/design-system/Link";

import { CC } from "../../brand";
import { Scene } from "../../Primitives";
import type { CopyLink, CopySection } from "../../copy";
import { HERO, NITRO_BAND, SECTIONS } from "../../copy";
import { GravityCheck } from "./GravityCheck";
import { LightPulse } from "./LightPulse";
import { ObservatoryLog } from "./ObservatoryLog";
import { CN } from "./palette";
import { RingSpectrum } from "./RingSpectrum";
import { SkySurvey } from "./SkySurvey";
import { StarField } from "./StarField";

/**
 * Fusion product page, concept v9: Constellation.
 *
 * The gateway is a star, the subgraphs are planets on its orbits and the
 * clients are probes on the rim. The hero is the whole system forming out of a
 * parallax starfield, each text block sits beside the observation that proves
 * its claim, and the page closes on the observatory's sky survey. All words
 * come from `../../copy`.
 *
 * The sky is built out of the site's own grid, type scale and tokens: the page
 * keeps `bg-cc-bg`, every band is the shared `max-w-6xl` container on the
 * site's vertical rhythm, and the night sky stays inside the bounded scene
 * boxes and the hero scrim.
 */

const PLATE_CLASS =
  "border-cc-card-border bg-cc-card-bg overflow-hidden rounded-2xl border";

/** Hero scrim: the page background itself, so the copy sits on the page colour. */
const scrim = (percent: number) =>
  `color-mix(in srgb, ${CC.bg} ${percent}%, transparent)`;

const HERO_SCRIM = `radial-gradient(125% 85% at 8% 50%, ${scrim(96)} 0%, ${scrim(86)} 40%, ${scrim(15)} 76%)`;

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

/** One observation per text block, in the order the copy declares them. */
const VISUALS: Readonly<Record<string, ReactNode>> = {
  "what-is-fusion": <LightPulse />,
  "both-specifications": <RingSpectrum />,
  "any-server": <GravityCheck />,
  "client-safety": <ObservatoryLog />,
};

interface OrbitLabelProps {
  readonly index: number;
  readonly children: ReactNode;
}

/** The orbit marker that numbers a block, drawn as a ring around its index. */
function OrbitLabel({ index, children }: OrbitLabelProps) {
  return (
    <Eyebrow size="2xs" className="mb-4 flex items-center gap-3">
      <span
        aria-hidden="true"
        className="border-cc-card-border inline-flex h-7 w-7 items-center justify-center rounded-full border"
        style={{ color: CN.star }}
      >
        {String(index).padStart(2, "0")}
      </span>
      {children}
    </Eyebrow>
  );
}

interface ObservationProps {
  readonly index: number;
  readonly section: CopySection;
  readonly visual: ReactNode;
}

/** One text block and the observation that demonstrates its claim. */
function Observation({ index, section, visual }: ObservationProps) {
  const flip = index % 2 === 0;

  return (
    <section className="border-cc-card-border border-t">
      <div className="mx-auto max-w-6xl px-5 py-20 sm:px-12 sm:py-28">
        <div
          id={section.id}
          className="grid items-center gap-10 lg:grid-cols-2"
        >
          <div className={flip ? "lg:order-2" : undefined}>
            <OrbitLabel index={index}>{`Orbit ${index}`}</OrbitLabel>
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

          <div className={`${PLATE_CLASS} ${flip ? "lg:order-1" : ""}`.trim()}>
            <Scene ratio="16 / 11">{visual}</Scene>
          </div>
        </div>
      </div>
    </section>
  );
}

export function Constellation() {
  return (
    <div className="bg-cc-bg">
      <section className="relative flex min-h-[88svh] items-center overflow-hidden">
        <StarField />
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
        <Observation
          key={section.id}
          index={i + 1}
          section={section}
          visual={VISUALS[section.id]}
        />
      ))}

      <section className="border-cc-card-border border-t">
        <div className="mx-auto max-w-6xl px-5 py-20 sm:px-12 sm:py-28">
          <div id={NITRO_BAND.id} className={`${PLATE_CLASS} p-8 sm:p-10`}>
            <div className="grid items-center gap-8 lg:grid-cols-2">
              <div>
                <OrbitLabel index={SECTIONS.length + 1}>Observatory</OrbitLabel>
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
              <div className={PLATE_CLASS}>
                <Scene ratio="16 / 6">
                  <SkySurvey />
                </Scene>
              </div>
            </div>
          </div>
        </div>
      </section>
    </div>
  );
}
