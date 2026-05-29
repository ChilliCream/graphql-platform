"use client";

import "@docsearch/css";
import "./search.css";

import dynamic from "next/dynamic";
import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useState,
  type ComponentPropsWithoutRef,
  type ReactNode,
} from "react";
import { createPortal } from "react-dom";

import { useDocSearchKeyboardEvents } from "@docsearch/react";
import type {
  DocSearchModalProps,
  InternalDocSearchHit,
  StoredDocSearchHit,
} from "@docsearch/react";

import { SearchIcon } from "@/src/icons/Search";

// Public DocSearch search credentials (same as the source site). These are
// search-only keys and are safe to embed in client-side code.
const DOCSEARCH = {
  appId: "WQ7ZRCU9RS",
  apiKey: "b40ebfd92eb180185aa52c192e4fbd86",
  indexName: "chillicream",
  placeholder: "Search...",
} as const;

// The modal pulls in the Algolia client bundle, so load it lazily on first
// open instead of shipping it in the initial chunk. DocSearch is fully
// client-side (Algolia-hosted index), so this is compatible with output:"export".
const DocSearchModal = dynamic(
  () => import("@docsearch/react").then((m) => m.DocSearchModal),
  { ssr: false },
);

interface SearchContextValue {
  open: () => void;
}

const SearchContext = createContext<SearchContextValue | null>(null);

/**
 * Returns the app-wide search controller. Any client component beneath
 * <SearchProvider> can call open() to launch the single shared modal.
 */
export function useSearch(): SearchContextValue {
  const ctx = useContext(SearchContext);
  if (!ctx) {
    throw new Error("useSearch must be used within a <SearchProvider>");
  }
  return ctx;
}

interface HitProps {
  hit: InternalDocSearchHit | StoredDocSearchHit;
  children: ReactNode;
}

// Result links point at the indexed production URLs (chillicream.com). That is
// expected for the static export, so render a plain anchor to the absolute URL.
function resolveHit({ hit, children }: HitProps) {
  return <a href={hit.url}>{children}</a>;
}

/**
 * Owns the single app-wide DocSearch modal: one Cmd/Ctrl+K handler and one
 * modal instance. Render this once near the root so every search entry point
 * (desktop header button, mobile nav button, keyboard shortcut) shares it.
 */
export function SearchProvider({ children }: { children: ReactNode }) {
  const [isOpen, setIsOpen] = useState(false);

  const onOpen = useCallback(() => setIsOpen(true), []);
  const onClose = useCallback(() => setIsOpen(false), []);

  // DocSearch v4 requires the Ask AI hooks even when the feature is unused.
  useDocSearchKeyboardEvents({
    isOpen,
    onOpen,
    onClose,
    isAskAiActive: false,
    onAskAiToggle: () => {},
  });

  const value = useMemo<SearchContextValue>(() => ({ open: onOpen }), [onOpen]);

  const modalProps: DocSearchModalProps = {
    appId: DOCSEARCH.appId,
    apiKey: DOCSEARCH.apiKey,
    indexName: DOCSEARCH.indexName,
    placeholder: DOCSEARCH.placeholder,
    theme: "dark",
    hitComponent: resolveHit,
    initialScrollY: typeof window !== "undefined" ? window.scrollY : 0,
    isAskAiActive: false,
    onAskAiToggle: () => {},
    onClose,
  };

  return (
    <SearchContext.Provider value={value}>
      {children}
      {isOpen && typeof document !== "undefined"
        ? createPortal(<DocSearchModal {...modalProps} />, document.body)
        : null}
    </SearchContext.Provider>
  );
}

/**
 * Magnifying-glass button that opens the shared search modal. Pass `className`
 * to match the surrounding layout so callers keep their existing styling.
 */
export function SearchButton({
  className,
  iconClassName = "h-5 w-5 fill-current",
  ...props
}: ComponentPropsWithoutRef<"button"> & { iconClassName?: string }) {
  const { open } = useSearch();
  return (
    <button
      type="button"
      aria-label="Search"
      onClick={open}
      className={className}
      {...props}
    >
      <SearchIcon className={iconClassName} />
    </button>
  );
}
