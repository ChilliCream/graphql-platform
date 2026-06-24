import type { Metadata } from "next";
import Link from "next/link";
import type { ReactNode } from "react";

import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "Page Variations",
  description:
    "Internal chooser to compare the design takes generated for each platform scene and pick one per page.",
  robots: { index: false, follow: false },
};

/* -------------------------------------------------------------------------- */
/*  Manifest                                                                   */
/*  Every take route is verified to exist on disk under                        */
/*  app/(content)/platform/preview/<slug>/v<n>/page.tsx. The `present` flag    */
/*  records that check; a missing take renders a placeholder instead of a link.*/
/* -------------------------------------------------------------------------- */

interface Take {
  readonly v: number;
  readonly name: string;
  readonly route: string;
  readonly present: boolean;
}

interface Scene {
  readonly scene: string;
  readonly label: string;
  readonly headline: string;
  readonly takes: readonly Take[];
}

const SCENES: readonly Scene[] = [
  {
    scene: "build",
    label: "Build loop",
    headline: "Ship from the code that runs it.",
    takes: [
      {
        v: 1,
        name: "Signature Console",
        route: "/platform/preview/build/v1",
        present: true,
      },
      {
        v: 2,
        name: "Editorial Blueprint",
        route: "/platform/preview/build/v2",
        present: true,
      },
      {
        v: 3,
        name: "Expressive Bento",
        route: "/platform/preview/build/v3",
        present: true,
      },
      {
        v: 4,
        name: "The Ledger (side-by-side, cyan)",
        route: "/platform/preview/build/v4",
        present: true,
      },
      {
        v: 5,
        name: "The Annotated Source (code-walkthrough, cc-accent)",
        route: "/platform/preview/build/v5",
        present: true,
      },
      {
        v: 6,
        name: "House Roast (barista, cc-accent)",
        route: "/platform/preview/build/v6",
        present: true,
      },
      {
        v: 7,
        name: "Codegen Forge (motion-showcase, cc-accent)",
        route: "/platform/preview/build/v7",
        present: true,
      },
      {
        v: 8,
        name: "Constellation of Truth (constellation, cc-accent)",
        route: "/platform/preview/build/v8",
        present: true,
      },
      {
        v: 9,
        name: "Contour Survey (topographic, cc-accent)",
        route: "/platform/preview/build/v9",
        present: true,
      },
    ],
  },
  {
    scene: "feedback",
    label: "Agentic coding",
    headline: "Give coding agents a feedback loop.",
    takes: [
      {
        v: 1,
        name: "Signature Console",
        route: "/platform/preview/agentic-coding/v1",
        present: true,
      },
      {
        v: 2,
        name: "Editorial Blueprint",
        route: "/platform/preview/agentic-coding/v2",
        present: true,
      },
      {
        v: 3,
        name: "Expressive Bento",
        route: "/platform/preview/agentic-coding/v3",
        present: true,
      },
      {
        v: 4,
        name: "The Operator's Manifest (dense-catalog, violet)",
        route: "/platform/preview/agentic-coding/v4",
        present: true,
      },
      {
        v: 5,
        name: "Ledger of Two Columns (side-by-side, violet)",
        route: "/platform/preview/agentic-coding/v5",
        present: true,
      },
      {
        v: 6,
        name: "House Blend for Agents (barista, violet)",
        route: "/platform/preview/agentic-coding/v6",
        present: true,
      },
      {
        v: 7,
        name: "Tool Lifecycle Conveyor (motion-showcase, violet)",
        route: "/platform/preview/agentic-coding/v7",
        present: true,
      },
      {
        v: 8,
        name: "Beamlines: Tool Calls in Flight (beam-streaks, violet)",
        route: "/platform/preview/agentic-coding/v8",
        present: true,
      },
      {
        v: 9,
        name: "Confetti Registry: every dot is a published operation (confetti-scatter, cc-accent)",
        route: "/platform/preview/agentic-coding/v9",
        present: true,
      },
    ],
  },
  {
    scene: "observe",
    label: "Production view",
    headline: "See what the API is doing.",
    takes: [
      {
        v: 1,
        name: "Signature Console",
        route: "/platform/preview/observability/v1",
        present: true,
      },
      {
        v: 2,
        name: "Editorial Blueprint",
        route: "/platform/preview/observability/v2",
        present: true,
      },
      {
        v: 3,
        name: "Expressive Bento",
        route: "/platform/preview/observability/v3",
        present: true,
      },
      {
        v: 4,
        name: "The Reference Manual (sidebar-toc, cyan)",
        route: "/platform/preview/observability/v4",
        present: true,
      },
      {
        v: 5,
        name: "Telemetry Reference Sheet (dense-catalog, cc-accent)",
        route: "/platform/preview/observability/v5",
        present: true,
      },
      {
        v: 6,
        name: "Nitro Cold Brew Bar (barista, cc-accent)",
        route: "/platform/preview/observability/v6",
        present: true,
      },
      {
        v: 7,
        name: "Trace Waterfall in Motion (motion-showcase, cc-accent)",
        route: "/platform/preview/observability/v7",
        present: true,
      },
      {
        v: 8,
        name: "tail -f production (scanlines, cc-accent)",
        route: "/platform/preview/observability/v8",
        present: true,
      },
      {
        v: 9,
        name: "Constellation of Signals (bokeh-orbs, cc-accent)",
        route: "/platform/preview/observability/v9",
        present: true,
      },
    ],
  },
  {
    scene: "workflows",
    label: "Workflow",
    headline: "Let work continue after the request.",
    takes: [
      {
        v: 1,
        name: "Signature Console",
        route: "/platform/preview/workflows/v1",
        present: true,
      },
      {
        v: 2,
        name: "Editorial Blueprint",
        route: "/platform/preview/workflows/v2",
        present: true,
      },
      {
        v: 3,
        name: "Expressive Bento",
        route: "/platform/preview/workflows/v3",
        present: true,
      },
      {
        v: 4,
        name: "Longform Dispatch (centered-narrative, coral)",
        route: "/platform/preview/workflows/v4",
        present: true,
      },
      {
        v: 5,
        name: "Field Manual (sidebar-toc, coral)",
        route: "/platform/preview/workflows/v5",
        present: true,
      },
      {
        v: 6,
        name: "The Order Counter (barista, coral)",
        route: "/platform/preview/workflows/v6",
        present: true,
      },
      {
        v: 7,
        name: "Live Wire (motion-showcase, coral)",
        route: "/platform/preview/workflows/v7",
        present: true,
      },
      {
        v: 8,
        name: "Message on the Rail (timeline-rail, coral)",
        route: "/platform/preview/workflows/v8",
        present: true,
      },
      {
        v: 9,
        name: "The Workflow Quarterly (asym-magazine, coral)",
        route: "/platform/preview/workflows/v9",
        present: true,
      },
    ],
  },
  {
    scene: "guardrails",
    label: "Release safety",
    headline: "Change contracts with a safety net.",
    takes: [
      {
        v: 1,
        name: "Signature Console",
        route: "/platform/preview/release-safety/v1",
        present: true,
      },
      {
        v: 2,
        name: "Editorial Blueprint",
        route: "/platform/preview/release-safety/v2",
        present: true,
      },
      {
        v: 3,
        name: "Expressive Bento",
        route: "/platform/preview/release-safety/v3",
        present: true,
      },
      {
        v: 4,
        name: "Guardrail Constellation (visual-hero, coral)",
        route: "/platform/preview/release-safety/v4",
        present: true,
      },
      {
        v: 5,
        name: "The Confession (centered-narrative, coral)",
        route: "/platform/preview/release-safety/v5",
        present: true,
      },
      {
        v: 6,
        name: "Quality Control on the Bar (barista, cc-accent)",
        route: "/platform/preview/release-safety/v6",
        present: true,
      },
      {
        v: 7,
        name: "Diff Reel (motion-showcase, coral)",
        route: "/platform/preview/release-safety/v7",
        present: true,
      },
      {
        v: 8,
        name: "The Validation Ticket (punchcard, cc-accent)",
        route: "/platform/preview/release-safety/v8",
        present: true,
      },
      {
        v: 9,
        name: "Deck of Diffs (stacked-deck, cc-accent)",
        route: "/platform/preview/release-safety/v9",
        present: true,
      },
    ],
  },
];

