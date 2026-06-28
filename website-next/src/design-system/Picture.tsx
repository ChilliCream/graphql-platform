import type { CSSProperties } from "react";
import ReactDOM from "react-dom";
import {
  getOptimizedImage,
  type OptimizedVariant,
} from "@/src/image-optimization/manifest";
import { Image } from "./Image";

// Default `sizes` used when a caller does not provide one. Configurable.
const DEFAULT_SIZES = "100vw";

interface PictureProps {
  src?: string;
  alt?: string;
  className?: string;
  width?: number;
  height?: number;
  sizes?: string;
  priority?: boolean;
  style?: CSSProperties;
  title?: string;
}

function srcset(variants: OptimizedVariant[]): string {
  return variants.map((v) => `${v.path} ${v.w}w`).join(", ");
}

export function Picture({
  src,
  alt,
  className,
  width,
  height,
  sizes,
  priority,
  style,
  title,
  ...rest
}: PictureProps & Record<string, unknown>) {
  const opt = src ? getOptimizedImage(src) : null;

  // Preload the LCP candidate during HTML parse. Because images are served by a
  // custom pipeline (next.config `images.unoptimized`), Next.js never emits the
  // `priority` preload that next/image would, so the hero <img> would otherwise
  // be discovered late and race deferred third-party scripts. Preload the AVIF
  // srcset to match the first <picture> <source>; the browser skips it when AVIF
  // is unsupported and falls back to the WebP/original source.
  if (priority && opt?.formats.avif?.length) {
    const avif = opt.formats.avif;
    ReactDOM.preload(avif[avif.length - 1].path, {
      as: "image",
      imageSrcSet: srcset(avif),
      imageSizes: sizes ?? DEFAULT_SIZES,
      type: "image/avif",
      fetchPriority: "high",
    });
  }

  const imgEl = (
    <Image
      src={opt?.fallbackSrc ?? src}
      alt={alt}
      className={className}
      style={style}
      title={title}
      width={width ?? opt?.width}
      height={height ?? opt?.height}
      loading={priority ? "eager" : "lazy"}
      fetchPriority={priority ? "high" : undefined}
      // Priority (eager, above-the-fold) images decode synchronously so they
      // paint atomically with the surrounding content. With async decoding the
      // browser can present a frame before the image is ready, flashing the page
      // background through the transparent <img> on every reload. Lazy images
      // stay async so decoding never blocks scrolling.
      decoding={priority ? "sync" : "async"}
      blurDataURL={opt?.blurDataURL}
      blurWidth={opt?.blurWidth}
      blurHeight={opt?.blurHeight}
      {...rest}
    />
  );

  if (!opt) {
    return imgEl;
  }

  return (
    // `contents` makes <picture> generate no box so the <img> participates in
    // layout as if it were a direct child of the caller's container (preserves
    // `object-cover`, `h-full`, flex/grid sizing on the image).
    <picture className="contents">
      {opt.formats.avif && (
        <source
          type="image/avif"
          srcSet={srcset(opt.formats.avif)}
          sizes={sizes ?? DEFAULT_SIZES}
        />
      )}
      {opt.formats.webp && (
        <source
          type="image/webp"
          srcSet={srcset(opt.formats.webp)}
          sizes={sizes ?? DEFAULT_SIZES}
        />
      )}
      {imgEl}
    </picture>
  );
}
