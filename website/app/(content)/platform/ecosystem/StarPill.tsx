"use client";

import { useGitHubStarCount } from "@/src/helpers/useGitHubStarCount";
import { GitHubIcon } from "@/src/icons/GitHub";

export const PILL_CLASSES =
  "border-cc-card-border bg-cc-surface text-cc-ink-dim flex h-7 items-center gap-1.5 rounded-full border px-3 pt-[2px] font-mono text-[0.6rem] tracking-[0.14em] whitespace-nowrap uppercase";

export function StarPillContent() {
  const count = useGitHubStarCount();

  return (
    <>
      <GitHubIcon aria-hidden="true" className="h-3 w-3 fill-current" />
      {count === null ? (
        <span>GITHUB</span>
      ) : (
        <span className="text-cc-heading">
          <span className="sr-only">GitHub stars: </span>
          {count.toLocaleString("en-US")}
        </span>
      )}
    </>
  );
}

export function StarPill() {
  return (
    <div className={PILL_CLASSES}>
      <StarPillContent />
    </div>
  );
}
