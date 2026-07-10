"use client";

import "@docsearch/css";
import { DocSearchModal } from "@docsearch/react";
import type { ComponentProps } from "react";

/**
 * Thin wrapper around DocSearch's modal so it (and its ~120 KB of JS plus the
 * DocSearch CSS) live in a separate chunk. Loaded via `next/dynamic` only when
 * the user actually opens search, keeping it out of every page's first load.
 */
export default function SearchModal(
  props: ComponentProps<typeof DocSearchModal>,
) {
  return <DocSearchModal {...props} />;
}
