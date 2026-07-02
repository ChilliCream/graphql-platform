"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";

const MANIFEST: readonly string[] = [
  "/landing/preview",
  "/platform/preview-hub",
  "/platform/scene-illustrations",
  "/platform-section",
  "/mocha-section",
  "/agentic-section",
  "/governance-section",
  "/observability-section",
  "/nitro-section",
  "/combined-section",
  "/messaging-graphic",
  "/platform/preview/build",
  "/platform/preview/agentic-coding",
  "/platform/preview/observability",
  "/platform/preview/workflows",
  "/platform/preview/release-safety",
  "/platform/analytics/preview",
  "/platform/continuous-integration/preview",
  "/platform/ecosystem/preview",
  "/products/nitro/preview",
  "/products/hotchocolate/preview",
  "/products/strawberryshake/preview",
  "/products/fusion/preview",
  "/products/mocha/preview",
  "/products/greendonut/preview",
  "/products/cookiecrumble/preview",
  "/pricing/preview",
  "/services/preview",
  "/services/advisory/preview",
  "/services/support/preview",
  "/services/training/preview",
  "/help/preview",
  "/about/preview",
  "/resources/preview",
];

const MAX_VERSIONS = 9;
const VERSION_PATTERN = /^(.*)\/v(\d+)\/?$/;
const PLATFORM_SCENE_PATTERN = /^\/platform\/preview\/[^/]+$/;

/** Tracks that ship fewer than MAX_VERSIONS variations; others default to 9. */
const TRACK_VERSION_COUNTS: Readonly<Record<string, number>> = {
  "/platform/scene-illustrations": 6,
  "/platform-section": 5,
  "/mocha-section": 8,
  "/agentic-section": 3,
  "/governance-section": 3,
  "/observability-section": 3,
  "/nitro-section": 3,
  "/combined-section": 6,
  "/messaging-graphic": 8,
};

/**
 * Tracks that expose an explicit, non-contiguous set of versions (others render
 * a 1..count sequence). Nitro lists only the surviving stances after archiving.
 */
const TRACK_VERSIONS: Readonly<Record<string, readonly number[]>> = {
  "/products/nitro/preview": [
    3, 4, 5, 7, 10, 15, 16, 17, 18, 20, 22, 23, 26, 27, 28,
  ],
};

interface PreviewSwitcherProps {
  readonly className?: string;
}

export function PreviewSwitcher({ className }: PreviewSwitcherProps) {
  const pathname = usePathname();

  if (!pathname) {
    return null;
  }

  const match = pathname.match(VERSION_PATTERN);
  if (!match) {
    return null;
  }

  const base = match[1];
  const activeVersion = Number.parseInt(match[2], 10);

  const isKnownTrack =
    MANIFEST.includes(base) || PLATFORM_SCENE_PATTERN.test(base);

  if (!isKnownTrack) {
    return null;
  }

  const count = TRACK_VERSION_COUNTS[base] ?? MAX_VERSIONS;
  const versions =
    TRACK_VERSIONS[base] ?? Array.from({ length: count }, (_, i) => i + 1);

  return (
    <div
      className={[
        "fixed right-4 bottom-4 z-50",
        "flex items-center gap-2",
        "rounded-full border px-3 py-1.5",
        "border-cc-card-border bg-cc-card-bg backdrop-blur",
        className ?? "",
      ]
        .filter(Boolean)
        .join(" ")}
    >
      <span className="text-cc-ink-dim font-mono text-[10px] tracking-wider uppercase">
        Versions
      </span>
      <div className="flex items-center gap-1">
        {versions.map((v) => {
          const href = `${base}/v${v}`;
          const isActive = v === activeVersion;
          const chipClasses = isActive
            ? "bg-cc-accent/20 text-cc-ink ring-1 ring-cc-accent"
            : "border border-cc-card-border bg-cc-card-bg text-cc-ink hover:border-cc-card-border-hover";
          return (
            <Link
              key={v}
              href={href}
              aria-current={isActive ? "page" : undefined}
              className={[
                "inline-flex h-7 min-w-7 items-center justify-center",
                "rounded-full px-2 font-mono text-xs",
                "transition-colors",
                chipClasses,
              ].join(" ")}
            >
              v{v}
            </Link>
          );
        })}
      </div>
    </div>
  );
}

export default PreviewSwitcher;
