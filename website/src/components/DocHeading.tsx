import type { ComponentPropsWithoutRef } from "react";
import { Typography, type TypographyVariant } from "@/src/design-system/Typography";
import { HeadingTags } from "./HeadingTags";

interface DocHeadingProps extends ComponentPropsWithoutRef<"h2"> {
  readonly variant: TypographyVariant;
  /** Set by the `headingTags` remark plugin from the markdown tag line. */
  readonly "data-since"?: string;
  /** Set by the `headingTags` remark plugin from the markdown tag line. */
  readonly "data-requires-nitro"?: string;
}

/**
 * Heading used for markdown content: permalink anchor plus the optional tag
 * badges an author declared in the tag line below the heading.
 */
export function DocHeading({
  variant,
  "data-since": since,
  "data-requires-nitro": requiresNitro,
  className,
  ...rest
}: DocHeadingProps) {
  const hasTags = Boolean(since ?? requiresNitro);

  return (
    <Typography
      variant={variant}
      anchor
      // Tagged headings lay their parts out in a row so the badges stay
      // centered against the heading text whatever its height.
      className={[hasTags ? "flex flex-wrap items-center" : "", className ?? ""].filter(Boolean).join(" ")}
      adornment={<HeadingTags since={since} requiresNitro={requiresNitro} />}
      {...rest}
    />
  );
}
