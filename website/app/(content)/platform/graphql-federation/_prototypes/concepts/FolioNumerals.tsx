import { Fragment } from "react";
import type { ReactNode } from "react";

import { PageSection } from "@/src/components/PageSection";
import { RevealOnScroll } from "@/src/components/RevealOnScroll";

import { CANON, GatewayChip, HorizonRule, MicroLabel } from "../Primitives";
import { CHAPTERS, ProtoCodeBox } from "../story";
import type { Chapter } from "../story";

type Side = "left" | "right";

interface CaptionSpec {
  readonly text: string;
  readonly accents?: readonly string[];
  readonly dim?: boolean;
}

/** Which side of the plate the numeral sits on, alternating beat by beat. */
const SIDES: readonly Side[] = [
  "left",
  "right",
  "left",
  "right",
  "left",
  "right",
  "left",
];

/**
 * The recurring "MERGED BY: ___" caption per beat - the argument's spine,
 * printed once per plate so seven separate chapters read as one question
 * asked seven times. Beat 6 carries a second line; the rest stay one line.
 */
const CAPTIONS: readonly (readonly CaptionSpec[])[] = [
  [{ text: "MERGED BY: NOBODY — YET" }],
  [{ text: "MERGED BY: EVERY APP · AT RUNTIME" }],
  [{ text: "MERGED BY: ONE TEAM · IN A QUEUE" }],
  [{ text: "MERGED BY: ONE QUERY — BUT WHO WRITES THE SCHEMA?" }],
  [{ text: "MERGED BY: NO ONE CAN — AUTHORSHIP SPLITS" }],
  [
    {
      text: "MERGED BY: COMPOSITION · SCHEMAS ONLY · AT BUILD TIME",
      accents: ["COMPOSITION", "SCHEMAS ONLY"],
    },
    { text: "SCHEMAS 5 → 1 · SERVICES 5 → 5", dim: true },
  ],
  [
    {
      text: "MERGED BY: THE EXECUTOR · RESPONSES ONLY · AT RUNTIME",
      accents: ["THE EXECUTOR", "RESPONSES ONLY"],
    },
  ],
];

