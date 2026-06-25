"use client";

import React, {
  FC,
  ReactNode,
  useCallback,
  useEffect,
  useRef,
  useState,
} from "react";
import { createPortal } from "react-dom";
import styled from "styled-components";

type Props = {
  readonly children: ReactNode;
};

type Slide = {
  src: string;
  alt: string;
};

// Renders a responsive, clickable photo grid. Every cell shares a single
// portrait aspect ratio and uses object-fit: cover, so source images with
// differing dimensions still line up cleanly. The children are markdown linked
// images; clicking one opens a full-screen popout with previous/next
// navigation, read from the anchors so the thumbnails stay optimized.
export const PhotoGrid: FC<Props> = ({ children }) => {
  const gridRef = useRef<HTMLDivElement>(null);
  const [slides, setSlides] = useState<Slide[]>([]);
  const [index, setIndex] = useState<number | null>(null);

  const handleGridClick = useCallback((event: React.MouseEvent) => {
    // Let modified clicks (open in new tab, etc.) keep the default behavior.
    if (
      event.metaKey ||
      event.ctrlKey ||
      event.shiftKey ||
      event.altKey ||
      event.button !== 0
    ) {
      return;
    }
    const anchor = (event.target as HTMLElement).closest("a");
    const grid = gridRef.current;
    if (!anchor || !grid || !grid.contains(anchor)) {
      return;
    }
    event.preventDefault();
    const anchors = Array.from(grid.querySelectorAll("a"));
    setSlides(
      anchors.map((a) => ({
        src: a.getAttribute("href") || "",
        alt: a.querySelector("img")?.getAttribute("alt") || "",
      }))
    );
    setIndex(anchors.indexOf(anchor as HTMLAnchorElement));
  }, []);

  const close = useCallback(() => setIndex(null), []);
  const prev = useCallback(
    () =>
      setIndex((i) =>
        i === null ? i : (i - 1 + slides.length) % slides.length
      ),
    [slides.length]
  );
  const next = useCallback(
    () => setIndex((i) => (i === null ? i : (i + 1) % slides.length)),
    [slides.length]
  );

  useEffect(() => {
    if (index === null) {
      return;
    }
    const onKey = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        close();
      } else if (event.key === "ArrowLeft") {
        prev();
      } else if (event.key === "ArrowRight") {
        next();
      }
    };
    window.addEventListener("keydown", onKey);
    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    return () => {
      window.removeEventListener("keydown", onKey);
      document.body.style.overflow = previousOverflow;
    };
  }, [index, close, prev, next]);

  const current = index !== null ? slides[index] : null;
  const hasMultiple = slides.length > 1;

  return (
    <>
      <Grid ref={gridRef} onClick={handleGridClick}>
        {children}
      </Grid>

      {current &&
        typeof document !== "undefined" &&
        createPortal(
          <Overlay
            onClick={close}
            role="dialog"
            aria-modal="true"
            aria-label="Photo viewer"
          >
            <CloseButton type="button" aria-label="Close" onClick={close}>
              {closeIcon}
            </CloseButton>

            {hasMultiple && (
              <NavButton
                type="button"
                aria-label="Previous photo"
                $side="left"
                onClick={(event) => {
                  event.stopPropagation();
                  prev();
                }}
              >
                {chevronLeftIcon}
              </NavButton>
            )}

            <FullImage
              src={current.src}
              alt={current.alt}
              onClick={(event) => event.stopPropagation()}
            />

            {hasMultiple && (
              <NavButton
                type="button"
                aria-label="Next photo"
                $side="right"
                onClick={(event) => {
                  event.stopPropagation();
                  next();
                }}
              >
                {chevronRightIcon}
              </NavButton>
            )}

            {hasMultiple && (
              <Counter>
                {(index ?? 0) + 1} / {slides.length}
              </Counter>
            )}
          </Overlay>,
          document.body
        )}
    </>
  );
};

const closeIcon = (
  <svg
    width="20"
    height="20"
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    strokeWidth="2"
    strokeLinecap="round"
    aria-hidden="true"
  >
    <path d="M6 6l12 12M18 6L6 18" />
  </svg>
);

const chevronLeftIcon = (
  <svg
    width="22"
    height="22"
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    strokeWidth="2"
    strokeLinecap="round"
    strokeLinejoin="round"
    aria-hidden="true"
  >
    <path d="M15 18l-6-6 6-6" />
  </svg>
);

const chevronRightIcon = (
  <svg
    width="22"
    height="22"
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    strokeWidth="2"
    strokeLinecap="round"
    strokeLinejoin="round"
    aria-hidden="true"
  >
    <path d="M9 6l6 6-6 6" />
  </svg>
);

const Grid = styled.div`
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 12px;
  margin: 32px 0;

  p {
    margin: 0;
  }

  a {
    display: block;
    overflow: hidden;
    border-radius: 8px;
    aspect-ratio: 2 / 3;
    cursor: zoom-in;
  }

  picture {
    display: block;
    width: 100%;
    height: 100%;
  }

  img {
    display: block;
    width: 100% !important;
    height: 100% !important;
    object-fit: cover !important;
    transition: transform 0.3s ease;
  }

  a:hover img {
    transform: scale(1.05);
  }

  @media only screen and (min-width: 640px) {
    grid-template-columns: repeat(3, 1fr);
  }

  @media only screen and (min-width: 1024px) {
    grid-template-columns: repeat(4, 1fr);
  }
`;

const Overlay = styled.div`
  position: fixed;
  inset: 0;
  z-index: 100000;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 16px;
  background-color: rgba(0, 0, 0, 0.92);
`;

const FullImage = styled.img`
  max-width: 92vw;
  max-height: 90vh;
  object-fit: contain;
  border-radius: 8px;
`;

const CloseButton = styled.button`
  position: absolute;
  top: 16px;
  right: 16px;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 44px;
  height: 44px;
  padding: 0;
  border: none;
  border-radius: 9999px;
  background-color: rgba(255, 255, 255, 0.12);
  color: #fff;
  cursor: pointer;
  transition: background-color 0.2s ease;

  &:hover {
    background-color: rgba(255, 255, 255, 0.24);
  }
`;

const NavButton = styled(CloseButton)<{ $side: "left" | "right" }>`
  top: 50%;
  right: auto;
  transform: translateY(-50%);
  ${({ $side }) => ($side === "left" ? "left: 12px;" : "right: 12px;")}
`;

const Counter = styled.div`
  position: absolute;
  bottom: 16px;
  left: 50%;
  transform: translateX(-50%);
  padding: 4px 12px;
  border-radius: 9999px;
  background-color: rgba(255, 255, 255, 0.12);
  color: #fff;
  font-size: 14px;
`;
