import { PlayGlyph } from "@/src/icons/PlayGlyph";

/**
 * YouTube-style play button centered over a poster. Its positioned parent must
 * be a `group`; the button turns YouTube red while the group is hovered.
 */
export function YouTubePlayButton() {
  return (
    <span
      aria-hidden="true"
      className="absolute inset-0 flex items-center justify-center"
    >
      <span className="bg-cc-black/70 text-cc-white group-hover:bg-cc-youtube flex h-12 w-17 items-center justify-center rounded-[14%] transition-colors">
        <PlayGlyph className="h-7 w-7" />
      </span>
    </span>
  );
}
