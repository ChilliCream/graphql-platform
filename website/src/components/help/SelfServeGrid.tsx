import NextLink from "next/link";
import type { ReactElement } from "react";

import { CardGrid } from "@/src/components/CardGrid";
import { GITHUB, SLACK, YOUTUBE } from "@/src/components/help/helpLinks";
import { IconFeatureCard } from "@/src/components/IconFeatureCard";
import { SectionHeading } from "@/src/components/SectionHeading";
import { ArticleIcon } from "@/src/icons/Article";
import { BranchIcon } from "@/src/icons/BranchIcon";
import { ChatIcon } from "@/src/icons/ChatIcon";
import { DocsIcon } from "@/src/icons/Docs";
import { PlayIcon } from "@/src/icons/Play";

interface Channel {
  readonly title: string;
  readonly copy: string;
  readonly href: string;
  readonly external: boolean;
  readonly icon: ReactElement;
}

const CHANNELS: readonly Channel[] = [
  {
    title: "Docs",
    copy: "Guides, recipes, and the full Hot Chocolate and Fusion reference.",
    href: "/docs",
    external: false,
    icon: <DocsIcon />,
  },
  {
    title: "Blog",
    copy: "Release notes, deep dives, and patterns from the team.",
    href: "/blog",
    external: false,
    icon: <ArticleIcon />,
  },
  {
    title: "Slack",
    copy: "Live conversation with maintainers and 7000+ developers.",
    href: SLACK,
    external: true,
    icon: <ChatIcon />,
  },
  {
    title: "YouTube",
    copy: "Workshops, talks, and walkthroughs from the ChilliCream team.",
    href: YOUTUBE,
    external: true,
    icon: <PlayIcon />,
  },
  {
    title: "GitHub",
    copy: "Source, issues, and discussions for graphql-platform.",
    href: GITHUB,
    external: true,
    icon: <BranchIcon />,
  },
];

/**
 * The self-serve channels: five linked cards (docs, blog, and the community
 * channels) to try before booking a session, rendered with the shared
 * `IconFeatureCard`.
 */
export function SelfServeGrid() {
  return (
    <section aria-labelledby="help-channels-heading" className="py-16">
      <div className="mb-12">
        <SectionHeading
          align="center"
          eyebrow="First stop"
          title="Try the self-serve resources before you ask."
          titleId="help-channels-heading"
          description="Most questions have already been answered. Five places to look before you reach out."
        />
      </div>
      <CardGrid cols={3} step="progressive" gap={6}>
        {CHANNELS.map((channel) => (
          <ChannelCard key={channel.title} channel={channel} />
        ))}
      </CardGrid>
    </section>
  );
}

function ChannelCard({ channel }: { readonly channel: Channel }) {
  const className =
    "group block h-full rounded-3xl [&_article]:transition-colors hover:[&_article]:border-cc-card-border-hover";
  const card = (
    <IconFeatureCard
      icon={channel.icon}
      title={channel.title}
      copy={channel.copy}
    />
  );

  if (channel.external) {
    return (
      <a
        href={channel.href}
        target="_blank"
        rel="noopener noreferrer"
        className={className}
      >
        {card}
      </a>
    );
  }

  return (
    <NextLink href={channel.href} className={className}>
      {card}
    </NextLink>
  );
}
