import type { ReactElement } from "react";

import { Card } from "@/src/design-system/Card";

interface LinkCardProps {
  readonly href: string;
  readonly title: string;
  readonly description: string;
  /**
   * `"trailing"`: ends with a "Learn more →" affordance.
   * `"plain"`: title and description only.
   * `"icon"`: horizontal layout with a leading icon tile, no trailing affordance.
   */
  readonly variant: "trailing" | "plain" | "icon";
  /** Icon rendered in the leading tile. Required when `variant` is `"icon"`. */
  readonly icon?: ReactElement;
  /** Opens the link in a new tab with `rel="noopener noreferrer"`. */
  readonly external?: boolean;
}

export function LinkCard({
  href,
  title,
  description,
  variant,
  icon,
  external = false,
}: LinkCardProps) {
  const linkProps = external
    ? { target: "_blank", rel: "noopener noreferrer" }
    : {};

  if (variant === "icon") {
    return (
      <li>
        <Card
          as="a"
          href={href}
          {...linkProps}
          variant="plain"
          className="group hover:border-cc-accent flex h-full items-center gap-4 p-5 no-underline transition-colors"
        >
          <span className="bg-cc-hover ring-cc-card-border flex h-14 w-14 flex-none items-center justify-center rounded-xl ring-1">
            {icon}
          </span>
          <span className="flex flex-col">
            <span className="font-heading text-cc-heading text-lg font-semibold">
              {title}
            </span>
            <span className="text-cc-ink-dim text-sm">{description}</span>
          </span>
        </Card>
      </li>
    );
  }

  if (variant === "plain") {
    return (
      <Card
        as="a"
        href={href}
        {...linkProps}
        variant="tile"
        className="group hover:border-cc-accent flex flex-col no-underline transition-colors"
      >
        <h2 className="text-cc-heading text-lg font-semibold">{title}</h2>
        <p className="text-cc-ink-dim mt-2 text-sm">{description}</p>
      </Card>
    );
  }

  return (
    <Card
      as="a"
      href={href}
      {...linkProps}
      variant="tile"
      className="group hover:border-cc-accent flex flex-col no-underline transition-colors"
    >
      <h2 className="text-cc-heading text-xl font-semibold">{title}</h2>
      <p className="text-cc-ink-dim mt-3 text-sm">{description}</p>
      <span className="text-cc-accent mt-6 text-sm font-medium">
        Learn more →
      </span>
    </Card>
  );
}
