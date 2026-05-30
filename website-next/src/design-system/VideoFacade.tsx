"use client";

import { useState } from "react";

type VideoFacadeProps = {
  /** YouTube video id; used to derive the poster and embed when given. */
  videoId?: string;
  /** Poster image URL (overrides the YouTube thumbnail; required for generic). */
  poster?: string;
  /** Iframe URL loaded on click (overrides the YouTube embed; for generic). */
  embedSrc?: string;
  playlabel?: string;
  /**
   * Source provider. Only YouTube gets the YouTube-red play button (matching
   * lite-youtube-embed / `@next/third-parties`); anything else uses our accent.
   */
  provider?: "youtube" | "generic";
};

/**
 * "Facade" YouTube embed: renders the poster image until clicked, then swaps
 * in the iframe with `autoplay=1`. Avoids loading the lite-yt-embed stylesheet
 * (a render-blocking jsdelivr request from `@next/third-parties`), and defers
 * the iframe payload until interaction.
 *
 * The play button mirrors lite-youtube-embed: a dark translucent lozenge at
 * rest that lights up (YouTube red, or our accent for non-YouTube) on hover.
 */
export function VideoFacade({
  videoId,
  poster,
  embedSrc,
  playlabel = "Play video",
  provider = "youtube",
}: VideoFacadeProps) {
  const [active, setActive] = useState(false);

  const posterSrc = poster ?? `https://i.ytimg.com/vi/${videoId}/hqdefault.jpg`;
  // The responsive YouTube thumbnails only apply when we derive them from an id.
  const posterSrcSet =
    !poster && videoId
      ? `https://i.ytimg.com/vi/${videoId}/hqdefault.jpg 480w, https://i.ytimg.com/vi/${videoId}/maxresdefault.jpg 1280w`
      : undefined;
  const embedUrl =
    embedSrc ??
    `https://www.youtube-nocookie.com/embed/${videoId}?autoplay=1&rel=0`;

  const playHover =
    provider === "youtube"
      ? "group-hover:bg-cc-youtube"
      : "group-hover:bg-cc-accent";

  if (active) {
    return (
      <div className="relative aspect-video w-full bg-cc-black">
        <iframe
          src={embedUrl}
          title={playlabel}
          allow="accelerometer; autoplay; encrypted-media; gyroscope; picture-in-picture"
          allowFullScreen
          loading="lazy"
          className="absolute inset-0 h-full w-full border-0"
        />
      </div>
    );
  }

  return (
    <button
      type="button"
      onClick={() => setActive(true)}
      aria-label={playlabel}
      className="group relative block aspect-video w-full cursor-pointer overflow-hidden border-0 bg-cc-black p-0"
    >
      {/* Thumbnail via YouTube i.ytimg CDN — `loading=lazy` defers off-screen
          videos until they're near the viewport. */}
      {/* eslint-disable-next-line @next/next/no-img-element */}
      <img
        src={posterSrc}
        srcSet={posterSrcSet}
        sizes="(min-width: 768px) 768px, 100vw"
        alt=""
        loading="lazy"
        decoding="async"
        className="h-full w-full object-cover transition-transform duration-300 group-hover:scale-[1.02]"
      />
      <span
        aria-hidden="true"
        className="absolute inset-0 flex items-center justify-center"
      >
        <span
          className={`flex h-12 w-17 items-center justify-center rounded-[14%] bg-cc-black/70 text-cc-white transition-colors ${playHover}`}
        >
          <svg
            viewBox="0 0 24 24"
            fill="currentColor"
            className="h-7 w-7"
            aria-hidden="true"
          >
            <path d="M8 5v14l11-7z" />
          </svg>
        </span>
      </span>
    </button>
  );
}
