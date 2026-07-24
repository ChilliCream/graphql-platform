"use client";

import {
  useCallback,
  useEffect,
  useRef,
  useState,
  type MouseEvent,
  type ReactNode,
} from "react";
import { Icon } from "../icons/Icon";

interface PhotoLightboxImage {
  readonly src: string;
  readonly alt: string;
}

interface PhotoLightboxProps {
  readonly images: readonly PhotoLightboxImage[];
  readonly children: ReactNode;
  readonly className?: string;
}

const LIGHTBOX_BUTTON_CLASS =
  "absolute flex h-11 w-11 cursor-pointer items-center justify-center rounded-full bg-white/10 text-white transition-colors hover:bg-white/20";

function isPlainPrimaryClick(event: MouseEvent) {
  return (
    event.button === 0 &&
    !event.metaKey &&
    !event.ctrlKey &&
    !event.shiftKey &&
    !event.altKey
  );
}

export function PhotoLightbox({
  images,
  children,
  className,
}: PhotoLightboxProps) {
  const gridRef = useRef<HTMLUListElement>(null);
  const [index, setIndex] = useState<number | null>(null);
  const current = index === null ? null : (images[index] ?? null);
  const hasMultiple = images.length > 1;

  const close = useCallback(() => setIndex(null), []);
  const go = useCallback(
    (step: number) => {
      setIndex((i) =>
        i === null ? i : (i + step + images.length) % images.length,
      );
    },
    [images.length],
  );

  const handleGridClick = useCallback(
    (event: MouseEvent<HTMLUListElement>) => {
      if (!isPlainPrimaryClick(event) || !(event.target instanceof Element)) {
        return;
      }

      const anchor = event.target.closest<HTMLAnchorElement>(
        "a[data-photo-index]",
      );
      const grid = gridRef.current;
      if (!anchor || !grid?.contains(anchor)) {
        return;
      }

      const nextIndex = Number(anchor.dataset.photoIndex);
      if (!Number.isInteger(nextIndex) || !images[nextIndex]) {
        return;
      }

      event.preventDefault();
      setIndex(nextIndex);
    },
    [images],
  );

  useEffect(() => {
    if (!current) {
      return;
    }
    const onKey = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        close();
      } else if (event.key === "ArrowLeft") {
        go(-1);
      } else if (event.key === "ArrowRight") {
        go(1);
      }
    };
    window.addEventListener("keydown", onKey);
    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    return () => {
      window.removeEventListener("keydown", onKey);
      document.body.style.overflow = previousOverflow;
    };
  }, [current, close, go]);

  return (
    <>
      <ul ref={gridRef} onClick={handleGridClick} className={className}>
        {children}
      </ul>

      {current ? (
        <div
          role="dialog"
          aria-modal="true"
          aria-label="Photo viewer"
          onClick={close}
          className="fixed inset-0 z-[100] flex items-center justify-center p-4"
          style={{ backgroundColor: "rgba(0, 0, 0, 0.92)" }}
        >
          <button
            type="button"
            aria-label="Close"
            onClick={close}
            autoFocus
            className={`${LIGHTBOX_BUTTON_CLASS} top-3 right-3 sm:top-5 sm:right-5`}
          >
            <Icon icon="x" />
          </button>

          {hasMultiple ? (
            <button
              type="button"
              aria-label="Previous photo"
              onClick={(event) => {
                event.stopPropagation();
                go(-1);
              }}
              className={`${LIGHTBOX_BUTTON_CLASS} top-1/2 left-2 -translate-y-1/2 sm:left-5`}
            >
              <Icon icon="chevron-down" size="lg" className="rotate-90" />
            </button>
          ) : null}

          {/* eslint-disable-next-line @next/next/no-img-element */}
          <img
            src={current.src}
            alt={current.alt}
            onClick={(event) => event.stopPropagation()}
            className="max-h-[90vh] max-w-[92vw] rounded-lg object-contain"
          />

          {hasMultiple ? (
            <button
              type="button"
              aria-label="Next photo"
              onClick={(event) => {
                event.stopPropagation();
                go(1);
              }}
              className={`${LIGHTBOX_BUTTON_CLASS} top-1/2 right-2 -translate-y-1/2 sm:right-5`}
            >
              <Icon icon="chevron-down" size="lg" className="-rotate-90" />
            </button>
          ) : null}

          {hasMultiple ? (
            <div className="absolute bottom-4 left-1/2 -translate-x-1/2 rounded-full bg-white/10 px-3 py-1 text-sm text-white">
              {(index ?? 0) + 1} / {images.length}
            </div>
          ) : null}
        </div>
      ) : null}
    </>
  );
}
