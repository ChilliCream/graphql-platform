import { PlayGlyph } from "@/src/icons/PlayGlyph";
import { YouTubePoster } from "@/src/components/YouTubePoster";

type PlayButtonSize = "none" | "sm" | "md" | "lg";

interface VideoPosterProps {
  /** 11-character YouTube video id. */
  readonly videoId: string;
  readonly sizes: string;
  readonly playButton?: PlayButtonSize;
  readonly priority?: boolean;
  /** Classes for the 16:9 frame, e.g. width, border, or corner radius. */
  readonly className?: string;
}

const PLAY_BUTTON: Record<Exclude<PlayButtonSize, "none">, string> = {
  sm: "bottom-1.5 left-1.5 size-6",
  md: "bottom-3 left-3 size-10",
  lg: "bottom-4 left-4 size-12 sm:bottom-5 sm:left-5 sm:size-14",
};

const PLAY_GLYPH: Record<Exclude<PlayButtonSize, "none">, string> = {
  sm: "size-3",
  md: "size-5",
  lg: "size-6 sm:size-7",
};

/**
 * A 16:9 video poster with a play button in its bottom-left corner. Hover
 * effects follow the nearest `group/video` ancestor, so the whole card or link
 * reacts as one.
 */
export function VideoPoster({
  videoId,
  sizes,
  playButton = "md",
  priority = false,
  className = "",
}: VideoPosterProps) {
  return (
    <div
      className={`bg-cc-black relative aspect-video overflow-hidden ${className}`}
    >
      <YouTubePoster
        videoId={videoId}
        sizes={sizes}
        priority={priority}
        className="h-full w-full object-cover transition-transform duration-300 group-hover/video:scale-[1.03]"
      />
      {playButton === "none" ? null : (
        <span
          aria-hidden="true"
          className={`bg-cc-black/75 text-cc-white group-hover/video:bg-cc-amber group-hover/video:text-cc-surface absolute flex items-center justify-center rounded-full transition-colors duration-200 ${PLAY_BUTTON[playButton]}`}
        >
          <PlayGlyph className={`translate-x-[8%] ${PLAY_GLYPH[playButton]}`} />
        </span>
      )}
    </div>
  );
}
