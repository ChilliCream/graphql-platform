import { InfoHint } from "@/src/components/InfoHint";
import { Tag } from "@/src/design-system/Tag";

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
 * Where a feature the product does not have yet stands, with an explanation on
 * hover. With `href`, the badge links to the issue or pull request.
 */
export function FeatureStatus({ status, href }: FeatureStatusProps) {
  const { label, hint, linkedHint } = STATUSES[status];

  return (
    <span className="inline-flex items-center whitespace-nowrap">
      {href ? <Tag href={href}>{label}</Tag> : <Tag>{label}</Tag>}
      <InfoHint label={label} align="center">
        {href ? linkedHint : hint}
      </InfoHint>
    </span>
  );
}
