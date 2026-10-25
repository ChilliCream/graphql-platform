"use client";

import { useGitHubStarCount } from "@/src/helpers/useGitHubStarCount";
import { GitHubIcon } from "@/src/icons/GitHub";

import { PILL_CLASSES } from "./pillClasses";

export function StarPillContent() {
  const count = useGitHubStarCount();

  return (
    <>
      <GitHubIcon aria-hidden="true" className="h-3 w-3 fill-current" />
      {count === null ? (
        <span>Stars</span>
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
