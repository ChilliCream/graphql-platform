export interface Pin {
  readonly x: number;
  readonly y: number;
  readonly side: "left" | "right" | "top" | "bottom";
}

const PAD_FILL = "rgba(158,176,204,0.34)";

interface PinRowProps {
  readonly pin: Pin;
}

export function PinRow({ pin }: PinRowProps) {
  const { x, y, side } = pin;
  return (
    <g>
      {[-1, 0, 1].map((i) =>
        side === "left" || side === "right" ? (
          <rect key={i} x={side === "left" ? x - 3.5 : x} y={y + i * 5 - 1} width={3.5} height={2} fill={PAD_FILL} />
        ) : (
          <rect key={i} x={x + i * 5 - 1} y={side === "top" ? y - 3.5 : y} width={2} height={3.5} fill={PAD_FILL} />
        ),
      )}
    </g>
  );
}
