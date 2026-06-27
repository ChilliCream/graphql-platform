import type { Metadata } from "next";
import Link from "next/link";
import type { ReactNode } from "react";

import { VERSIONS } from "@/src/components/home/act2/versions";

export const metadata: Metadata = {
  title: "Scene Illustrations",
  description:
    "Nine versions of the homepage scroll-scene illustrations, each a distinct visual take on the same 25 concepts.",
  robots: { index: false, follow: false },
};

/** The single brand-spectrum gradient event on this screen. */
const SPECTRUM = "linear-gradient(100deg, #16b9e4, #7c92c6, #f0786a)";

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

interface VersionBadgeProps {
  readonly v: number;
}

function VersionBadge({ v }: VersionBadgeProps) {
  return (
    <span className="border-cc-card-border bg-cc-surface text-cc-heading flex h-9 w-9 shrink-0 items-center justify-center rounded-full border font-mono text-[0.82rem] font-semibold tabular-nums">
      v{v}
    </span>
  );
}

export default function SceneIllustrationsHubPage() {
  return (
    <div className="flex flex-col gap-16 py-6">
      <header>
        <Eyebrow>Internal · scene illustrations</Eyebrow>
        <h1 className="font-heading text-h1 text-cc-heading mt-5 font-semibold tracking-tight">
          Scene Illustration{" "}
          <span
            className="bg-clip-text text-transparent"
            style={{ backgroundImage: SPECTRUM }}
          >
            Versions
          </span>
        </h1>
        <p className="text-cc-ink mt-6 max-w-2xl text-[1.1rem] leading-relaxed">
          Nine takes on the five homepage scroll-scenes. Every version renders
          the same 25 illustration concepts in its own visual language, from the
          on-brand baseline at v1 to the most experimental at v9. Open each and
          compare them scene by scene.
        </p>
        <ul className="text-cc-ink-dim mt-8 flex flex-wrap gap-x-6 gap-y-2 font-mono text-[0.72rem] tracking-tight">
          {VERSIONS.map((version) => (
            <li key={version.v}>
              v{version.v} · {version.name}
            </li>
          ))}
        </ul>
      </header>

      <section>
        <div className="grid gap-5 md:grid-cols-3">
          {VERSIONS.map((version) => (
            <Link
              key={version.v}
              href={version.route}
              className="group border-cc-card-border bg-cc-card-bg hover:border-cc-accent flex flex-col gap-5 rounded-xl border p-6 no-underline backdrop-blur-sm transition-colors"
            >
              <div className="flex items-center gap-3">
                <VersionBadge v={version.v} />
                <Eyebrow>Version {version.v}</Eyebrow>
              </div>
              <p className="text-cc-heading group-hover:text-cc-accent font-heading text-h5 font-semibold tracking-tight transition-colors">
                {version.name}
              </p>
              <p className="text-cc-ink text-[0.95rem] leading-relaxed">
                {version.summary}
              </p>
              <span className="text-cc-ink-dim mt-auto font-mono text-[0.66rem] tracking-tight">
                {version.route}
              </span>
              <span className="text-cc-accent text-[0.82rem] font-medium">
                Open version →
              </span>
            </Link>
          ))}
        </div>
      </section>
    </div>
  );
}
