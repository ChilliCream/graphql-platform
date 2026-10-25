"use client";

import { useEffect, useRef, useState, type ReactNode } from "react";
import {
  canLoadMarketingEmbeds,
  grantMarketingConsent,
  sendAnalyticsEvent,
} from "@/src/helpers/analytics";
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

type PlayerState = "poster" | "consent" | "loaded" | "playing";

/**
 * Click-to-load YouTube facade: shows the poster (passed as children) behind a
 * play button and only mounts the embed iframe once marketing consent is given:
 * on click, or without a click with `autoload`. A click without consent asks
 * for it first. Internal to <YouTubeVideo>.
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
    function loadWithConsent() {
      if (window.Cookiebot?.consent?.marketing !== true) {
        return;
      }
      setPlayer((current) => {
        if (current === "consent") {
          return "playing";
        }
        return autoload && current === "poster" ? "loaded" : current;
      });
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

  if (player === "loaded" || player === "playing") {
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

  if (player === "consent") {
    return (
      <div className="bg-cc-black relative aspect-video w-full overflow-hidden">
        {children}
        <div
          role="group"
          aria-label={playlabel}
          className="bg-cc-black/85 text-cc-white absolute inset-0 flex flex-col items-center justify-center gap-3 p-4 text-center"
        >
          <p className="m-0 max-w-md text-xs leading-relaxed sm:text-sm">
            This video is played by YouTube, which may set cookies. Playing it
            requires your consent to marketing cookies.
          </p>
          <div className="flex flex-wrap items-center justify-center gap-2">
            <button
              type="button"
              onClick={() => {
                if (grantMarketingConsent() && canLoadMarketingEmbeds()) {
                  sendVideoStart(videoId);
                  setPlayer("playing");
                }
              }}
              className="bg-cc-youtube text-cc-white cursor-pointer rounded-full border-0 px-4 py-1.5 text-xs font-medium sm:text-sm"
            >
              Accept and play
            </button>
            <a
              href={`https://www.youtube.com/watch?v=${videoId}`}
              target="_blank"
              rel="noopener noreferrer"
              className="bg-cc-white/10 hover:bg-cc-white/15 text-cc-white rounded-full px-4 py-1.5 text-xs font-medium no-underline transition-colors sm:text-sm"
            >
              Watch on YouTube
            </a>
          </div>
        </div>
      </div>
    );
  }

  return (
    <button
      type="button"
      onClick={() => {
        if (!canLoadMarketingEmbeds()) {
          setPlayer("consent");
          return;
        }
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