/* -------------------------------------------------------------------------- */
/*  Brand spectrum: the single gradient event on this screen                   */
/* -------------------------------------------------------------------------- */

const SPECTRUM = "linear-gradient(100deg, #16b9e4, #7c92c6, #f0786a)";

/* -------------------------------------------------------------------------- */
/*  Small chrome                                                               */
/* -------------------------------------------------------------------------- */

interface EyebrowProps {
  readonly children: ReactNode;
}

function Eyebrow({ children }: EyebrowProps) {
  return (
    <p className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.22em] uppercase">
      {children}
    </p>
  );
}

interface TakeBadgeProps {
  readonly v: number;
}

/** The take number, rendered as a mono coin so the three cards line up. */
function TakeBadge({ v }: TakeBadgeProps) {
  return (
    <span className="border-cc-card-border bg-cc-surface text-cc-heading flex h-8 w-8 shrink-0 items-center justify-center rounded-full border font-mono text-[0.78rem] font-semibold tabular-nums">
      {v}
    </span>
  );
}

/* -------------------------------------------------------------------------- */
/*  Take cards                                                                 */
/* -------------------------------------------------------------------------- */

interface TakeCardProps {
  readonly take: Take;
}

function TakeCard({ take }: TakeCardProps) {
  if (!take.present) {
    return (
      <div className="border-cc-card-border bg-cc-card-bg/60 flex flex-col gap-4 rounded-xl border border-dashed p-5 backdrop-blur-sm">
        <div className="flex items-center gap-3">
          <span className="border-cc-card-border text-cc-nav-label flex h-8 w-8 shrink-0 items-center justify-center rounded-full border border-dashed font-mono text-[0.78rem] tabular-nums">
            {take.v}
          </span>
          <Eyebrow>Take {take.v}</Eyebrow>
        </div>
        <p className="text-cc-heading font-heading text-h6 font-semibold tracking-tight">
          {take.name}
        </p>
        <p className="text-cc-nav-label font-mono text-[0.66rem] tracking-tight">
          not generated
        </p>
      </div>
    );
  }

  return (
    <Link
      href={take.route}
      className="group border-cc-card-border bg-cc-card-bg hover:border-cc-accent flex flex-col gap-4 rounded-xl border p-5 no-underline backdrop-blur-sm transition-colors"
    >
      <div className="flex items-center gap-3">
        <TakeBadge v={take.v} />
        <Eyebrow>Take {take.v}</Eyebrow>
      </div>
      <p className="text-cc-heading group-hover:text-cc-accent font-heading text-h6 font-semibold tracking-tight transition-colors">
        {take.name}
      </p>
      <span className="text-cc-ink-dim mt-auto font-mono text-[0.66rem] tracking-tight">
        {take.route}
      </span>
      <span className="text-cc-accent text-[0.82rem] font-medium">
        Open preview →
      </span>
    </Link>
  );
}

