"use client";

import React, { useEffect, useState } from "react";

export const StickyCta: React.FC = () => {
  const [visible, setVisible] = useState(false);
  const [dismissed, setDismissed] = useState(false);

  useEffect(() => {
    if (typeof sessionStorage !== "undefined") {
      setDismissed(sessionStorage.getItem("cc-sticky-dismissed") === "1");
    }
  }, []);

  useEffect(() => {
    if (dismissed) return;
    let cancelled = false;
    let cleanup: (() => void) | null = null;

    const watch = () => {
      if (cancelled) return;
      const el = document.getElementById("pinch-anchor");
      if (!el) {
        const t = window.setTimeout(watch, 400);
        cleanup = () => clearTimeout(t);
        return;
      }
      const obs = new IntersectionObserver(
        (entries) => {
          entries.forEach((e) => {
            if (!e.isIntersecting && e.boundingClientRect.top < 0) {
              setVisible(true);
            } else {
              setVisible(false);
            }
          });
        },
        { threshold: 0 }
      );
      obs.observe(el);
      cleanup = () => obs.disconnect();
    };

    watch();
    return () => {
      cancelled = true;
      if (cleanup) cleanup();
    };
  }, [dismissed]);

  if (dismissed) return null;

  const dismiss = () => {
    setDismissed(true);
    try {
      sessionStorage.setItem("cc-sticky-dismissed", "1");
    } catch {
      /* noop */
    }
  };

  return (
    <button
      type="button"
      className="cc-sticky-cta"
      data-visible={visible}
    >
      <span
        className="cc-sticky-cta-close"
        onClick={(e) => {
          e.stopPropagation();
          dismiss();
        }}
      >
        ✕
      </span>
      <span className="cc-sticky-cta-text">Start free →</span>
    </button>
  );
};
