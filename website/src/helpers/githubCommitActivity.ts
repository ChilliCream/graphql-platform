const GITHUB_COMMIT_ACTIVITY_API =
  "https://api.github.com/repos/ChilliCream/graphql-platform/stats/commit_activity";

/**
 * Fetches the past year of commit activity for the
 * ChilliCream/graphql-platform repository: 52 weeks, each holding seven daily
 * commit counts (Sunday through Saturday). The result is cached and
 * revalidated once per hour. GitHub answers 202 while it computes the
 * statistics; that and any failure return `null` so callers can render a
 * fallback.
 */
export async function getGitHubCommitActivity(): Promise<ReadonlyArray<
  ReadonlyArray<number>
> | null> {
  try {
    const response = await fetch(GITHUB_COMMIT_ACTIVITY_API, {
      headers: { Accept: "application/vnd.github+json" },
      next: { revalidate: 3600 },
    });

    if (response.status !== 200) {
      return null;
    }

    const data = (await response.json()) as ReadonlyArray<{
      days?: ReadonlyArray<number>;
    }>;

    if (!Array.isArray(data)) {
      return null;
    }

    const weeks = data.flatMap((week) =>
      Array.isArray(week.days) &&
      week.days.length === 7 &&
      week.days.every((day: unknown) => typeof day === "number")
        ? [week.days]
        : [],
    );

    return weeks.length > 0 && weeks.length === data.length ? weeks : null;
  } catch {
    return null;
  }
}
