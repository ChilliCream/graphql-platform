"use client";

import React, { FC, useCallback, useEffect, useState } from "react";

interface ImageMapEntry {
  /** Default WebP src */
  s: string;
  /** WebP srcSet */
  w: string;
  /** AVIF srcSet */
  a: string;
  /** Base64 placeholder */
  p: string;
  /** width */
  W: number;
  /** height */
  H: number;
}

type ImageMap = Record<string, ImageMapEntry>;

// Singleton: load the image map once and cache it
let imageMapPromise: Promise<ImageMap> | null = null;

function fetchImageMap(): Promise<ImageMap> {
  if (!imageMapPromise) {
    imageMapPromise = fetch("/optimized/image-map.json")
      .then((res) => (res.ok ? res.json() : {}))
      .catch(() => ({}));
  }
  return imageMapPromise;
}

export interface MdxImageProps {
  src?: string;
  alt?: string;
  title?: string;
  width?: number | string;
  height?: number | string;
}

/**
 * Drop-in `img` replacement for MDX content.
 *
 * On mount, looks up the image src in the optimized image map.
 * If found, renders a <picture> with AVIF/WebP srcsets, blur-up placeholder,
 * and proper width/height for CLS prevention.
 * If not found, falls back to a regular <img>.
 */
export const MdxImage: FC<MdxImageProps> = ({ src, alt = "", ...rest }) => {
  const [entry, setEntry] = useState<ImageMapEntry | null>(null);
  const [loaded, setLoaded] = useState(false);
  const [checked, setChecked] = useState(false);

  useEffect(() => {
    if (!src) {
      setChecked(true);
      return;
    }

    fetchImageMap().then((map) => {
      setEntry(map[src] || null);
      setChecked(true);
    });
  }, [src]);

  const handleLoad = useCallback(() => {
    setLoaded(true);
  }, []);

  if (!src) return null;

  // Still loading the map - render the plain img to avoid flash
  if (!checked) {
    return (
      <img
        src={src}
        alt={alt}
        loading="lazy"
        decoding="async"
        style={{ width: "100%", height: "auto" }}
        {...rest}
      />
    );
  }

  // No optimized version available - plain img with basic improvements
  if (!entry) {
    return (
      <img
        src={src}
        alt={alt}
        loading="lazy"
        decoding="async"
        style={{ width: "100%", height: "auto" }}
        {...rest}
      />
    );
  }

  const sizes = "(max-width: 640px) 100vw, (max-width: 1024px) 75vw, 660px";

  return (
    <span
      style={{
        display: "block",
        position: "relative",
        overflow: "hidden",
        width: "100%",
        maxWidth: entry.W,
        marginBottom: 16,
      }}
    >
      {/* Blur-up placeholder */}
      {entry.p && (
        <img
          src={entry.p}
          alt=""
          aria-hidden="true"
          style={{
            position: "absolute",
            top: 0,
            left: 0,
            width: "100%",
            height: "100%",
            objectFit: "cover",
            filter: "blur(20px)",
            transform: "scale(1.1)",
            transition: "opacity 0.4s ease-in-out",
            opacity: loaded ? 0 : 1,
            pointerEvents: "none",
          }}
        />
      )}
      <picture>
        {entry.a && <source type="image/avif" srcSet={entry.a} sizes={sizes} />}
        {entry.w && <source type="image/webp" srcSet={entry.w} sizes={sizes} />}
        <img
          src={entry.s || src}
          alt={alt}
          width={entry.W}
          height={entry.H}
          sizes={sizes}
          loading="lazy"
          decoding="async"
          onLoad={handleLoad}
          style={{
            display: "block",
            width: "100%",
            height: "auto",
            transition: "opacity 0.4s ease-in-out",
            opacity: loaded ? 1 : 0,
          }}
        />
      </picture>
    </span>
  );
};
