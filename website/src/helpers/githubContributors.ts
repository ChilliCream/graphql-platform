const GITHUB_CONTRIBUTORS_API = "https://api.github.com/repos/ChilliCream/graphql-platform/contributors?per_page=40";

export interface GitHubContributor {
  readonly login: string;
  readonly avatarUrl: string;
}

/**
 * Fetches the top contributors for the ChilliCream/graphql-platform
 * repository. The site is statically exported, so this data is fetched once
 * at build time. Returns `null` when the request fails so callers can render
 * a fallback.
 */
export async function getGitHubContributors(): Promise<ReadonlyArray<GitHubContributor> | null> {
  try {
    const response = await fetch(GITHUB_CONTRIBUTORS_API, {
      headers: {
        Accept: "application/vnd.github+json",
        ...(process.env.GITHUB_TOKEN ? { Authorization: `Bearer ${process.env.GITHUB_TOKEN}` } : {}),
      },
      signal: AbortSignal.timeout(10_000),
    });

    if (response.status !== 200) {
      console.warn(`getGitHubContributors: request failed with status ${response.status}`);
      return null;
    }

    const data = (await response.json()) as ReadonlyArray<{
      login?: string;
      avatar_url?: string;
      type?: string;
    }>;

    if (!Array.isArray(data)) {
      return null;
    }

    return data
      .flatMap((entry) =>
        typeof entry.login === "string" &&
        typeof entry.avatar_url === "string" &&
        entry.type !== "Bot" &&
        !entry.login.includes("[bot]")
          ? [{ login: entry.login, avatarUrl: entry.avatar_url }]
          : [],
      )
      .slice(0, 24);
  } catch (error) {
    console.warn("getGitHubContributors: request failed", error);
    return null;
  }
}
