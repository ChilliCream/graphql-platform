import type { CSSProperties } from "react";

import { lightDark } from "@/src/components/lightDark";

export const CARD_WASH_CLASS = "bg-(--card-wash) hover:bg-(--card-wash-hover)";

export const CARD_WASH_STYLE = {
  "--card-wash": lightDark(
    "var(--color-cc-card-bg)",
    "rgba(255,255,255,0.025)",
  ),
  "--card-wash-hover": lightDark(
    "color-mix(in srgb, var(--color-cc-card-bg), var(--color-cc-heading) 4%)",
    "rgba(255,255,255,0.05)",
  ),
} as CSSProperties;

export const MEDIA_WASH = lightDark(
  "var(--color-cc-code-bg)",
  "rgba(255,255,255,0.04)",
);
