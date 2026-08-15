import type { ReactNode } from "react";

import { TOOLS } from "@/src/components/header/navData";
import { RevealOnScroll } from "@/src/components/RevealOnScroll";
import { SectionHeading } from "@/src/components/SectionHeading";
import { Card } from "@/src/design-system/Card";
import { GITHUB_REPO_URL } from "@/src/helpers/github";
import { BlogIcon } from "@/src/icons/Blog";
import { GitHubIcon } from "@/src/icons/GitHub";
import { SlackIcon } from "@/src/icons/Slack";
import { YouTubeIcon } from "@/src/icons/YouTube";

import { CARD_FOCUS_CLASSES } from "./cardFocus";
import { StarPill } from "./StarPill";

interface CommunityCardSpec {
  readonly title: string;
  readonly body: string;
  readonly href: string;
  readonly icon: ReactNode;
  readonly withStars: boolean;
  readonly action: string | null;
}

const COMMUNITY_CARDS: readonly CommunityCardSpec[] = [
  {
    title: "GitHub",
    body: "Issues, discussions, pull requests, and the source itself.",
    href: GITHUB_REPO_URL,
    icon: <GitHubIcon className="h-5 w-auto fill-current" />,
    withStars: true,
    action: null,
  },
  {
    title: "Slack",
    body: "Ask questions and talk to the team and other users directly.",
    href: TOOLS.slack,
    icon: <SlackIcon className="h-5 w-auto fill-current" />,
    withStars: false,
    action: "Join",
  },
  {
    title: "YouTube",
    body: "Talks, release walkthroughs, and deep dives.",
    href: TOOLS.youtube,
    icon: <YouTubeIcon className="h-5 w-auto fill-current" />,
    withStars: false,
    action: "Watch",
  },
  {
    title: "Blog",
    body: "Release notes, engineering write-ups, and announcements.",
    href: "/blog",
    icon: <BlogIcon className="h-5 w-auto fill-current" />,
    withStars: false,
    action: "Read",
  },
];

interface CommunityCardProps {
  readonly card: CommunityCardSpec;
}

function CommunityCard({ card }: CommunityCardProps) {
  const content = (
    <>
      <div className="border-cc-card-border bg-cc-surface text-cc-ink-dim flex h-10 w-10 items-center justify-center rounded-full border">
        {card.icon}
      </div>
      <h3 className="font-heading text-cc-heading text-h6 mt-4 font-semibold">
        {card.title}
      </h3>
      <p className="text-cc-ink-dim mt-2 pb-6 text-sm">{card.body}</p>
      <div className="mt-auto flex h-7 items-center">
        {card.withStars ? (
          <StarPill />
        ) : (
          <span className="text-cc-ink-dim group-hover:text-cc-heading text-sm font-medium transition-colors">
            {card.action} <span aria-hidden="true">→</span>
          </span>
        )}
      </div>
    </>
  );

  const external = !card.href.startsWith("/");

  return (
    <Card
      as="a"
      href={card.href}
      target={external ? "_blank" : undefined}
      rel={external ? "noopener noreferrer" : undefined}
      variant="tile"
      hoverBorder
      className={`group flex h-full flex-col no-underline ${CARD_FOCUS_CLASSES}`}
    >
      {content}
    </Card>
  );
}

export function CommunityGrid() {
  return (
    <section className="py-14 sm:py-20">
      <RevealOnScroll>
        <SectionHeading
          title="Find the people behind the code."
          description="Follow the work on GitHub, bring questions to Slack, and learn from talks and engineering posts."
        />
        <div className="mt-10 grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-4">
          {COMMUNITY_CARDS.map((card) => (
            <CommunityCard key={card.title} card={card} />
          ))}
        </div>
      </RevealOnScroll>
    </section>
  );
}
