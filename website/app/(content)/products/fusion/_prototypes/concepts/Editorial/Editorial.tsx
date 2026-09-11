import { Fragment } from "react";
import type { ComponentType, ReactNode } from "react";

import { PageSection } from "@/src/components/PageSection";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { Eyebrow } from "@/src/design-system/Eyebrow";
import { Link } from "@/src/design-system/Link";

import type { CopyLink, CopySection } from "../../copy";
import { HERO, NITRO_BAND, SECTIONS } from "../../copy";
import { Scene } from "../../Primitives";
import { ArrowFan } from "./ArrowFan";
import { HeroSketch } from "./HeroSketch";
import { HighlighterOps } from "./HighlighterOps";
import { MarginNotes } from "./MarginNotes";
import { PAPER } from "./palette";
import { RedPenProof } from "./RedPenProof";
import { SpecStickies } from "./SpecStickies";

/**
 * Fusion product page, concept v10: Editorial.
 *
 * The page is set as a magazine feature that someone has drawn all over: a
 * standfirst line pulled out of each section's first paragraph, a folio mark
 * and a handwritten caption under every plate, and a marker-drawn figure
 * beside every text block that draws itself when it comes into view. The words
 * are the production page's, imported from `../../copy`; only the marginalia
 * and the six scenes belong to the concept.
 *
 * The spread is set in the site's own chrome: the page ground stays
 * `bg-cc-bg`, every band is a `PageSection` on the site's container and
 * rhythm, headings and copy sit on the type scale, and the drawn plates are
 * bounded scene boxes on `bg-cc-surface`.
 */

interface Plate {
  readonly id: string;
  /** Figure number in the margin. */
  readonly folio: string;
  readonly Visual: ComponentType;
  readonly ratio: string;
  /** Accessible name for the scene; the SVG inside it is decorative. */
  readonly label: string;
  /** Handwritten caption printed under the figure. */
  readonly caption: string;
}

/** One drawn plate per text block, in the order the copy lists them. */
const PLATES: readonly Plate[] = [
  {
    id: "what-is-fusion",
    folio: "Fig. 1",
    Visual: ArrowFan,
    ratio: "4 / 3",
    label:
      "One query is drawn as a fan of hand-drawn arrows from a client through the gateway to four subgraphs and back as one response.",
    caption: "One query in, a fan of calls out, one response back.",
  },
  {
    id: "both-specifications",
    folio: "Fig. 2",
    Visual: SpecStickies,
    ratio: "4 / 3",
    label:
      "Sticky notes labelled GraphQL Federation and Apollo Federation, plus taped OpenAPI and gRPC cards, pinned onto one composite schema sheet.",
    caption: "Either specification, plus OpenAPI and gRPC, on one sheet.",
  },
  {
    id: "any-server",
    folio: "Fig. 3",
    Visual: RedPenProof,
    ratio: "4 / 3",
    label:
      "A proof sheet of five source schemas checked in green pen; a conflicting field is crossed out in red and the build stops.",
    caption: "Composition reads the proof and stops on a conflict.",
  },
  {
    id: "client-safety",
    folio: "Fig. 4",
    Visual: HighlighterOps,
    ratio: "4 / 3",
    label:
      "A removed field passes composition while a highlighter marks the client operations that still ask for it as breaking.",
    caption: "The highlighter finds the operations clients really run.",
  },
];

/**
 * Re-links the phrases the production page links, without touching a word of
 * the paragraph: the label is matched verbatim inside the string and wrapped.
 */
function withLinks(text: string, links: readonly CopyLink[]): ReactNode {
  let parts: ReactNode[] = [text];

  for (const link of links) {
    const next: ReactNode[] = [];
    for (const part of parts) {
      if (typeof part !== "string") {
        next.push(part);
        continue;
      }
      const at = part.indexOf(link.label);
      if (at === -1) {
        next.push(part);
        continue;
      }
      next.push(
        part.slice(0, at),
        <Link key={link.href} href={link.href}>
          {link.label}
        </Link>,
        part.slice(at + link.label.length),
      );
    }
    parts = next;
  }

  return parts.map((part, i) => <Fragment key={i}>{part}</Fragment>);
}

/**
 * Splits the opening paragraph after its first sentence so the sentence can be
 * set as the standfirst and the remainder run on as body copy. No word is
 * added, dropped or reordered; the split point is the first ". " in the text.
 */
function splitLead(text: string): readonly [string, string] {
  const at = text.indexOf(". ");
  if (at === -1) return [text, ""];
  return [text.slice(0, at + 1), text.slice(at + 2)];
}

interface InPracticeProps {
  readonly links: readonly CopyLink[];
}

