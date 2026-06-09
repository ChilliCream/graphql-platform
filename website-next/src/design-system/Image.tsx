"use client";

import {
  useEffect,
  useRef,
  useState,
  type ComponentPropsWithoutRef,
  type CSSProperties,
} from "react";
import { BrokenMedia } from "./BrokenMedia";

interface ImageProps extends ComponentPropsWithoutRef<"img"> {
  /** Tiny base64 placeholder shown blurred until the full image loads. */
  blurDataURL?: string;
  blurWidth?: number;
  blurHeight?: number;
}

/**
 * Builds the placeholder background, mirroring next/image: the tiny blur image
 * is stretched across an upscaled viewBox and run through a Gaussian blur, then
 * used as the element background (`cover`) behind the real image until it loads.
 */
function blurBackground(
  blurDataURL: string,
  blurWidth = 8,
  blurHeight = 8
): CSSProperties {
  const svgWidth = blurWidth * 40;
  const svgHeight = blurHeight * 40;
  const svg =
    `%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 ${svgWidth} ${svgHeight}'%3E` +
    `%3Cfilter id='b' color-interpolation-filters='sRGB'%3E` +
    `%3CfeGaussianBlur stdDeviation='20'/%3E` +
    `%3CfeComponentTransfer%3E%3CfeFuncA type='discrete' tableValues='1 1'/%3E%3C/feComponentTransfer%3E` +
    `%3C/filter%3E` +
    `%3Cimage preserveAspectRatio='none' filter='url(%23b)' x='0' y='0' height='100%25' width='100%25' href='${blurDataURL}'/%3E` +
    `%3C/svg%3E`;
  return {
    backgroundImage: `url("data:image/svg+xml;charset=utf-8,${svg}")`,
    backgroundSize: "cover",
    backgroundPosition: "50% 50%",
    backgroundRepeat: "no-repeat",
  };
}

export function Image({
  alt,
  className = "",
  style,
  onError,
  onLoad,
  blurDataURL,
  blurWidth,
  blurHeight,
  ...props
}: ImageProps) {
  const [broken, setBroken] = useState(false);
  const [loaded, setLoaded] = useState(false);
  const ref = useRef<HTMLImageElement>(null);

  useEffect(() => {
    // On a statically exported page the <img> can settle before React hydrates,
    // so its load/error events are missed. Reconcile from the live element.
    const img = ref.current;
    if (!img || !img.complete) {
      return;
    }
    if (img.naturalWidth === 0) {
      setBroken(true);
    } else {
      setLoaded(true);
    }
  }, []);

  if (broken) {
    return <BrokenMedia message="This image couldn't be loaded." />;
  }

  const showBlur = !loaded && !!blurDataURL;
  const mergedStyle: CSSProperties | undefined = showBlur
    ? { ...blurBackground(blurDataURL, blurWidth, blurHeight), ...style }
    : style;

  return (
    // eslint-disable-next-line @next/next/no-img-element
    <img
      ref={ref}
      alt={alt ?? ""}
      className={className}
      style={mergedStyle}
      {...props}
      onLoad={(event) => {
        onLoad?.(event);
        setLoaded(true);
      }}
      onError={(event) => {
        onError?.(event);
        setBroken(true);
      }}
    />
  );
}
