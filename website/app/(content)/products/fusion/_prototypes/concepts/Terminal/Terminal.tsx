import { Fragment } from "react";
import type { ComponentType, ReactNode } from "react";

import { PageSection } from "@/src/components/PageSection";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { Eyebrow } from "@/src/design-system/Eyebrow";
import { Link } from "@/src/design-system/Link";

import type { CopyLink, CopySection } from "../../copy";
import { HERO, NITRO_BAND, SECTIONS } from "../../copy";
import { Scene } from "../../Primitives";
import { FanOutLog } from "./FanOutLog";
import { MetricsTail } from "./MetricsTail";
import { MONO, TERM } from "./palette";
import { PipelineRun } from "./PipelineRun";
import { RegistryTable } from "./RegistryTable";
import { SpecDiff } from "./SpecDiff";
import { TerminalHero } from "./TerminalHero";

/**
 * Fusion product page, concept v8: Terminal.
 *
 * The page has no illustration at all. Every claim is demonstrated in the one
 * medium the reader's own graph lives in: monospace text and box-drawing
 * characters. The hero is a live session composing the graph, and each text
 * block is answered by a different text form - a log tail, a two-file diff, a
 * failing CI run, a registry table, a watch command. Around those runs an
 * editorial layout: oversized headings on the dark surface, a mono index rail
 * down the left, nothing else. The words are the production page's, imported
 * from `../../copy`.
 */

interface Block {
  readonly id: string;
  /** Index printed on the rail, e.g. "01". */
  readonly index: string;
  /** The command the block's visual is the output of. */
  readonly command: string;
  readonly Visual: ComponentType;
  readonly ratio: string;
  /** Accessible name for the scene; the text inside it is decorative. */
  readonly label: string;
}

/** One terminal output per text block, in the order the copy lists them. */
const BLOCKS: readonly Block[] = [
  {
    id: "what-is-fusion",
    index: "01",
    command: "tail -f gateway.log",
    Visual: FanOutLog,
    ratio: "16 / 9",
    label:
      "A gateway log tail: one query from one client fans out to four subgraphs and the four results merge into one response.",
  },
  {
    id: "both-specifications",
    index: "02",
    command: "fusion compose --explain Product",
    Visual: SpecDiff,
    ratio: "1 / 1",
    label:
      "Two source schemas side by side, one written to the GraphQL Federation specification and one to Apollo Federation, composing into one composite schema together with an OpenAPI and a gRPC source.",
  },
  {
    id: "any-server",
    index: "03",
    command: "ci: fusion compose ./sources",
    Visual: PipelineRun,
    ratio: "16 / 9",
    label:
      "A CI run checking five subgraphs written in five languages, then failing on a type conflict and exiting non-zero before anything is deployed.",
  },
  {
    id: "client-safety",
    index: "04",
    command: "nitro schema check --against registry",
    Visual: RegistryTable,
    ratio: "16 / 9",
    label:
      "Composition passes while Nitro scans the operation registry and marks the mobile client's published operation as breaking.",
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

interface OutputProps {
  readonly block: Block;
}

/** A scene, framed the way a terminal window is: a rule and a command above it. */
function Output({ block }: OutputProps) {
  const { Visual } = block;

  return (
    <figure className="m-0">
      <figcaption
        className="text-caption flex items-baseline gap-[1ch] pb-2"
        style={{ color: TERM.dim, fontFamily: MONO }}
      >
        <span style={{ color: TERM.prompt }}>$</span>
        <span className="min-w-0 truncate">{block.command}</span>
      </figcaption>
      <div className="border-cc-card-border overflow-hidden rounded-lg border">
        <Scene ratio={block.ratio} label={block.label}>
          <Visual />
        </Scene>
      </div>
    </figure>
  );
}

interface BlockSectionProps {
  readonly block: Block;
  readonly section: CopySection;
  readonly flipped: boolean;
}

function BlockSection({ block, section, flipped }: BlockSectionProps) {
  return (
    <div id={section.id}>
      <PageSection maxWidth="6xl" className="py-20 sm:py-28">
        <div className="grid items-start gap-10 lg:grid-cols-2 lg:gap-16">
          <div className={flipped ? "lg:order-2" : undefined}>
            <Eyebrow size="2xs">
              {`${block.index} — `}
              {section.id}
            </Eyebrow>
            <h2 className="font-heading text-cc-heading text-h4 sm:text-h3 mt-4 text-balance">
              {section.title}
            </h2>
            <div className="text-cc-prose text-body mt-6 space-y-4">
              {section.paragraphs.map((paragraph) => (
                <p key={paragraph.slice(0, 24)}>
                  {withLinks(paragraph, section.links)}
                </p>
              ))}
            </div>
            <InPractice links={section.inPractice} />
          </div>

          <div className={flipped ? "lg:order-1" : undefined}>
            <Output block={block} />
          </div>
        </div>
      </PageSection>
    </div>
  );
}

/** A full-width mono rule, the way a shell separates two runs. */
function Rule() {
  return (
    <div aria-hidden="true" className="mx-auto max-w-6xl px-5 sm:px-12">
      <div
        className="overflow-hidden text-[10px] leading-none"
        style={{ color: TERM.faint, fontFamily: MONO, whiteSpace: "pre" }}
      >
        {"─".repeat(400)}
      </div>
    </div>
  );
}

export function Terminal() {
  return (
    <div className="bg-cc-bg">
      {/* Hero: the session that composes the graph */}
      <PageSection
        maxWidth="7xl"
        className="flex min-h-[88svh] flex-col justify-center py-20 sm:py-28"
      >
        <div className="grid items-center gap-12 lg:grid-cols-[1.05fr_1fr] lg:gap-16">
          <div>
            <Eyebrow size="2xs">{HERO.eyebrow}</Eyebrow>
            <h1 className="font-heading text-cc-heading text-h2 sm:text-h1 mt-5 tracking-tight text-balance">
              {HERO.title}
            </h1>
            <p className="text-cc-prose text-body sm:text-lead mt-6 max-w-2xl">
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
          </div>

          <div className="border-cc-card-border overflow-hidden rounded-lg border">
            <Scene
              ratio="1 / 1"
              label="A terminal session running fusion compose: five subgraph files and two non-GraphQL sources are read in and printed as one composite schema."
            >
              <TerminalHero />
            </Scene>
          </div>
        </div>
      </PageSection>

      {BLOCKS.map((block, i) => {
        const section = SECTIONS.find((s) => s.id === block.id);
        if (!section) return null;

        return (
          <Fragment key={block.id}>
            <Rule />
            <BlockSection
              block={block}
              section={section}
              flipped={i % 2 === 1}
            />
          </Fragment>
        );
      })}

      <Rule />

      {/* Nitro band: the watch command left running */}
      <div id={NITRO_BAND.id}>
        <PageSection maxWidth="6xl" className="py-20 sm:py-28">
          <div className="grid items-start gap-10 lg:grid-cols-2 lg:gap-16">
            <div>
              <Eyebrow size="2xs">05 — nitro</Eyebrow>
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

            <Output
              block={{
                id: NITRO_BAND.id,
                index: "05",
                command: "nitro watch --fusion",
                Visual: MetricsTail,
                ratio: "2 / 1",
                label:
                  "A live watch command reporting latency, throughput and error rate for the gateway and for each subgraph behind it.",
              }}
            />
          </div>
        </PageSection>
      </div>
    </div>
  );
}
