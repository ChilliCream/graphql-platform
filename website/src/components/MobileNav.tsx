"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { createPortal } from "react-dom";
import { SolidButton } from "@/src/design-system/Button";
import { BarsIcon } from "@/src/icons/Bars";
import { ChilliCream } from "@/src/icons/ChilliCream";
import { XmarkIcon } from "@/src/icons/Xmark";
import { Search } from "./Search";

type Item = { href: string; label: string };

interface MobileNavProps {
  items: Item[];
  demoHref: string;
  nitroHref: string;
}

export function MobileNav({ items, demoHref, nitroHref }: MobileNavProps) {
  const [open, setOpen] = useState(false);

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
    <div className="bg-cc-surface fixed inset-0 z-50 flex flex-col min-[1060px]:hidden">
      <div className="border-cc-white/10 flex h-18 flex-none items-center justify-between border-b px-4">
        <Link
          href="/"
          aria-label="ChilliCream Home"
          onClick={() => setOpen(false)}
          className="text-cc-heading flex h-full items-center"
        >
          <ChilliCream className="h-8 w-8 fill-current" />
        </Link>
        <button
          type="button"
          aria-label="Close navigation menu"
          onClick={() => setOpen(false)}
          className="text-cc-heading flex h-full items-center px-2"
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
              className="border-cc-white/10 text-cc-heading flex h-18 w-full items-center border-b px-6 text-lg font-medium no-underline"
            >
              {item.label}
            </Link>
          </li>
        ))}
        <li className="flex flex-none items-center justify-center gap-6 px-6 py-8">
          <Link
            href={demoHref}
            onClick={() => setOpen(false)}
            className="text-cc-heading text-sm font-medium no-underline"
          >
            Contact Us
          </Link>
          <SolidButton href={nitroHref} className="h-10 py-0">
            Launch
          </SolidButton>
        </li>
      </ol>
    </div>
  ) : null;

  return (
    <>
      <div className="flex items-center gap-2 min-[1060px]:hidden">
        <Search
          ariaLabel="Search"
          className="text-cc-heading flex h-full items-center px-2"
        />
        <button
          type="button"
          aria-label="Open navigation menu"
          aria-expanded={open}
          onClick={() => setOpen(true)}
          className="text-cc-heading flex h-full items-center px-2"
        >
          <BarsIcon className="h-5 w-5 fill-current" />
        </button>
      </div>

      {/* `overlay` is null until the user opens the menu (a client-only
          action), so createPortal never runs during SSG / first render and
          never touches document.body before it exists. */}
      {overlay ? createPortal(overlay, document.body) : null}
    </>
  );
}

export type MobileNavItem = Item;
