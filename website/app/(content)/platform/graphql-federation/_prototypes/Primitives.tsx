import type { ReactNode } from "react";

export {
  CANON,
  INK_DIM,
  GlowNode,
  GatewayChip,
  NodeCaption,
  SchemaCard,
  schemaRowY,
} from "../visuals/stage";

interface MicroLabelProps {
  readonly children: ReactNode;
  readonly className?: string;
}

/** Shared mono caption voice: 10px, wide tracking, uppercase, dim ink. */
export function MicroLabel({ children, className }: MicroLabelProps) {
  return (
    <span
      className={`text-cc-nav-label font-mono text-[10px] tracking-[0.2em] uppercase ${className ?? ""}`.trim()}
    >
      {children}
    </span>
  );
}

/**
 * Full-width dashed divider between the build-time and runtime halves of a
 * prototype, matching TransitStory's horizon line at y=4300: a dashed rule
 * with "BUILD TIME" above it and "RUNTIME" below, both dimmed mono captions.
 */
export function HorizonRule() {
  return (
    <div className="relative my-10">
      <MicroLabel className="absolute bottom-full left-0 mb-2">
        Build time
      </MicroLabel>
      <div
        aria-hidden="true"
        className="border-t border-dashed border-[rgba(245,241,234,0.22)]"
      />
      <MicroLabel className="absolute top-full left-0 mt-2 opacity-70">
        Runtime
      </MicroLabel>
    </div>
  );
}
