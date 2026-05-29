"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { createPortal } from "react-dom";
import { BarsIcon } from "@/src/icons/Bars";
import { ChilliCream } from "@/src/icons/ChilliCream";
import { XmarkIcon } from "@/src/icons/Xmark";
import { SearchButton } from "./Search";

type Item = { href: string; label: string };

interface MobileNavProps {
  items: Item[];
  demoHref: string;
  nitroHref: string;
}

export function MobileNav({ items, demoHref, nitroHref }: MobileNavProps) {
  const [open, setOpen] = useState(false);
  const [mounted, setMounted] = useState(false);

  useEffect(() => {
    setMounted(true);
  }, []);

  useEffect(() => {
    if (!open) {
      return;
    }
    const prev = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    return () => {
      document.body.style.overflow = prev;
    };
  }, [open]);

  // Portal the overlay to <body> so it escapes the Header's
  // `backdrop-filter` containing block (which clips `position: fixed`).
  const overlay = open ? (
    <div className="fixed inset-0 z-50 flex flex-col bg-[#0c1322] lg:hidden">
      <div className="flex h-[72px] flex-none items-center justify-between border-b border-white/10 px-4">
        <Link
          href="/"
          aria-label="ChilliCream Home"
          onClick={() => setOpen(false)}
          className="flex h-full items-center text-[var(--cc-ink)]"
        >
          <ChilliCream className="h-8 w-8 fill-current" />
        </Link>
        <button
          type="button"
          aria-label="Close navigation menu"
          onClick={() => setOpen(false)}
          className="flex h-full items-center px-2 text-[var(--cc-ink-dim)] transition-colors hover:text-[var(--cc-ink)]"
        >
          <XmarkIcon className="h-5 w-5 fill-current" />
        </button>
      </div>
      <ol className="m-0 flex flex-1 list-none flex-col overflow-y-auto p-0">
        {items.map((item) => (
          <li key={item.href} className="flex">
            <Link
              href={item.href}
              onClick={() => setOpen(false)}
              className="flex h-[72px] w-full items-center border-b border-white/10 px-6 text-lg font-medium text-[var(--cc-ink)] no-underline transition-colors hover:text-[var(--cc-accent)]"
            >
              {item.label}
            </Link>
          </li>
        ))}
        <li className="flex flex-none items-center justify-center gap-6 px-6 py-8">
          <a
            href={demoHref}
            onClick={() => setOpen(false)}
            className="text-sm font-medium text-[var(--cc-ink-dim)] no-underline transition-colors hover:text-[var(--cc-ink)]"
          >
            Contact Us
          </a>
          <a
            href={nitroHref}
            target="_blank"
            rel="noopener noreferrer"
            onClick={() => setOpen(false)}
            className="inline-flex h-[38px] items-center rounded-full bg-[var(--cc-ink)] px-7 text-sm font-medium text-[#0c1322] no-underline transition-colors hover:bg-white"
          >
            Launch
          </a>
        </li>
      </ol>
    </div>
  ) : null;

  return (
    <>
      <div className="flex items-center gap-2 lg:hidden">
        <SearchButton className="flex h-full items-center px-2 text-[var(--cc-ink-dim)] transition-colors hover:text-[var(--cc-ink)]" />
        <button
          type="button"
          aria-label="Open navigation menu"
          aria-expanded={open}
          onClick={() => setOpen(true)}
          className="flex h-full items-center px-2 text-[var(--cc-ink-dim)] transition-colors hover:text-[var(--cc-ink)]"
        >
          <BarsIcon className="h-5 w-5 fill-current" />
        </button>
      </div>

      {mounted && overlay ? createPortal(overlay, document.body) : overlay}
    </>
  );
}

export type MobileNavItem = Item;
