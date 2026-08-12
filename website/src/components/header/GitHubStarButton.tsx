"use client";

import { GitHubIcon } from "@/src/icons/GitHub";

import { useGitHubStarCount } from "@/src/helpers/useGitHubStarCount";
import { GITHUB_REPO_URL, GITHUB_STARGAZERS_URL } from "./navData";

export function GitHubStarButton() {
  const count = useGitHubStarCount();

  return (
    <span className="border-cc-card-border bg-cc-hover text-cc-heading inline-flex items-stretch overflow-hidden rounded-md border text-xs font-medium">
      <a
        href={GITHUB_REPO_URL}
        target="_blank"
        rel="noopener noreferrer"
        className="hover:bg-cc-ink-faint inline-flex items-center gap-1.5 px-2 py-1 no-underline transition-colors"
        aria-label="Star ChilliCream on GitHub"
      >
        <GitHubIcon className="text-cc-heading h-3.5 w-3.5 fill-current" />
        Star
      </a>
      {!!count && (
        <a
          href={GITHUB_STARGAZERS_URL}
          target="_blank"
          rel="noopener noreferrer"
          className="border-cc-card-border text-cc-heading hover:bg-cc-ink-faint inline-flex items-center border-l px-2 py-1 tabular-nums no-underline transition-colors"
          aria-label={`${count.toLocaleString("en-US")} stargazers on GitHub`}
        >
          {count.toLocaleString("en-US")}
        </a>
      )}
    </span>
  );
}
