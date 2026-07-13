import NextLink from "next/link";
import type { ReactElement } from "react";

import { CardGrid } from "@/src/components/CardGrid";
import { GITHUB, SLACK, YOUTUBE } from "@/src/components/help/helpLinks";
import { IconFeatureCard } from "@/src/components/IconFeatureCard";
import { SectionHeading } from "@/src/components/SectionHeading";
import { IconElement } from "@/src/icons/IconElement";

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
    icon: <IconElement icon="file-lines" size="2x" />,
  },
  {
    title: "Blog",
    copy: "Release notes, deep dives, and patterns from the team.",
    href: "/blog",
    external: false,
    icon: <IconElement icon="newspaper" size="2x" />,
  },
  {
    title: "Slack",
    copy: "Live conversation with maintainers and 7000+ developers.",
    href: SLACK,
    external: true,
    icon: <IconElement icon="message" size="2x" />,
  },
  {
    title: "YouTube",
    copy: "Workshops, talks, and walkthroughs from the ChilliCream team.",
    href: YOUTUBE,
    external: true,
    icon: <IconElement icon="play-circle" size="2x" />,
  },
  {
    title: "GitHub",
    copy: "Source, issues, and discussions for graphql-platform.",
    href: GITHUB,
    external: true,
    icon: <IconElement icon="code-branch" size="2x" />,
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
