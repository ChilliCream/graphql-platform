"use client";

import { useEffect, useRef, useState, type ReactNode } from "react";
import { sendAnalyticsEvent } from "@/src/helpers/analytics";
import { YouTubePlayButton } from "./YouTubePlayButton";

type VideoFacadeProps = {
  /** 11-character YouTube video id. */
  videoId: string;
  /** Visually hidden label for the play button. */
  playlabel?: string;
  /**
   * Loads the player without a click, and without autoplay, once the visitor
   * has given Cookiebot marketing consent.
   */
  autoload?: boolean;
  /** The (server-rendered) poster shown until the player loads. */
  children: ReactNode;
};

type PlayerState = "poster" | "loaded" | "playing";

/**
 * Click-to-load YouTube facade: shows the poster (passed as children) behind a
 * play button and only mounts the embed iframe once clicked (or, with
 * `autoload`, once marketing consent is given), so no YouTube player code loads
 * during the initial render. Internal to <YouTubeVideo>.
 */
export function VideoFacade({
  videoId,
  playlabel = "Play video",
  autoload = false,
  children,
}: VideoFacadeProps) {
  const [player, setPlayer] = useState<PlayerState>("poster");
  const frameRef = useRef<HTMLIFrameElement>(null);

  useEffect(() => {
    if (!autoload) {
      return;
    }

    function loadWithConsent() {
      if (window.Cookiebot?.consent?.marketing === true) {
        setPlayer((current) => (current === "poster" ? "loaded" : current));
      }
    }

    loadWithConsent();
    window.addEventListener("CookiebotOnConsentReady", loadWithConsent);
    return () =>
      window.removeEventListener("CookiebotOnConsentReady", loadWithConsent);
  }, [autoload]);

  useEffect(() => {
    if (player !== "loaded") {
      return;
    }

    // The player is cross-origin, so the first click into it shows up as the
    // page losing focus to the iframe.
    function trackFirstInteraction() {
      if (document.activeElement !== frameRef.current) {
        return;
      }
      window.removeEventListener("blur", trackFirstInteraction);
      sendVideoStart(videoId);
    }

    window.addEventListener("blur", trackFirstInteraction);
    return () => window.removeEventListener("blur", trackFirstInteraction);
  }, [player, videoId]);

  if (player !== "poster") {
    const autoplay = player === "playing" ? "&autoplay=1" : "";
    const embedUrl = `https://www.youtube-nocookie.com/embed/${videoId}?rel=0${autoplay}`;
    return (
      <div className="bg-cc-black relative aspect-video w-full">
        <iframe
          ref={frameRef}
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
        sendVideoStart(videoId);
        setPlayer("playing");
      }}
      aria-label={playlabel}
      className="group bg-cc-black relative block aspect-video w-full cursor-pointer overflow-hidden border-0 p-0"
    >
      {children}
      <YouTubePlayButton />
    </button>
  );
}

function sendVideoStart(videoId: string) {
  sendAnalyticsEvent("video_start", {
    video_id: videoId,
    page_path: window.location.pathname,
  });
}
