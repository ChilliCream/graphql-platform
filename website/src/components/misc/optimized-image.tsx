"use client";

import React, { FC, useCallback, useState } from "react";

export interface OptimizedImageData {
  /** Default src (WebP) */
  src: string;
  /** WebP srcSet */
  srcSet: string;
  /** AVIF srcSet */
  srcSetAvif: string;
  /** Original format srcSet for fallback */
  srcSetOriginal: string;
  /** Base64 blur placeholder */
  placeholder: string;
  /** Original width */
  width: number;
  /** Original height */
  height: number;
  /** Width/Height ratio */
  aspectRatio: number;
  /** Sizes hint */
  sizes: string;
}

export interface OptimizedImageProps extends OptimizedImageData {
  alt: string;
  className?: string;
}

/**
 * Gatsby-like optimized image component with:
 * - Blur-up placeholder effect
 * - Responsive srcset with WebP/AVIF support via <picture>
 * - Lazy loading
 * - Proper width/height to prevent CLS
 */
export const OptimizedImage: FC<OptimizedImageProps> = ({
  src,
  srcSet,
  srcSetAvif,
  srcSetOriginal,
  placeholder,
  width,
  height,
  aspectRatio,
  sizes,
  alt,
  className,
}) => {
  const [loaded, setLoaded] = useState(false);

  const handleLoad = useCallback(() => {
    setLoaded(true);
  }, []);

  if (!srcSet && !src) {
    return null;
  }

  return (
    <div
      className={className}
      style={{
        position: "relative",
        overflow: "hidden",
        width: "100%",
        aspectRatio: aspectRatio ? `${aspectRatio}` : undefined,
      }}
    >
      {placeholder && (
        <img
          src={placeholder}
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
            transition: "opacity 0.3s ease-in-out",
            opacity: loaded ? 0 : 1,
            pointerEvents: "none",
          }}
        />
      )}
      <picture>
        {srcSetAvif && (
          <source type="image/avif" srcSet={srcSetAvif} sizes={sizes} />
        )}
        {srcSet && <source type="image/webp" srcSet={srcSet} sizes={sizes} />}
        {srcSetOriginal && <source srcSet={srcSetOriginal} sizes={sizes} />}
        <img
          src={src}
          alt={alt}
          width={width}
          height={height}
          sizes={sizes}
          loading="lazy"
          decoding="async"
          onLoad={handleLoad}
          style={{
            display: "block",
            width: "100%",
            height: "auto",
            transition: "opacity 0.3s ease-in-out",
            opacity: loaded ? 1 : 0,
          }}
        />
      </picture>
    </div>
  );
};
