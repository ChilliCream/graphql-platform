"use client";

import React, { useEffect, useRef, useState } from "react";
import styled from "styled-components";

export const Topbar: React.FC = () => {
  const [open, setOpen] = useState(false);
  const drawerRef = useRef<HTMLDivElement>(null);
  const startXRef = useRef<number | null>(null);

  useEffect(() => {
    if (!open) return;
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") setOpen(false);
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [open]);

  const onTouchStart = (e: React.TouchEvent) => {
    if (e.touches.length === 1) startXRef.current = e.touches[0].clientX;
  };
  const onTouchEnd = (e: React.TouchEvent) => {
    if (startXRef.current == null) return;
    const endX = e.changedTouches[0]?.clientX;
    if (endX != null && endX - startXRef.current > 60) setOpen(false);
    startXRef.current = null;
  };

  return (
    <>
      <Bar>
        <Brand>
          <Mark />
          <span>ChilliCream</span>
        </Brand>
        <Right>
          <SignIn>Sign in</SignIn>
          <Hamburger
            type="button"
            aria-label="Open menu"
            aria-expanded={open}
            onClick={() => setOpen(true)}
          >
            <svg
              width="24"
              height="24"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="2"
              strokeLinecap="round"
            >
              <line x1="4" y1="7" x2="20" y2="7" />
              <line x1="4" y1="12" x2="20" y2="12" />
              <line x1="4" y1="17" x2="20" y2="17" />
            </svg>
          </Hamburger>
        </Right>
      </Bar>

      <DrawerOverlay
        data-open={open}
        onClick={() => setOpen(false)}
        aria-hidden
      />
      <Drawer
        ref={drawerRef}
        data-open={open}
        aria-hidden={!open}
        onTouchStart={onTouchStart}
        onTouchEnd={onTouchEnd}
      >
        <DrawerHead>
          <DrawerClose
            type="button"
            aria-label="Close menu"
            onClick={() => setOpen(false)}
          >
            ✕
          </DrawerClose>
        </DrawerHead>
        <DrawerList>
          <li>
            <a href="#">Products</a>
          </li>
          <li>
            <a href="#">Platform</a>
          </li>
          <li>
            <a href="#">Docs</a>
          </li>
          <li>
            <a href="#">Blog</a>
          </li>
          <li>
            <a href="#">Pricing</a>
          </li>
        </DrawerList>
      </Drawer>
    </>
  );
};

const Bar = styled.header`
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  z-index: 50;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 12px 16px;
  height: 56px;
  backdrop-filter: blur(18px) saturate(140%);
  -webkit-backdrop-filter: blur(18px) saturate(140%);
  background: linear-gradient(
    180deg,
    rgba(11, 15, 26, 0.7),
    rgba(11, 15, 26, 0)
  );
  font-family: var(--cc-font-mono), monospace;
  font-size: 12px;
  letter-spacing: 0.04em;
  text-transform: uppercase;

  @media (min-width: 700px) {
    left: 50%;
    right: auto;
    transform: translateX(-50%);
    width: 100%;
    max-width: 480px;
  }
`;

const Brand = styled.div`
  display: flex;
  align-items: center;
  gap: 10px;
  font-weight: 600;
  color: var(--cc-ink);
`;

const Mark = styled.span`
  width: 22px;
  height: 22px;
  border-radius: 50%;
  background: linear-gradient(135deg, var(--cc-col-cat), var(--cc-col-shi));
`;

const Right = styled.div`
  display: flex;
  align-items: center;
  gap: 8px;
`;

const SignIn = styled.button`
  border: 1px solid var(--cc-ink-faint);
  color: var(--cc-ink);
  padding: 10px 14px;
  border-radius: 999px;
  background: transparent;
  cursor: pointer;
  font-family: inherit;
  font-size: inherit;
  letter-spacing: inherit;
  text-transform: inherit;
  min-height: 44px;
  display: inline-flex;
  align-items: center;
`;

const Hamburger = styled.button`
  width: 44px;
  height: 44px;
  background: transparent;
  border: 0;
  color: var(--cc-ink);
  display: inline-flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  padding: 0;
  svg {
    display: block;
  }
`;

const DrawerOverlay = styled.div`
  position: fixed;
  inset: 0;
  z-index: 99;
  background: rgba(0, 0, 0, 0.4);
  opacity: 0;
  pointer-events: none;
  transition: opacity 250ms ease-out;

  &[data-open="true"] {
    opacity: 1;
    pointer-events: auto;
  }
  @media (prefers-reduced-motion: reduce) {
    transition: none;
  }
`;

const Drawer = styled.aside`
  position: fixed;
  top: 0;
  right: 0;
  bottom: 0;
  z-index: 100;
  width: 84%;
  max-width: 360px;
  background: #0c1322;
  transform: translateX(100%);
  transition: transform 250ms ease-out;
  display: flex;
  flex-direction: column;

  &[data-open="true"] {
    transform: translateX(0);
  }
  @media (prefers-reduced-motion: reduce) {
    transition: none;
  }
`;

const DrawerHead = styled.div`
  display: flex;
  justify-content: flex-end;
  padding: 12px 16px;
  height: 56px;
  align-items: center;
`;

const DrawerClose = styled.button`
  width: 44px;
  height: 44px;
  background: transparent;
  border: 0;
  color: var(--cc-ink);
  display: inline-flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  padding: 0;
  font-size: 22px;
`;

const DrawerList = styled.ul`
  list-style: none;
  padding: 0;
  margin: 0;
  display: flex;
  flex-direction: column;

  a {
    display: flex;
    align-items: center;
    height: 56px;
    padding: 0 22px;
    color: var(--cc-ink);
    text-decoration: none;
    font-family: var(--cc-font-mono), monospace;
    font-size: 12px;
    letter-spacing: 0.18em;
    text-transform: uppercase;
    border-top: 1px solid var(--cc-ink-faint);
  }
  li:last-child a {
    border-bottom: 1px solid var(--cc-ink-faint);
  }
`;
