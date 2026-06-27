import type { ComponentType } from "react";

import { SCENES, type VariantConcept } from "@/src/components/home/act2/scenes";
import { getVersion } from "@/src/components/home/act2/versions";

/** An illustration component: renders one concept, optionally positioned by the caller. */
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

interface SceneGalleryProps {
  /** The version number (1-9); drives the header label and title lookup. */
  readonly version: number;
  /** This version's 25 illustration components. */
  readonly components: SceneComponents;
}

/**
 * Renders the full 5-scenes x 5-concepts gallery for one scene-illustration
 * version. The scene labels, headlines, accent rationale, and per-concept
 * captions come from the shared `SCENES` metadata, so every version is laid out
 * identically and only the illustrations differ.
 */
export function SceneGallery({ version, components }: SceneGalleryProps) {
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
          Every illustration concept for the five homepage scroll-scenes,
          rendered in this version&apos;s visual language. The 25 concepts are
          fixed across versions; only the rendering changes, so versions stay
          comparable side by side.
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
            <p className="text-cc-ink-dim font-mono text-[0.7rem]/relaxed">
              accent: {scene.accent}
            </p>
          </div>

          <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-3">
            {scene.variants.map((variant) => (
              <VariantCard
                key={variant.n}
                concept={variant}
                Component={components[scene.key]?.[variant.n]}
              />
            ))}
          </div>
        </section>
      ))}
    </div>
  );
}

interface VariantCardProps {
  readonly concept: VariantConcept;
  readonly Component: VariantComponent | undefined;
}

/** Bordered cell holding one illustration, captioned with its number, name, and depiction. */
function VariantCard({ concept, Component }: VariantCardProps) {
  const { n, name, depicts } = concept;

  return (
    <div className="border-cc-card-border bg-cc-surface flex flex-col gap-4 rounded-2xl border p-5">
      <div className="flex min-h-48 flex-1 items-center justify-center">
        {Component === undefined ? (
          <div className="border-cc-ink-faint text-cc-ink-dim text-caption w-full rounded-xl border border-dashed px-4 py-10 text-center">
            variant file missing
          </div>
        ) : (
          <Component />
        )}
      </div>

      <div className="border-cc-card-border space-y-1.5 border-t pt-4">
        <p className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.12em] uppercase">
          Variant {n}
        </p>
        <p className="text-cc-heading text-sm font-medium">{name}</p>
        <p className="text-cc-ink-dim text-xs/relaxed">{depicts}</p>
      </div>
    </div>
  );
}