function escapeForRegExp(value: string): string {
  return value.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

/** One caption line, with the key "who merges" words in the answer accent. */
function CaptionLine({ text, accents, dim }: CaptionSpec) {
  const className =
    `font-mono text-[10px] tracking-[0.2em] uppercase text-cc-ink-dim ${
      dim ? "opacity-70" : ""
    }`.trim();

  if (!accents || accents.length === 0) {
    return <p className={className}>{text}</p>;
  }

  const pattern = new RegExp(
    `(${accents.map(escapeForRegExp).join("|")})`,
    "g",
  );
  const parts = text.split(pattern);

  return (
    <p className={className}>
      {parts.map((part, i) =>
        accents.includes(part) ? (
          <span key={i} className="text-[#5eead4]">
            {part}
          </span>
        ) : (
          <span key={i}>{part}</span>
        ),
      )}
    </p>
  );
}

/**
 * The oversized frontispiece numeral: a 1px-ish outline glyph in the
 * starfield's own faint ink, drawn as SVG text so the thin stroke stays
 * crisp instead of relying on `-webkit-text-stroke`.
 */
function NumeralSvg({ n }: { readonly n: string }) {
  return (
    <svg
      viewBox="0 0 320 240"
      aria-hidden="true"
      className="text-cc-ink-faint h-16 w-auto sm:aspect-[4/3] sm:h-auto sm:w-full sm:max-w-64"
    >
      <text
        x="0"
        y="200"
        fontFamily="var(--font-heading)"
        fontSize="210"
        fill="none"
        stroke="currentColor"
        strokeWidth="1"
        vectorEffect="non-scaling-stroke"
      >
        {n}
      </text>
    </svg>
  );
}

interface PlateProps {
  readonly index: number;
  readonly title: string;
  readonly children: ReactNode;
}

/**
 * One chapter plate: a numeral + baseline rule + caption in a 4-column cell,
 * the chapter's own title/copy/boxes in an 8-column cell, alternating sides
 * via grid order. Normal flow only - no absolute positioning, no scrim - so
 * copy and artwork always sit in separate tracks with real gap between them.
 */
function Plate({ index, title, children }: PlateProps) {
  const numeral = String(index + 1).padStart(2, "0");
  const side = SIDES[index];
  const isRight = side === "right";

  return (
    <RevealOnScroll>
      <div className="grid gap-8 py-16 sm:grid-cols-12 sm:gap-10 sm:py-24">
        <div
          className={`flex flex-col items-start sm:col-span-4 ${
            isRight
              ? "sm:order-last sm:items-end sm:text-right"
              : "sm:items-start"
          }`}
        >
          <NumeralSvg n={numeral} />
          <span
            aria-hidden="true"
            className={`border-cc-card-border mt-4 block h-px w-32 border-t sm:w-40 ${
              isRight ? "sm:ml-auto" : ""
            }`}
          />
          <div className="mt-3 space-y-1">
            {CAPTIONS[index].map((c, i) => (
              <CaptionLine key={i} {...c} />
            ))}
          </div>
        </div>
        <div className="sm:col-span-8">
          <h3 className="font-heading text-cc-heading text-h4 sm:text-h3 text-balance">
            {title}
          </h3>
          <div className="text-cc-ink mt-4 space-y-4 text-sm sm:text-base">
            {children}
          </div>
        </div>
      </div>
    </RevealOnScroll>
  );
}

function ServiceChip({
  service,
}: {
  readonly service: (typeof CANON)[number];
}) {
  return (
    <div className="border-cc-card-border flex flex-col gap-1 rounded-lg border px-3 py-2">
      <span className="flex items-center gap-1.5">
        <span
          className="inline-block h-2 w-2 rounded-full"
          style={{ background: service.color }}
        />
        <span className="text-cc-ink font-mono text-[10px] tracking-[0.15em] uppercase">
          {service.name}
        </span>
      </span>
      <span className="text-cc-ink-dim font-mono text-[9px] tracking-[0.12em] uppercase opacity-70">
        Own deploy
      </span>
    </div>
  );
}

/** Beat 6's evidence that composition merges schemas only: five services,
 * still separate, still individually deployed. */
function ServicesUntouched() {
  return (
    <div>
      <MicroLabel className="block">Services untouched</MicroLabel>
      <div className="mt-2 flex flex-wrap gap-2">
        {CANON.map((s) => (
          <ServiceChip key={s.name} service={s} />
        ))}
      </div>
    </div>
  );
}

/** Beat 7's runtime evidence: the gateway, and the calls it makes - never a
 * merged service. */
function RuntimeBox() {
  return (
    <div className="border-cc-card-border rounded-xl border bg-[#0d1424] p-4">
      <svg viewBox="0 0 88 26" aria-hidden="true" className="h-6 w-auto">
        <GatewayChip x={44} y={13} />
      </svg>
      <div className="border-cc-card-border mt-3 space-y-1 border-t pt-3 font-mono text-[12px] leading-6 text-[#c9d4e8]">
        <div>query → catalog.name</div>
        <div>query → billing.price</div>
        <div>query → shipping.delivery</div>
      </div>
    </div>
  );
}

function BeatExtras({
  index,
  chapter,
}: {
  readonly index: number;
  readonly chapter: Chapter;
}) {
  if (index === 4) {
    // Beat 5: the paired Catalog/Billing schema cards, side by side.
    return (
      <div className="flex flex-col gap-4 sm:flex-row sm:gap-6">
        {chapter.boxes.map((box, j) => (
          <div key={j} className="flex-1">
            <ProtoCodeBox
              label={box.label}
              color={box.color}
              lines={box.lines}
            />
          </div>
        ))}
      </div>
    );
  }

  if (index === 5) {
    // Beat 6: the composite schema, then the five still-separate services.
    return (
      <>
        {chapter.boxes.map((box, j) => (
          <ProtoCodeBox
            key={j}
            label={box.label}
            color={box.color}
            lines={box.lines}
          />
        ))}
        <ServicesUntouched />
      </>
    );
  }

  if (index === 6) {
    return <RuntimeBox />;
  }

  return (
    <>
      {chapter.boxes.map((box, j) => (
        <ProtoCodeBox
          key={j}
          label={box.label}
          color={box.color}
          lines={box.lines}
        />
      ))}
    </>
  );
}

/**
 * Prototype v11 - "Folio Numerals": each beat is a chapter plate fronted by
 * an oversized thin-outline numeral (01-07) whose folio caption repeats one
 * template - "MERGED BY: ___" - so the argument's spine (who does the
 * merging?) is printed seven times, resolving to "composition · schemas
 * only" and "the executor · responses only". Numerals alternate left/right
 * down the section; between plates there is only the page background.
 */
export function FolioNumerals() {
  return (
    <PageSection maxWidth="6xl">
      {CHAPTERS.map((chapter, i) => (
        <Fragment key={i}>
          <Plate index={i} title={chapter.title}>
            {chapter.body}
            <BeatExtras index={i} chapter={chapter} />
          </Plate>
          {i === 5 && <HorizonRule />}
        </Fragment>
      ))}
      <div className="border-cc-card-border mt-4 border-t pt-6 text-center">
        <MicroLabel>End of chapter seven · one API · five teams</MicroLabel>
      </div>
    </PageSection>
  );
}
