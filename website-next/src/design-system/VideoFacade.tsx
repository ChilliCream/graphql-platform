"use client";

import { useState } from "react";

type VideoFacadeProps = {
  videoId: string;
  playlabel?: string;
};

/**
 * "Facade" YouTube embed: renders the poster image until clicked, then swaps
 * in the iframe with `autoplay=1`. Avoids loading the lite-yt-embed stylesheet
 * (a render-blocking jsdelivr request from `@next/third-parties`), and defers
 * the iframe payload until interaction.
 */
export function VideoFacade({
  videoId,
  playlabel = "Play video",
}: VideoFacadeProps) {
  const [active, setActive] = useState(false);

  if (active) {
    return (
      <div className="relative aspect-video w-full bg-cc-black">
        <iframe
          src={`https://www.youtube-nocookie.com/embed/${videoId}?autoplay=1&rel=0`}
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
        src={`https://i.ytimg.com/vi/${videoId}/hqdefault.jpg`}
        srcSet={`https://i.ytimg.com/vi/${videoId}/hqdefault.jpg 480w, https://i.ytimg.com/vi/${videoId}/maxresdefault.jpg 1280w`}
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
        <span className="flex h-16 w-24 items-center justify-center rounded-xl bg-cc-youtube text-cc-white transition-colors group-hover:bg-cc-youtube-hover">
          <svg
            viewBox="0 0 24 24"
            fill="currentColor"
            className="ml-1 h-8 w-8"
            aria-hidden="true"
          >
            <path d="M8 5v14l11-7z" />
          </svg>
        </span>
      </span>
    </button>
  );
}
