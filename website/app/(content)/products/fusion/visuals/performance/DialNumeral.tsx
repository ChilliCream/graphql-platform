import { token } from "@/src/nitro";

import { polarCss } from "./gauge";
import { NUMERAL_COLOR, NUMERAL_FONT_PX, VB, numeralInset } from "./hud";

interface DialNumeralProps {
  readonly angle: number;
  readonly label: string;
  readonly edge: number;
}

export function DialNumeral({ angle, label, edge }: DialNumeralProps) {
  const { left, top } = polarCss(angle, edge / VB, -numeralInset(angle, label));

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
