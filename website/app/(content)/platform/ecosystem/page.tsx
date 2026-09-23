import { FromOurBlog } from "@/src/components/FromOurBlog";
import { NextStepsSection } from "@/src/components/NextStepsSection";
import { RevealOnScroll } from "@/src/components/RevealOnScroll";
import { TOOLS } from "@/src/components/header/navData";
import { getGitHubCommitActivity } from "@/src/helpers/githubCommitActivity";
import { getGitHubContributors } from "@/src/helpers/githubContributors";
import { getGitHubStarCount } from "@/src/helpers/githubStars";
import { pageMetadata } from "@/src/helpers/pageMetadata";

import { CommunityGrid } from "./CommunityGrid";
import { Hero } from "./Hero";
import { ProofBand } from "./ProofBand";
import { StandardsBand } from "./StandardsBand";

export const metadata = pageMetadata({
  title: ".NET GraphQL Ecosystem",
  description:
    "Explore ChilliCream's open-source .NET GraphQL platform, public development, standards participation, and community channels.",
  path: "/platform/ecosystem",
  keywords: [
    "GraphQL ecosystem",
    "open source .NET GraphQL",
    "HotChocolate GraphQL server",
    "GraphQL standards",
    "GraphQL Foundation",
    "GitHub contributors",
    "public roadmap",
    "community channels",
    "Fusion GraphQL",
    "Strawberry Shake",
  ],
});

const HIDDEN_CONTRIBUTOR_LOGINS = new Set(["artola"]);

export default async function EcosystemPage() {
  const [starCount, contributors, commitActivity] = await Promise.all([
    getGitHubStarCount(),
    getGitHubContributors(),
    getGitHubCommitActivity(),
  ]);
  const heroContributors =
    contributors?.filter(
      (contributor) => !HIDDEN_CONTRIBUTOR_LOGINS.has(contributor.login),
    ) ?? null;

  return (
    <>
      <Hero starCount={starCount} contributors={heroContributors} />
      <ProofBand commitActivity={commitActivity} />
      <StandardsBand />
      <CommunityGrid starCount={starCount} />
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
