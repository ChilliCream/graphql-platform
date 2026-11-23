import { InfoHint } from "@/src/components/InfoHint";
import { Tag } from "@/src/design-system/Tag";
import { ArrowRightIcon } from "@/src/icons/ArrowRight";

type Status = "planned" | "in-progress" | "in-review";

interface FeatureStatusProps {
  readonly status: Status;
  /** GitHub issue or pull request that tracks the feature; omitted until one exists. */
  readonly href?: string;
}

const STATUSES: Record<
  Status,
  { label: string; hint: string; linkedHint: string }
> = {
  planned: {
    label: "Planned",
    hint: "On our roadmap, but not started yet.",
    linkedHint:
      "On our roadmap, but not started yet. Upvote the issue on GitHub so we know what to build first.",
  },
  "in-progress": {
    label: "In progress",
    hint: "We are building it.",
    linkedHint: "We are building it. Follow the issue on GitHub for updates.",
  },
  "in-review": {
    label: "In review",
    hint: "It is built and in review.",
    linkedHint:
      "It is built and in review. Follow the pull request on GitHub to see when it ships.",
  },
};

/**
 * Where a feature the product does not have yet stands, with an explanation
 * behind a hint button. With `href`, the badge links to the issue or pull
 * request and carries an arrow. In a narrow cell the hint wraps below the
 * badge; the badge text itself never wraps.
 */
export function FeatureStatus({ status, href }: FeatureStatusProps) {
  const { label, hint, linkedHint } = STATUSES[status];

  return (
    <span className="inline-flex flex-wrap items-center gap-y-1">
      {href ? (
        <Tag href={href} className="group/status gap-1 whitespace-nowrap">
          {label}
          <span className="sr-only">, tracked on GitHub</span>
          <ArrowRightIcon className="size-3 shrink-0 -rotate-45 transition-transform group-hover/status:translate-x-px group-hover/status:-translate-y-px motion-reduce:transition-none" />
        </Tag>
      ) : (
        <Tag className="whitespace-nowrap">{label}</Tag>
      )}
      <InfoHint label={label} align="center">
        {href ? linkedHint : hint}
      </InfoHint>
    </span>
  );
}
