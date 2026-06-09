import type { CSSProperties } from "react";
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
      decoding="async"
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
