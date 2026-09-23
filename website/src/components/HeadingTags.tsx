import { Tag } from "@/src/design-system/Tag";

/** Tags an author declared for a heading, in markdown or via frontmatter. */
export type PageTags = {
  /** Product version that introduced the feature the heading describes. */
  readonly since?: string;
  /** Lowest self-hosted Nitro backend version the feature works with. */
  readonly requiresNitro?: string;
};

type HeadingTagsProps = PageTags;

// Native tooltips render the newline, keeping the caveat on its own line.
const NITRO_VERSION_TOOLTIP =
  "Minimum Nitro backend version required.\nOnly relevant when self-hosting Nitro.";

/**
 * Renders the tag line of a docs heading (see the `Since:` / `Nitro:`
 * markdown convention). Tags always render in the same order, no matter how
 * the author ordered them in the source.
 */
export function HeadingTags({ since, requiresNitro }: HeadingTagsProps) {
  if (!since && !requiresNitro) {
    return null;
  }

  return (
    <span className="ml-2 inline-flex flex-wrap items-center gap-2 text-base leading-none font-normal">
      {since ? (
        <Tag title="Minimum package/tool version required.">{since}+</Tag>
      ) : null}
      {requiresNitro ? (
        <Tag title={NITRO_VERSION_TOOLTIP}>Nitro {requiresNitro}+</Tag>
      ) : null}
    </span>
  );
}
