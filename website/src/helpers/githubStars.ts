const GITHUB_REPO_API =
  "https://api.github.com/repos/ChilliCream/graphql-platform";

/**
 * Fetches the GitHub stargazer count for the ChilliCream/graphql-platform
 * repository. The site is statically exported, so this data is fetched once
 * at build time. Returns `null` when the request fails so callers can render
 * a fallback.
 */
export async function getGitHubStarCount(): Promise<number | null> {
  try {
    const response = await fetch(GITHUB_REPO_API, {
      headers: {
        Accept: "application/vnd.github+json",
        ...(process.env.GITHUB_TOKEN
          ? { Authorization: `Bearer ${process.env.GITHUB_TOKEN}` }
          : {}),
      },
      signal: AbortSignal.timeout(10_000),
    });

    if (!response.ok) {
      console.warn(
        `getGitHubStarCount: request failed with status ${response.status}`,
      );
      return null;
    }

    const data = (await response.json()) as { stargazers_count?: number };

    return typeof data.stargazers_count === "number"
      ? data.stargazers_count
      : null;
  } catch (error) {
    console.warn("getGitHubStarCount: request failed", error);
    return null;
  }
}
