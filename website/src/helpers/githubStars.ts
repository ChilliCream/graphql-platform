const GITHUB_REPO_API =
  "https://api.github.com/repos/ChilliCream/graphql-platform";

/**
 * Fetches the GitHub stargazer count for the ChilliCream/graphql-platform
 * repository. The result is cached and revalidated once per hour. Returns
 * `null` when the request fails so callers can render a fallback.
 */
export async function getGitHubStarCount(): Promise<number | null> {
  try {
    const response = await fetch(GITHUB_REPO_API, {
      headers: { Accept: "application/vnd.github+json" },
      next: { revalidate: 3600 },
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
