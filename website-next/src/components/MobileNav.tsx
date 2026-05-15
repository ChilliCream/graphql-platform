"use client";

import Link from "next/link";
import { useState } from "react";
import { BarsIcon } from "@/src/icons/Bars";
import { ChilliCream } from "@/src/icons/ChilliCream";
import { SearchIcon } from "@/src/icons/Search";
import { XmarkIcon } from "@/src/icons/Xmark";

type Item = { href: string; label: string };

interface MobileNavProps {
  items: Item[];
  demoHref: string;
  nitroHref: string;
}

export function MobileNav({ items, demoHref, nitroHref }: MobileNavProps) {
  const [open, setOpen] = useState(false);

  return (
    <>
      <div className="flex items-center gap-2 lg:hidden">
        <button
          type="button"
          aria-label="Search"
          className="flex h-full items-center px-2 text-stone-700 transition-colors hover:text-fuchsia-700"
        >
          <SearchIcon className="h-5 w-5 fill-current" />
        </button>
        <button
          type="button"
          aria-label="Open navigation menu"
          onClick={() => setOpen(true)}
          className="flex h-full items-center px-2 text-stone-700 transition-colors hover:text-fuchsia-700"
        >
          <BarsIcon className="h-5 w-5 fill-current" />
        </button>
      </div>

      {open && (
        <div className="fixed inset-0 z-40 flex flex-col bg-white lg:hidden">
          <div className="flex h-[72px] flex-none items-center justify-between border-b border-stone-200 px-4">
            <Link
              href="/"
              aria-label="ChilliCream Home"
              onClick={() => setOpen(false)}
              className="flex h-full items-center text-stone-900"
            >
              <ChilliCream className="h-8 w-8 fill-current" />
            </Link>
            <button
              type="button"
              aria-label="Close navigation menu"
              onClick={() => setOpen(false)}
              className="flex h-full items-center px-2 text-stone-700"
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
                  className="flex h-[72px] w-full items-center border-b border-stone-200 px-6 text-lg font-medium text-stone-700 hover:text-fuchsia-700"
                >
                  {item.label}
                </Link>
              </li>
            ))}
            <li className="flex flex-none items-center justify-center gap-6 px-6 py-8">
              <a
                href={demoHref}
                onClick={() => setOpen(false)}
                className="text-sm font-medium text-stone-700 hover:text-fuchsia-700"
              >
                Request a Demo
              </a>
              <a
                href={nitroHref}
                target="_blank"
                rel="noopener noreferrer"
                onClick={() => setOpen(false)}
                className="inline-flex h-[38px] items-center rounded-md border-2 border-fuchsia-700 bg-fuchsia-700 px-7 text-sm font-medium text-white hover:bg-fuchsia-800"
              >
                Launch
              </a>
            </li>
          </ol>
        </div>
      )}
    </>
  );
}

export type MobileNavItem = Item;
