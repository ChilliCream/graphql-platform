import { forwardRef, type ReactNode } from "react";

interface LandingRootProps {
  children?: ReactNode;
}

// Plain <div> with the `.cc-landing-root` class. All the visual styling lives
// in landing-styles.css (extracted from /website's styled-components template
// so the design ports across 1:1 without dragging in styled-components).
export const LandingRoot = forwardRef<HTMLDivElement, LandingRootProps>(
  function LandingRoot({ children }, ref) {
    return (
      <div ref={ref} className="cc-landing-root">
        {children}
      </div>
    );
  }
);
