"use client";

import { useState } from "react";
import { Image } from "./Image";

export type VideoPoster = {
  fallbackSrc: string;
  avifSrcSet?: string;
  webpSrcSet?: string;
  blurDataURL?: string;
  blurWidth?: number;
  blurHeight?: number;
  width?: number;
  height?: number;
};

type CommonProps = {
  playlabel?: string;
};

type YouTubeFacadeProps = CommonProps & {
  provider?: "youtube";
  videoId: string;
  poster?: VideoPoster;
  embedSrc?: never;
};

type GenericFacadeProps = CommonProps & {
  provider: "generic";
  poster?: VideoPoster;
  embedSrc: string;
  videoId?: never;
};

type VideoFacadeProps = YouTubeFacadeProps | GenericFacadeProps;

export function VideoFacade(props: VideoFacadeProps) {
  const { playlabel = "Play video" } = props;
  const [active, setActive] = useState(false);

  const isGeneric = props.provider === "generic";
  const { poster } = props;
  const posterSrc = isGeneric
    ? undefined
    : `https://i.ytimg.com/vi/${props.videoId}/hqdefault.jpg`;
  // External fallback (only used when the thumbnail wasn't self-hosted, e.g. an
  // offline build). hqdefault always exists; maxresdefault 404s for many videos,
  // so it must not be offered as a srcset candidate here.
  const posterSrcSet = undefined;
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
      {poster ? (
        <picture className="contents">
          {poster.avifSrcSet && (
            <source
              type="image/avif"
              srcSet={poster.avifSrcSet}
              sizes="(min-width: 768px) 768px, 100vw"
            />
          )}
          {poster.webpSrcSet && (
            <source
              type="image/webp"
              srcSet={poster.webpSrcSet}
              sizes="(min-width: 768px) 768px, 100vw"
            />
          )}
          <Image
            src={poster.fallbackSrc}
            alt=""
            width={poster.width}
            height={poster.height}
            blurDataURL={poster.blurDataURL}
            blurWidth={poster.blurWidth}
            blurHeight={poster.blurHeight}
            loading="lazy"
            decoding="async"
            className="h-full w-full object-cover transition-transform duration-300 group-hover:scale-[1.02]"
          />
        </picture>
      ) : (
        // eslint-disable-next-line @next/next/no-img-element
        <img
          src={posterSrc}
          srcSet={posterSrcSet}
          sizes="(min-width: 768px) 768px, 100vw"
          alt=""
          loading="lazy"
          decoding="async"
          className="h-full w-full object-cover transition-transform duration-300 group-hover:scale-[1.02]"
        />
      )}
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
