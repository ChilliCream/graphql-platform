import { token } from "@/src/nitro";

import { polarCss } from "./gauge";
import {
  NUMERAL_COLOR,
  NUMERAL_FONT_PX,
  NUMERAL_OFFSET_PX,
  NUMERAL_R_FRACTION,
} from "./hud";

interface DialNumeralProps {
  readonly angle: number;
  readonly label: string;
}

export function DialNumeral({ angle, label }: DialNumeralProps) {
  const { left, top } = polarCss(angle, NUMERAL_R_FRACTION, NUMERAL_OFFSET_PX);

  return (
    <div
      aria-hidden="true"
      className="whitespace-nowrap"
      style={{
        position: "absolute",
        left,
        top,
        transform: "translate(-50%, -50%)",
        fontSize: NUMERAL_FONT_PX,
        lineHeight: 1,
        fontFamily: token.mono,
        fontVariantNumeric: "tabular-nums",
        color: NUMERAL_COLOR,
      }}
    >
      {label}
    </div>
  );
}
