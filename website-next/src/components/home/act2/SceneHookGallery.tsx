import Link from "next/link";
import type { ComponentType } from "react";

import { SCENES, type VariantConcept } from "@/src/components/home/act2/scenes";
import { getVersion } from "@/src/components/home/act2/versions";

/** An illustration component: renders one hook's visual, optionally positioned by the caller. */
export type VariantComponent = ComponentType<{ readonly className?: string }>;

/**
 * A version's illustrations, keyed by scene key then variant number, e.g.
 * `components.build[2]`. A missing entry renders a placeholder cell instead of
 * failing the build, so a partially generated version still shows.
 */
export type SceneComponents = Record<
  string,
  Record<number, VariantComponent | undefined>
>;

/** The hook copy for one cell: a punchy headline and a one-line blurb. */
export interface HookCopy {
  readonly headline: string;
  readonly blurb: string;
}

/** A version's hook copy, keyed by scene key then variant number. */
export type SceneCopy = Record<string, Record<number, HookCopy | undefined>>;

interface SceneHookGalleryProps {
  /** The version number (1-9); drives the header label and title lookup. */
  readonly version: number;
  /** This version's 25 illustration components. */
  readonly components: SceneComponents;
  /** This version's 25 hook copies. */
  readonly copy: SceneCopy;
}

/**
 * Renders one scene-illustration version as five scenes of content hooks. Each
 * hook is a clickable cell that pairs the version's illustration with its own
 * headline and blurb and links to that scene's content page, so the gallery
 * reads the way the hooks would on the real platform page rather than as bare
 * illustrations. Scene labels, headlines, and link targets come from the shared
 * `SCENES` metadata; only the illustrations and copy change between versions.
 */
export function SceneHookGallery({
  version,
  components,
  copy,
}: SceneHookGalleryProps) {
  const spec = getVersion(version);

  return (
    <div className="space-y-20">
      <header className="space-y-3">
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
          Internal gallery · v{spec.v}
        </p>
        <h1 className="font-heading text-h2 text-cc-heading">{spec.name}</h1>
        <p className="text-cc-ink max-w-2xl text-base/relaxed">
          {spec.summary}
        </p>
        <p className="text-cc-ink-dim max-w-2xl text-sm/relaxed">
          Five scenes, five hooks each. Every hook pairs this version&apos;s
          illustration with its own headline and blurb and jumps to the
          scene&apos;s content page. The version is one coherent take; the five
          hooks per scene are different angles on the same destination.
        </p>
      </header>

      {SCENES.map((scene) => (
        <section key={scene.key} className="space-y-6">
          <div className="space-y-2">
            <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
              {scene.label}
            </p>
            <h2 className="font-heading text-h4 text-cc-heading">
              {scene.headline}
            </h2>
            <p className="text-cc-ink-dim font-mono text-[0.7rem]">
              hooks → {scene.learnMore}
            </p>
          </div>

          <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-3">
            {scene.variants.map((variant) => (
              <HookCard
                key={variant.n}
                cellId={`cell-${version}-${scene.key}-${variant.n}`}
                label={scene.label}
                href={scene.learnMore}
                concept={variant}
                copy={copy[scene.key]?.[variant.n]}
                Component={components[scene.key]?.[variant.n]}
              />
            ))}
          </div>
        </section>
      ))}
    </div>
  );
}

interface HookCardProps {
  readonly cellId: string;
  readonly label: string;
  readonly href: string;
  readonly concept: VariantConcept;
  readonly copy: HookCopy | undefined;
  readonly Component: VariantComponent | undefined;
}

/** One clickable content hook: illustration, eyebrow, headline, blurb, and a learn-more link. */
function HookCard({
  cellId,
  label,
  href,
  concept,
  copy,
  Component,
}: HookCardProps) {
  const headline = copy?.headline ?? concept.name;
  const blurb = copy?.blurb ?? concept.depicts;

  return (
    <Link
      id={cellId}
      href={href}
      className="group border-cc-card-border bg-cc-card-bg/40 hover:border-cc-card-border-hover flex flex-col gap-5 rounded-2xl border p-5 no-underline transition-colors"
    >
      <div className="flex min-h-44 flex-1 items-center justify-center">
        {Component === undefined ? (
          <div className="border-cc-ink-faint text-cc-ink-dim text-caption w-full rounded-xl border border-dashed px-4 py-10 text-center">
            illustration missing
          </div>
        ) : (
          <Component />
        )}
      </div>

      <div className="space-y-2">
        <p className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.12em] uppercase">
          {label}
        </p>
        <h3 className="font-heading text-h6 text-cc-heading group-hover:text-cc-accent text-balance transition-colors">
          {headline}
        </h3>
        <p className="text-cc-ink text-sm/relaxed text-pretty">{blurb}</p>
        <span className="text-cc-accent inline-flex items-center gap-1 text-[0.8rem] font-medium">
          Learn more
          <span aria-hidden="true">→</span>
        </span>
      </div>
    </Link>
  );
}
