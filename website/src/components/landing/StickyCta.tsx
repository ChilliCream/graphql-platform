"use client";

import React, { useEffect, useState } from "react";
import styled from "styled-components";

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
    } catch (e) {
      /* noop */
    }
  };

  return (
    <Pill data-visible={visible} type="button">
      <CloseSpan
        onClick={(e) => {
          e.stopPropagation();
          dismiss();
        }}
      >
        ✕
      </CloseSpan>
      <Text>Start free →</Text>
    </Pill>
  );
};

const Pill = styled.button`
  position: fixed;
  bottom: 16px;
  right: 16px;
  z-index: 60;
  height: 48px;
  padding: 0 18px 0 8px;
  border-radius: 999px;
  background: var(--cc-ink);
  color: #0c1322;
  display: none;
  align-items: center;
  gap: 6px;
  font-family: var(--cc-font-sans), sans-serif;
  font-size: 14px;
  font-weight: 600;
  border: none;
  cursor: pointer;
  box-shadow: 0 12px 30px -8px rgba(0, 0, 0, 0.6);

  &[data-visible="true"] {
    display: inline-flex;
  }
`;

const CloseSpan = styled.span`
  width: 28px;
  height: 28px;
  background: transparent;
  border: 0;
  color: #0c1322;
  font-size: 16px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
  cursor: pointer;
  padding: 0;

  &:hover {
    background: rgba(0, 0, 0, 0.08);
  }
`;

const Text = styled.span`
  padding-right: 6px;
`;
