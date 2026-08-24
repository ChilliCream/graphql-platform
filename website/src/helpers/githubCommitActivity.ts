const GITHUB_COMMIT_ACTIVITY_API = "https://api.github.com/repos/ChilliCream/graphql-platform/stats/commit_activity";

const RETRY_DELAY_MS = 2_000;
const MAX_ATTEMPTS = 3;

/**
 * Fetches weekly commit activity for the ChilliCream/graphql-platform
 * repository. The site is statically exported, so this data is fetched once
 * at build time. Returns `null` when the request fails so callers can render
 * a fallback.
 */
export async function getGitHubCommitActivity(): Promise<ReadonlyArray<ReadonlyArray<number>> | null> {
  try {
    for (let attempt = 1; attempt <= MAX_ATTEMPTS; attempt++) {
      const response = await fetch(GITHUB_COMMIT_ACTIVITY_API, {
        headers: {
          Accept: "application/vnd.github+json",
          ...(process.env.GITHUB_TOKEN ? { Authorization: `Bearer ${process.env.GITHUB_TOKEN}` } : {}),
        },
        signal: AbortSignal.timeout(10_000),
      });

      // GitHub returns 202 while it computes the stats in the background;
      // retry after a short delay before giving up.
      if (response.status === 202 && attempt < MAX_ATTEMPTS) {
        await new Promise((resolve) => setTimeout(resolve, RETRY_DELAY_MS));
        continue;
      }

      if (response.status !== 200) {
        console.warn(`getGitHubCommitActivity: request failed with status ${response.status}`);
        return null;
      }

      const data = (await response.json()) as ReadonlyArray<{
        days?: ReadonlyArray<number>;
      }>;

      if (!Array.isArray(data)) {
        return null;
      }

      const weeks = data.flatMap((week) =>
        Array.isArray(week.days) && week.days.length === 7 && week.days.every((day: unknown) => typeof day === "number")
          ? [week.days]
          : [],
      );

      return weeks.length > 0 && weeks.length === data.length ? weeks : null;
    }

    console.warn("getGitHubCommitActivity: request still pending (202) after retries");
    return null;
  } catch (error) {
    console.warn("getGitHubCommitActivity: request failed", error);
    return null;
  }
}
