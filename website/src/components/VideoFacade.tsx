"use client";

import { useState, type ReactNode } from "react";

type VideoFacadeProps = {
  /** 11-character YouTube video id. */
  videoId: string;
  /** Visually hidden label for the play button. */
  playlabel?: string;
  /** The (server-rendered) poster shown until the user clicks to play. */
  children: ReactNode;
};

/**
 * Click-to-load YouTube facade: shows the poster (passed as children) behind a
 * play button and only mounts the embed iframe once clicked, so no YouTube
 * player code loads during the initial render. Internal to <YouTubeVideo>.
 */
export function VideoFacade({
  videoId,
  playlabel = "Play video",
  children,
}: VideoFacadeProps) {
  const [active, setActive] = useState(false);

  if (active) {
    const embedUrl = `https://www.youtube-nocookie.com/embed/${videoId}?autoplay=1&rel=0`;
    return (
      <div className="bg-cc-black relative aspect-video w-full">
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
      className="group bg-cc-black relative block aspect-video w-full cursor-pointer overflow-hidden border-0 p-0"
    >
      {children}
      <span
        aria-hidden="true"
        className="absolute inset-0 flex items-center justify-center"
      >
        <span className="bg-cc-black/70 text-cc-white group-hover:bg-cc-youtube flex h-12 w-17 items-center justify-center rounded-[14%] transition-colors">
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
