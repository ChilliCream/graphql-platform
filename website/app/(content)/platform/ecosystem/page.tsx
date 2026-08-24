import { FromOurBlog } from "@/src/components/FromOurBlog";
import { NextStepsSection } from "@/src/components/NextStepsSection";
import { PageStructuredData } from "@/src/components/PageStructuredData";
import { RevealOnScroll } from "@/src/components/RevealOnScroll";
import { TOOLS } from "@/src/components/header/navData";
import commitActivity from "@/src/data/githubCommitActivity.json";
import { getGitHubContributors } from "@/src/helpers/githubContributors";
import { pageMetadata } from "@/src/helpers/pageMetadata";
import { schemaId, schemaRef } from "@/src/helpers/structuredData";

import { CommunityGrid } from "./CommunityGrid";
import { Hero } from "./Hero";
import { ProofBand } from "./ProofBand";
import { StandardsBand } from "./StandardsBand";

const PAGE = {
  title: "Open-Source GraphQL Ecosystem for .NET",
  description:
    "Explore the open-source .NET GraphQL ecosystem behind Hot Chocolate, Fusion, Mocha and Strawberry Shake: public code, standards work, docs, and community.",
  path: "/platform/ecosystem",
  keywords: [
    "open-source .NET GraphQL ecosystem",
    ".NET GraphQL open source",
    "HotChocolate GraphQL server",
    "GraphQL standards",
    "GraphQL Foundation",
    "GitHub contributors",
    "public roadmap",
    "community channels",
    "Fusion GraphQL",
    "Strawberry Shake",
    "Mocha Messaging",
  ],
} as const;

export const metadata = pageMetadata(PAGE);

const HIDDEN_CONTRIBUTOR_LOGINS = new Set(["artola"]);

export default async function EcosystemPage() {
  const contributors = await getGitHubContributors();
  const heroContributors =
    contributors?.filter(
      (contributor) => !HIDDEN_CONTRIBUTOR_LOGINS.has(contributor.login),
    ) ?? null;

  return (
    <>
      <PageStructuredData
        title={PAGE.title}
        description={PAGE.description}
        path={PAGE.path}
        pageType="CollectionPage"
        breadcrumbs={[
          { name: "Home", path: "/" },
          { name: "Platform", path: "/platform" },
          { name: "Ecosystem" },
        ]}
        about={[
          schemaRef(schemaId("/products/hotchocolate", "software")),
          schemaRef(schemaId("/products/strawberryshake", "software")),
          schemaRef(schemaId("/products/mocha", "software")),
        ]}
      />
      <Hero contributors={heroContributors} />
      <ProofBand commitActivity={commitActivity.weeks} />
      <StandardsBand />
      <CommunityGrid />
      <RevealOnScroll>
        <FromOurBlog limit={3} className="py-10 sm:py-14" />
      </RevealOnScroll>
      <RevealOnScroll>
        <NextStepsSection
          title="See whether it fits your architecture."
          text="Read the docs, run a focused evaluation, and talk to a maintainer about your architecture. Then decide."
          primaryLink="/docs"
          primaryLinkText="Read the docs"
          secondaryLink={TOOLS.slack}
          secondaryLinkText="Talk to a maintainer"
        />
      </RevealOnScroll>
    </>
  );
}
