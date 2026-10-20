"use client";

import { useState, type ReactNode } from "react";
import { sendAnalyticsEvent } from "@/src/helpers/analytics";
import { YouTubePlayButton } from "./YouTubePlayButton";

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
      onClick={() => {
        sendAnalyticsEvent("video_start", {
          video_id: videoId,
          page_path: window.location.pathname,
        });
        setActive(true);
      }}
      aria-label={playlabel}
      className="group bg-cc-black relative block aspect-video w-full cursor-pointer overflow-hidden border-0 p-0"
    >
      {children}
      <YouTubePlayButton />
    </button>
  );
}
