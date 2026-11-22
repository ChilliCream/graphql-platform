"use client";

import { PauseGlyph } from "@/src/icons/PauseGlyph";
import { PlayGlyph } from "@/src/icons/PlayGlyph";

interface PlaybackToggleProps {
  readonly paused: boolean;
  readonly onToggle: () => void;
  /** Id of the element that says which animation the button controls. */
  readonly describedBy?: string;
  readonly className?: string;
}

/**
 * Pauses and resumes a looping diagram (WCAG 2.2.2). Hidden under reduced
 * motion, where the diagrams show a static frame.
 */
export function PlaybackToggle({
  paused,
  onToggle,
  describedBy,
  className = "",
}: PlaybackToggleProps) {
  return (
    <button
      type="button"
      onClick={onToggle}
      aria-label={paused ? "Play animation" : "Pause animation"}
      aria-describedby={describedBy}
      title={paused ? "Play animation" : "Pause animation"}
      className={`border-cc-card-border bg-cc-code-header text-cc-ink-dim hover:text-cc-ink focus-visible:ring-cc-accent/50 inline-flex size-7 cursor-pointer items-center justify-center rounded-md border transition-colors focus-visible:ring-2 focus-visible:outline-none motion-reduce:hidden ${className}`}
    >
      {paused ? (
        <PlayGlyph className="size-3.5" />
      ) : (
        <PauseGlyph className="size-3.5" />
      )}
    </button>
  );
}