/* -------------------------------------------------------------------------- */
/*  Scene block                                                                */
/* -------------------------------------------------------------------------- */

interface SceneBlockProps {
  readonly scene: Scene;
  readonly index: number;
  readonly total: number;
}

function SceneBlock({ scene, index, total }: SceneBlockProps) {
  return (
    <section>
      <div className="flex flex-wrap items-baseline gap-x-4 gap-y-1">
        <Eyebrow>{scene.label}</Eyebrow>
        <span className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.22em] uppercase">
          {String(index + 1).padStart(2, "0")} /{" "}
          {String(total).padStart(2, "0")}
        </span>
      </div>
      <h2 className="font-heading text-h3 text-cc-heading mt-3 max-w-3xl font-semibold tracking-tight">
        {scene.headline}
      </h2>
      <div className="mt-7 grid gap-5 md:grid-cols-3 xl:grid-cols-9">
        {scene.takes.map((take) => (
          <TakeCard key={take.v} take={take} />
        ))}
      </div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  Page                                                                       */
/* -------------------------------------------------------------------------- */

export default function PreviewChooserPage() {
  return (
    <div className="flex flex-col gap-20 py-6">
      <header>
        <Eyebrow>Internal · design review</Eyebrow>
        <h1 className="font-heading text-h1 text-cc-heading mt-5 font-semibold tracking-tight">
          Page{" "}
          <span
            className="bg-clip-text text-transparent"
            style={{ backgroundImage: SPECTRUM }}
          >
            Variations
          </span>
        </h1>
        <p className="text-cc-prose mt-6 max-w-2xl text-[1.1rem] leading-relaxed">
          Five scenes, nine design takes each. Open every take, compare them
          side by side, and pick one stance per page. Each take is a full,
          self-contained layout built from the same brand shell.
        </p>
        <ul className="text-cc-ink-dim mt-8 flex flex-wrap gap-x-6 gap-y-2 font-mono text-[0.72rem] tracking-tight">
          <li>v1 · Signature Console</li>
          <li>v2 · Editorial Blueprint</li>
          <li>v3 · Expressive Bento</li>
          <li>v4 · per-scene stance</li>
          <li>v5 · per-scene stance</li>
          <li>v6 · per-scene stance</li>
          <li>v7 · per-scene stance</li>
          <li>v8 · per-scene stance</li>
          <li>v9 · per-scene stance</li>
        </ul>
      </header>

      {SCENES.map((scene, i) => (
        <SceneBlock
          key={scene.scene}
          scene={scene}
          index={i}
          total={SCENES.length}
        />
      ))}

      <section className="flex flex-col items-center gap-7 py-6 text-center">
        <h2 className="font-heading text-h4 text-cc-heading max-w-2xl font-semibold tracking-tight">
          Picked a stance? The same CTA closes every take.
        </h2>
        <div className="flex flex-wrap items-center justify-center gap-3">
          <SolidButton href="/get-started">Start for Free</SolidButton>
          <OutlineButton href="/docs">Read the Docs</OutlineButton>
        </div>
      </section>
    </div>
  );
}
