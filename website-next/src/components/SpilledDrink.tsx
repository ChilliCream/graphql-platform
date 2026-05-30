type SpilledDrinkProps = {
  className?: string;
};

const CREAM = "#f5f1ea";
const PURPLE = "#d3bce0";
const PURPLE_DARK = "#b89ccc";
const MAGENTA = "#c0316f";

/**
 * Playful 404 illustration: a milkshake cup knocked onto its side with the
 * domed lid popped loose, the drink pouring out of the mouth into a puddle.
 */
export function SpilledDrink({ className }: SpilledDrinkProps) {
  return (
    <svg
      viewBox="0 0 480 320"
      fill="none"
      role="img"
      aria-label="A milkshake cup knocked on its side, spilling into a puddle"
      className={className}
    >
      {/* puddle */}
      <path
        d="M150 262 C118 262 114 240 158 232 C216 222 272 234 322 240 C374 246 416 236 434 256 C445 269 410 283 354 284 C266 286 196 288 156 278 C140 274 138 266 150 262 Z"
        fill={PURPLE}
        stroke={CREAM}
        strokeWidth="8"
        strokeLinejoin="round"
      />
      <ellipse cx="298" cy="262" rx="66" ry="17" fill={MAGENTA} opacity="0.9" />

      {/* pour ribbon from the mouth into the puddle */}
      <path
        d="M286 188 C296 208 306 226 314 246 C319 259 292 262 283 249 C271 230 272 208 286 188 Z"
        fill={PURPLE}
        stroke={CREAM}
        strokeWidth="8"
        strokeLinejoin="round"
      />

      {/* cup lying on its side, mouth to the right (tilted to pour) */}
      <g transform="rotate(8 184 160)">
        <path
          d="M66 124 C52 124 46 140 46 160 C46 180 52 196 66 196 L300 208 L300 112 Z"
          fill={PURPLE}
          stroke={CREAM}
          strokeWidth="8"
          strokeLinejoin="round"
        />
        {/* soft top highlight */}
        <path
          d="M70 128 L296 116 L296 140 L72 150 Z"
          fill={CREAM}
          opacity="0.12"
        />
        {/* magenta band near the mouth */}
        <path d="M250 114 L276 113 L276 207 L250 205 Z" fill={MAGENTA} />
        {/* mouth: liquid surface + cream rim ring */}
        <ellipse cx="300" cy="160" rx="15" ry="48" fill={PURPLE_DARK} />
        <ellipse
          cx="300"
          cy="160"
          rx="15"
          ry="48"
          fill="none"
          stroke={CREAM}
          strokeWidth="8"
        />
      </g>

      {/* loose domed lid, popped off above-right */}
      <g transform="rotate(18 384 116)">
        <rect x="332" y="116" width="104" height="15" rx="7" fill={CREAM} />
        <path
          d="M340 118 C340 84 360 64 384 64 C408 64 428 84 428 118 Z"
          fill={CREAM}
        />
      </g>
    </svg>
  );
}
