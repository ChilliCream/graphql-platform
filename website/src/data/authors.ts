import { schemaId, type JsonLdNode } from "@/src/helpers/structuredData";
import { toAbsoluteUrl } from "@/src/helpers/siteUrl";

export interface AuthorProfile {
  readonly slug: string;
  readonly name: string;
  readonly bio: string;
  readonly externalUrl: string;
  readonly imageUrl: string;
  readonly profileLinks: readonly AuthorProfileLink[];
}

export interface AuthorProfileLink {
  readonly provider: "github" | "linkedin" | "x";
  readonly url: string;
}

export const AUTHORS_PATH = "/authors";

/**
 * The authors currently named in blog frontmatter. Keep this list limited to
 * identities that already have a matching public profile URL in the content.
 */
export const AUTHOR_PROFILES = [
  {
    slug: "glen",
    name: "Glen",
    bio: "Glen works at ChilliCream and writes about Hot Chocolate, Fusion, MCP, and agent-ready GraphQL APIs.",
    externalUrl: "https://github.com/glen-84",
    imageUrl: "https://avatars.githubusercontent.com/u/261509?v=4",
    profileLinks: [{ provider: "github", url: "https://github.com/glen-84" }],
  },
  {
    slug: "michael-staib",
    name: "Michael Staib",
    bio: "Michael is the author of Hot Chocolate and works on GraphQL server performance, Fusion, and distributed GraphQL standards.",
    externalUrl: "https://github.com/michaelstaib",
    imageUrl: "https://avatars1.githubusercontent.com/u/9714350?s=100&v=4",
    profileLinks: [
      { provider: "github", url: "https://github.com/michaelstaib" },
      {
        provider: "linkedin",
        url: "https://www.linkedin.com/in/michael-staib-31519571",
      },
      { provider: "x", url: "https://x.com/michael_staib" },
    ],
  },
  {
    slug: "pascal-senn",
    name: "Pascal Senn",
    bio: "Pascal works on ChilliCream's GraphQL platform, focusing on data APIs, OpenTelemetry, semantic introspection, and developer tooling.",
    externalUrl: "https://github.com/pascalsenn",
    imageUrl: "https://avatars.githubusercontent.com/u/14233220?v=4",
    profileLinks: [
      { provider: "github", url: "https://github.com/pascalsenn" },
      {
        provider: "linkedin",
        url: "https://www.linkedin.com/in/pascal-senn-90899a15a",
      },
      { provider: "x", url: "https://x.com/Pascal_Senn" },
    ],
  },
  {
    slug: "rafael-staib",
    name: "Rafael Staib",
    bio: "Rafael is a software architect and engineer at ChilliCream who writes about Nitro, Banana Cake Pop, and GraphQL developer tooling.",
    externalUrl: "https://github.com/rstaib",
    imageUrl: "https://avatars0.githubusercontent.com/u/4325318?s=100&v=4",
    profileLinks: [
      { provider: "github", url: "https://github.com/rstaib" },
      {
        provider: "linkedin",
        url: "https://www.linkedin.com/in/rafaelstaib",
      },
    ],
  },
  {
    slug: "salome-ruckstuhl",
    name: "Salome Ruckstuhl",
    bio: "Salome writes about the GraphQL community, federation, AI agents, and the standards shaping the ecosystem.",
    externalUrl: "https://github.com/sal-ome",
    imageUrl: "https://avatars.githubusercontent.com/u/67280421?v=4",
    profileLinks: [
      { provider: "github", url: "https://github.com/sal-ome" },
      {
        provider: "linkedin",
        url: "https://www.linkedin.com/in/salome-ruckstuhl",
      },
    ],
  },
  {
    slug: "tobias-tengler",
    name: "Tobias Tengler",
    bio: "Tobias works at ChilliCream on its GraphQL platform and documentation, with a focus on federation, schema design, REST, and OpenAPI.",
    externalUrl: "https://github.com/tobias-tengler",
    imageUrl: "https://avatars.githubusercontent.com/u/45513122?v=4",
    profileLinks: [
      { provider: "github", url: "https://github.com/tobias-tengler" },
      {
        provider: "linkedin",
        url: "https://www.linkedin.com/in/tobiastengler",
      },
      { provider: "x", url: "https://x.com/tobiastengler" },
    ],
  },
] as const satisfies readonly AuthorProfile[];

export function authorPageUrl(author: AuthorProfile): string {
  return `${AUTHORS_PATH}/${author.slug}`;
}

export function authorPersonId(author: AuthorProfile): string {
  return schemaId(authorPageUrl(author), "person");
}

export function findAuthorProfile(
  name: string | null,
  externalUrl: string | null,
): AuthorProfile | null {
  if (!name || !externalUrl) {
    return null;
  }

  return (
    AUTHOR_PROFILES.find(
      (author) => author.name === name && author.externalUrl === externalUrl,
    ) ?? null
  );
}

export function createAuthorPersonNode(author: AuthorProfile): JsonLdNode {
  return {
    "@type": "Person",
    "@id": authorPersonId(author),
    name: author.name,
    description: author.bio,
    url: toAbsoluteUrl(authorPageUrl(author)),
    image: toAbsoluteUrl(author.imageUrl),
    sameAs: author.profileLinks.map(({ url }) => url),
  };
}
