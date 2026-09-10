"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useRef, useState } from "react";

import { PROTOTYPE_VERSIONS } from "./versions";

const CURRENT_PAGE_HREF = "/products/fusion";

function hrefFor(n: number): string {
  return `${CURRENT_PAGE_HREF}/v${n}`;
}

function labelFor(n: number): string {
  const version = PROTOTYPE_VERSIONS.find((v) => v.n === n);
  return version ? `v${version.n} · ${version.name}` : `v${n}`;
}

interface VersionSwitcherProps {
  readonly current: number;
}

/**
 * Floating pill bar, fixed to the bottom of the viewport on every Fusion
 * prototype page: prev/next arrows plus a popover that lists the production
 * page and all ten concept versions. Hidden from print.
 *
 * Concept tasks do not render this directly - `PrototypeShell` does.
 */
export function VersionSwitcher({ current }: VersionSwitcherProps) {
  const [open, setOpen] = useState(false);
  const pathname = usePathname();
  const containerRef = useRef<HTMLDivElement>(null);

  const prevHref = current <= 1 ? CURRENT_PAGE_HREF : hrefFor(current - 1);
  const nextHref =
    current < PROTOTYPE_VERSIONS.length ? hrefFor(current + 1) : null;

  return (
    <div
      ref={containerRef}
      className="fixed bottom-5 left-1/2 z-30 -translate-x-1/2 print:hidden"
      onBlur={(event) => {
        if (!containerRef.current?.contains(event.relatedTarget as Node)) {
          setOpen(false);
        }
      }}
      onKeyDown={(event) => {
        if (event.key === "Escape") {
          setOpen(false);
        }
      }}
    >
      {open && (
        <div
          role="menu"
          className="border-cc-card-border bg-cc-surface/95 absolute bottom-full left-1/2 mb-2 max-h-[min(60vh,28rem)] w-[min(90vw,22rem)] -translate-x-1/2 overflow-y-auto rounded-xl border p-2 shadow-xl backdrop-blur-md"
        >
          <Link
            href={CURRENT_PAGE_HREF}
            prefetch={false}
            role="menuitem"
            onClick={() => setOpen(false)}
            className={`text-cc-ink hover:bg-cc-white/5 focus-visible:ring-cc-heading flex items-center justify-between rounded-lg px-3 py-2 text-sm no-underline focus-visible:ring-2 focus-visible:outline-none ${
              pathname === CURRENT_PAGE_HREF
                ? "text-cc-heading font-semibold"
                : ""
            }`}
          >
            Current page
          </Link>
          {PROTOTYPE_VERSIONS.map((version) => {
            const href = hrefFor(version.n);
            const active = pathname === href;
            return (
              <Link
                key={version.n}
                href={href}
                prefetch={false}
                role="menuitem"
                onClick={() => setOpen(false)}
                className={`text-cc-ink hover:bg-cc-white/5 focus-visible:ring-cc-heading flex items-center justify-between gap-3 rounded-lg px-3 py-2 text-sm no-underline focus-visible:ring-2 focus-visible:outline-none ${
                  active ? "text-cc-heading font-semibold" : ""
                }`}
              >
                <span>
                  v{version.n} · {version.name}
                </span>
              </Link>
            );
          })}
        </div>
      )}

      <div className="border-cc-card-border bg-cc-surface/95 flex items-center gap-1 rounded-full border p-1 backdrop-blur-md">
        <Link
          href={prevHref}
          prefetch={false}
          aria-label="Previous version"
          className="text-cc-ink hover:bg-cc-white/5 focus-visible:ring-cc-heading flex h-9 w-9 items-center justify-center rounded-full text-lg no-underline focus-visible:ring-2 focus-visible:outline-none"
        >
          ‹
        </Link>
        <button
          type="button"
          aria-haspopup="menu"
          aria-expanded={open}
          onClick={() => setOpen((v) => !v)}
          className="text-cc-heading hover:bg-cc-white/5 focus-visible:ring-cc-heading rounded-full px-3 py-1.5 font-mono text-xs whitespace-nowrap focus-visible:ring-2 focus-visible:outline-none"
        >
          {labelFor(current)}
        </button>
        {nextHref ? (
          <Link
            href={nextHref}
            prefetch={false}
            aria-label="Next version"
            className="text-cc-ink hover:bg-cc-white/5 focus-visible:ring-cc-heading flex h-9 w-9 items-center justify-center rounded-full text-lg no-underline focus-visible:ring-2 focus-visible:outline-none"
          >
            ›
          </Link>
        ) : (
          <span
            aria-hidden="true"
            className="text-cc-nav-label flex h-9 w-9 items-center justify-center text-lg opacity-30"
          >
            ›
          </span>
        )}
      </div>
    </div>
  );
}
