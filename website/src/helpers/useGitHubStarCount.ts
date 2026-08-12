import { useEffect, useState } from "react";

const GITHUB_REPO_API =
  "https://api.github.com/repos/ChilliCream/graphql-platform";

export function useGitHubStarCount(): number | null {
  const [count, setCount] = useState<number | null>(null);

  useEffect(() => {
    const controller = new AbortController();

    void resolveStarCount(controller.signal).then((resolved) => {
      if (resolved !== null && !controller.signal.aborted) {
        setCount(resolved);
      }
    });

    return () => controller.abort();
  }, []);

  return count;
}

// The site is statically exported, so the build-time count goes stale between
// deploys. The browser refreshes it from the GitHub API and caches the result
// per tab to stay well inside the unauthenticated rate limit.
const CACHE_KEY = "chillicream:github-star-count";
const CACHE_MAX_AGE_MS = 600_000; // 10 min

interface CachedStarCount {
  readonly count: number;
  readonly fetchedAt: number;
}

function readCachedCount(): number | null {
  try {
    const raw = window.sessionStorage.getItem(CACHE_KEY);

    if (raw === null) {
      return null;
    }

    const cached = JSON.parse(raw) as Partial<CachedStarCount>;

    return typeof cached.count === "number" &&
      typeof cached.fetchedAt === "number" &&
      Date.now() - cached.fetchedAt < CACHE_MAX_AGE_MS
      ? cached.count
      : null;
  } catch {
    return null;
  }
}

function writeCachedCount(count: number): void {
  try {
    const cached: CachedStarCount = { count, fetchedAt: Date.now() };

    window.sessionStorage.setItem(CACHE_KEY, JSON.stringify(cached));
  } catch {
    // Session storage can be unavailable; the count is simply fetched again on
    // the next page load.
  }
}

async function fetchStarCount(signal: AbortSignal): Promise<number | null> {
  try {
    const response = await fetch(GITHUB_REPO_API, {
      headers: { Accept: "application/vnd.github+json" },
      signal,
    });

    if (!response.ok) {
      return null;
    }

    const data = (await response.json()) as { stargazers_count?: number };

    return typeof data.stargazers_count === "number"
      ? data.stargazers_count
      : null;
  } catch {
    return null;
  }
}

async function resolveStarCount(signal: AbortSignal): Promise<number | null> {
  const cached = readCachedCount();

  if (cached !== null) {
    return cached;
  }

  const liveCount = await fetchStarCount(signal);

  if (liveCount !== null) {
    writeCachedCount(liveCount);
  }

  return liveCount;
}