function InPractice({ links }: InPracticeProps) {
  if (links.length === 0) return null;

  return (
    <p className="text-cc-ink-dim text-caption mt-6">
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

interface PlateFigureProps {
  readonly plate: Plate;
}

/** The drawn figure with its folio mark and handwritten caption. */
function PlateFigure({ plate }: PlateFigureProps) {
  const { Visual } = plate;

  return (
    <figure className="m-0">
      <div className="border-cc-card-border bg-cc-surface overflow-hidden border">
        <Scene ratio={plate.ratio} label={plate.label}>
          <Visual />
        </Scene>
      </div>
      <figcaption className="text-cc-ink-dim text-caption mt-3 flex items-baseline gap-3">
        <Eyebrow as="span">{plate.folio}</Eyebrow>
        <span>{plate.caption}</span>
      </figcaption>
    </figure>
  );
}

interface ArticleProps {
  readonly plate: Plate;
  readonly section: CopySection;
  readonly flipped: boolean;
}

function Article({ plate, section, flipped }: ArticleProps) {
  const [lead, rest] = splitLead(section.paragraphs[0]);
  const tail = section.paragraphs.slice(1);

  return (
    <article id={section.id}>
      <PageSection maxWidth="6xl" className="py-20 sm:py-28">
        <div className="grid items-start gap-10 lg:grid-cols-12 lg:gap-16">
          <div
            className={flipped ? "lg:order-2 lg:col-span-5" : "lg:col-span-5"}
          >
            <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 text-balance">
              {section.title}
            </h2>
            <p className="text-cc-heading text-body sm:text-lead mt-6 leading-snug">
              {withLinks(lead, section.links)}
            </p>
            <div className="text-cc-prose text-body mt-5 space-y-4">
              {rest ? <p>{withLinks(rest, section.links)}</p> : null}
              {tail.map((paragraph) => (
                <p key={paragraph.slice(0, 24)}>
                  {withLinks(paragraph, section.links)}
                </p>
              ))}
            </div>
            <InPractice links={section.inPractice} />
          </div>

          <div
            className={flipped ? "lg:order-1 lg:col-span-7" : "lg:col-span-7"}
          >
            <PlateFigure plate={plate} />
          </div>
        </div>
      </PageSection>
    </article>
  );
}

/** The site's section rule between two articles, ruled over by hand. */
function PenRule() {
  return (
    <div aria-hidden="true" className="border-cc-card-border border-t">
      <div className="mx-auto max-w-6xl px-5 sm:px-12">
        <svg viewBox="0 0 1000 12" className="h-3 w-full" role="presentation">
          <path
            d="M2 7 C 180 2, 320 11, 500 6 S 840 2, 998 7"
            fill="none"
            stroke={PAPER.pencil}
            strokeWidth={1.6}
            strokeLinecap="round"
            opacity={0.5}
          />
        </svg>
      </div>
    </div>
  );
}

export function Editorial() {
  return (
    <div className="bg-cc-bg">
      {/* Cover: the headline over a gateway drawing itself */}
      <section className="relative isolate flex min-h-[88svh] items-end overflow-hidden">
        <div className="absolute inset-0">
          <Scene className="h-full">
            <HeroSketch />
          </Scene>
        </div>
        <div
          aria-hidden="true"
          className="from-cc-bg via-cc-bg/75 absolute inset-0 bg-gradient-to-t to-transparent"
        />
        <PageSection maxWidth="7xl" className="relative pt-40 pb-20">
          <div className="flex items-center gap-4">
            <Eyebrow as="span">{HERO.eyebrow}</Eyebrow>
            <span className="bg-cc-ink-faint h-px flex-1" />
            <Eyebrow as="span" className="hidden sm:inline">
              Feature
            </Eyebrow>
          </div>
          <h1 className="font-heading text-cc-heading text-h2 sm:text-h1 mt-6">
            {HERO.title}
          </h1>
          <p className="text-cc-heading text-body sm:text-lead mt-8 max-w-3xl leading-snug">
            {HERO.teaser}
          </p>
          <div className="mt-10 flex flex-wrap gap-4">
            <SolidButton href={HERO.buttons[0].href}>
              {HERO.buttons[0].label}
            </SolidButton>
            <OutlineButton href={HERO.buttons[1].href}>
              {HERO.buttons[1].label}
            </OutlineButton>
          </div>
        </PageSection>
      </section>

      {PLATES.map((plate, i) => {
        const section = SECTIONS.find((s) => s.id === plate.id);
        if (!section) return null;

        return (
          <Fragment key={plate.id}>
            {i > 0 ? <PenRule /> : null}
            <Article plate={plate} section={section} flipped={i % 2 === 1} />
          </Fragment>
        );
      })}

      {/* Back page: the editor's own margin notes */}
      <div id={NITRO_BAND.id} className="border-cc-card-border border-t">
        <PageSection maxWidth="6xl" className="py-20 sm:py-28">
          <div className="grid items-center gap-10 lg:grid-cols-12 lg:gap-16">
            <div className="lg:col-span-5">
              <Eyebrow>Margin notes</Eyebrow>
              <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 mt-4 text-balance">
                {NITRO_BAND.title}
              </h2>
              <p className="text-cc-prose text-body mt-6">
                {NITRO_BAND.description}
              </p>
              <div className="mt-10 flex flex-wrap gap-4">
                <SolidButton href={NITRO_BAND.buttons[0].href}>
                  {NITRO_BAND.buttons[0].label}
                </SolidButton>
                <OutlineButton href={NITRO_BAND.buttons[1].href}>
                  {NITRO_BAND.buttons[1].label}
                </OutlineButton>
              </div>
            </div>
            <div className="lg:col-span-7">
              <div className="border-cc-card-border bg-cc-surface overflow-hidden border">
                <Scene
                  ratio="16 / 9"
                  label="Hand-drawn margin charts of latency, throughput and error rate for the gateway and each subgraph behind it."
                >
                  <MarginNotes />
                </Scene>
              </div>
            </div>
          </div>
        </PageSection>
      </div>
    </div>
  );
}
