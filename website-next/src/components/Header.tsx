"use client";

import Link from "next/link";
import { useState } from "react";
import { BarsIcon } from "@/src/icons/Bars";
import { ChilliCream } from "@/src/icons/ChilliCream";
import { SearchIcon } from "@/src/icons/Search";
import { XmarkIcon } from "@/src/icons/Xmark";

const NAV_ITEMS: { href: string; label: string }[] = [
  { href: "/platform", label: "Platform" },
  { href: "/services", label: "Services" },
  { href: "/docs", label: "Developers" },
  { href: "/resources", label: "Company" },
  { href: "/pricing", label: "Pricing" },
  { href: "/help", label: "Help" },
];

const NITRO_URL = "https://nitro.chillicream.com";
const DEMO_HREF = "mailto:contact@chillicream.com?subject=Demo";

// TODO: wire Algolia DocSearch — hook into @docsearch/react and replace handleSearch.
function openSearch() {
  if (typeof window !== "undefined") {
    console.warn("Search not yet implemented (Algolia stub).");
  }
}

export default function Header() {
  const [menuOpen, setMenuOpen] = useState(false);

  const closeMenu = () => setMenuOpen(false);
  const openMenu = () => setMenuOpen(true);

  return (
    <header className="sticky top-0 z-30 flex h-[72px] w-full justify-center border-b border-stone-200 bg-white/80 backdrop-blur-md">
      <div className="relative flex h-full w-full max-w-7xl items-center justify-between px-4 lg:justify-center lg:gap-8">
        <Link
          href="/"
          aria-label="ChilliCream Home"
          className="flex h-full items-center text-stone-900 transition-colors hover:text-fuchsia-700"
        >
          <ChilliCream className="h-8 w-8 fill-current" />
        </Link>

        <nav className="hidden flex-1 lg:block">
          <ol className="m-0 flex h-full list-none items-center gap-2 p-0">
            {NAV_ITEMS.map((item) => (
              <li key={item.href} className="flex items-center">
                <Link
                  href={item.href}
                  className="px-4 py-2 text-sm font-medium text-stone-700 no-underline transition-colors hover:text-fuchsia-700"
                >
                  {item.label}
                </Link>
              </li>
            ))}
          </ol>
        </nav>

        <div className="hidden items-center gap-6 lg:flex">
          <a
            href={DEMO_HREF}
            className="text-sm font-medium text-stone-700 no-underline transition-colors hover:text-fuchsia-700"
          >
            Request a Demo
          </a>
          <a
            href={NITRO_URL}
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex h-[38px] items-center rounded-md border-2 border-fuchsia-700 bg-fuchsia-700 px-7 text-sm font-medium text-white no-underline transition-colors hover:bg-fuchsia-800"
          >
            Launch
          </a>
          <button
            type="button"
            aria-label="Search"
            onClick={openSearch}
            className="flex h-full items-center text-stone-700 transition-colors hover:text-fuchsia-700"
          >
            <SearchIcon className="h-5 w-5 fill-current" />
          </button>
        </div>

        <div className="flex items-center gap-2 lg:hidden">
          <button
            type="button"
            aria-label="Search"
            onClick={openSearch}
            className="flex h-full items-center px-2 text-stone-700 transition-colors hover:text-fuchsia-700"
          >
            <SearchIcon className="h-5 w-5 fill-current" />
          </button>
          <button
            type="button"
            aria-label="Open navigation menu"
            onClick={openMenu}
            className="flex h-full items-center px-2 text-stone-700 transition-colors hover:text-fuchsia-700"
          >
            <BarsIcon className="h-5 w-5 fill-current" />
          </button>
        </div>
      </div>

      {menuOpen && (
        <MobileMenu onClose={closeMenu} onSearch={openSearch} />
      )}
    </header>
  );
}

function MobileMenu({
  onClose,
  onSearch,
}: {
  onClose: () => void;
  onSearch: () => void;
}) {
  return (
    <div className="fixed inset-0 z-30 flex flex-col bg-white lg:hidden">
      <div className="flex h-[72px] items-center justify-between border-b border-stone-200 px-4">
        <Link
          href="/"
          aria-label="ChilliCream Home"
          onClick={onClose}
          className="flex h-full items-center text-stone-900 transition-colors hover:text-fuchsia-700"
        >
          <ChilliCream className="h-8 w-8 fill-current" />
        </Link>
        <div className="flex items-center gap-2">
          <button
            type="button"
            aria-label="Search"
            onClick={() => {
              onClose();
              onSearch();
            }}
            className="flex h-full items-center px-2 text-stone-700 transition-colors hover:text-fuchsia-700"
          >
            <SearchIcon className="h-5 w-5 fill-current" />
          </button>
          <button
            type="button"
            aria-label="Close navigation menu"
            onClick={onClose}
            className="flex h-full items-center px-2 text-stone-700 transition-colors hover:text-fuchsia-700"
          >
            <XmarkIcon className="h-5 w-5 fill-current" />
          </button>
        </div>
      </div>
      <ol className="m-0 flex flex-1 flex-col list-none overflow-y-auto p-0">
        {NAV_ITEMS.map((item) => (
          <li key={item.href} className="flex items-center">
            <Link
              href={item.href}
              onClick={onClose}
              className="flex h-[84px] w-full items-center border-b border-stone-200 px-6 text-xl font-medium text-stone-700 no-underline transition-colors hover:text-fuchsia-700"
            >
              {item.label}
            </Link>
          </li>
        ))}
        <li className="flex items-center justify-center gap-6 px-6 py-8">
          <a
            href={DEMO_HREF}
            onClick={onClose}
            className="text-sm font-medium text-stone-700 no-underline transition-colors hover:text-fuchsia-700"
          >
            Request a Demo
          </a>
          <a
            href={NITRO_URL}
            target="_blank"
            rel="noopener noreferrer"
            onClick={onClose}
            className="inline-flex h-[38px] items-center rounded-md border-2 border-fuchsia-700 bg-fuchsia-700 px-7 text-sm font-medium text-white no-underline transition-colors hover:bg-fuchsia-800"
          >
            Launch
          </a>
        </li>
      </ol>
    </div>
  );
}
