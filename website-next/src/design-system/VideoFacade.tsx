"use client";

import { useState } from "react";

type CommonProps = {
  playlabel?: string;
};

type YouTubeFacadeProps = CommonProps & {
  provider?: "youtube";
  videoId: string;
  poster?: never;
  embedSrc?: never;
};

type GenericFacadeProps = CommonProps & {
  provider: "generic";
  poster: string;
  embedSrc: string;
  videoId?: never;
};

type VideoFacadeProps = YouTubeFacadeProps | GenericFacadeProps;

export function VideoFacade(props: VideoFacadeProps) {
  const { playlabel = "Play video" } = props;
  const [active, setActive] = useState(false);

  const isGeneric = props.provider === "generic";
  const posterSrc = isGeneric
    ? props.poster
    : `https://i.ytimg.com/vi/${props.videoId}/hqdefault.jpg`;
  // The responsive YouTube thumbnails only apply when we derive them from an id.
  const posterSrcSet = isGeneric
    ? undefined
    : `https://i.ytimg.com/vi/${props.videoId}/hqdefault.jpg 480w, https://i.ytimg.com/vi/${props.videoId}/maxresdefault.jpg 1280w`;
  const embedUrl = isGeneric
    ? props.embedSrc
    : `https://www.youtube-nocookie.com/embed/${props.videoId}?autoplay=1&rel=0`;

  const playHover = isGeneric
    ? "group-hover:bg-cc-accent"
    : "group-hover:bg-cc-youtube";

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
