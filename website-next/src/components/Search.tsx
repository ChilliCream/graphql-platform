"use client";

import "@docsearch/css";
import { DocSearchModal, useDocSearchKeyboardEvents } from "@docsearch/react";
import type {
  InternalDocSearchHit,
  StoredDocSearchHit,
} from "@docsearch/react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useCallback, useRef, useState, type ReactNode } from "react";
import { createPortal } from "react-dom";
import { SearchIcon } from "@/src/icons/Search";

const APP_ID = "WQ7ZRCU9RS"; //process.env.NEXT_PUBLIC_ALGOLIA_APP_ID;
const API_KEY = "b40ebfd92eb180185aa52c192e4fbd86"; //process.env.NEXT_PUBLIC_ALGOLIA_API_KEY;
const INDEX_NAME = "chillicream"; //process.env.NEXT_PUBLIC_ALGOLIA_INDEX;
const IS_CONFIGURED = Boolean(APP_ID && API_KEY && INDEX_NAME);

type HitProps = {
  hit: InternalDocSearchHit | StoredDocSearchHit;
  children: ReactNode;
};

export function Search({
  className,
  ariaLabel = "Search",
}: {
  className?: string;
  ariaLabel?: string;
}) {
  const [open, setOpen] = useState(false);
  const [initialScrollY, setInitialScrollY] = useState(0);
  const router = useRouter();
  const searchButtonRef = useRef<HTMLButtonElement>(null);

  const onOpen = useCallback(() => {
    if (typeof window !== "undefined") {
      setInitialScrollY(window.scrollY);
    }
    setOpen(true);
  }, []);

  const onClose = useCallback(() => {
    setOpen(false);
  }, []);

  useDocSearchKeyboardEvents({
    isOpen: open,
    onOpen,
    onClose,
    searchButtonRef,
    isAskAiActive: false,
    onAskAiToggle: () => {},
  });

  function Hit({ hit, children }: HitProps) {
    let to: string;
    try {
      to = new URL(hit.url).pathname + (new URL(hit.url).hash ?? "");
    } catch {
      to = hit.url;
    }
    return <Link href={to}>{children}</Link>;
  }

  return (
    <>
      <button
        ref={searchButtonRef}
        type="button"
        aria-label={ariaLabel}
        onClick={IS_CONFIGURED ? onOpen : undefined}
        aria-disabled={IS_CONFIGURED ? undefined : true}
        title={
          IS_CONFIGURED
            ? undefined
            : "Search is unavailable — Algolia credentials are not configured"
        }
        className={className}
      >
        <SearchIcon className="h-5 w-5 fill-current" aria-hidden="true" />
      </button>
      {IS_CONFIGURED && open && typeof document !== "undefined"
        ? createPortal(
            <DocSearchModal
              appId={APP_ID!}
              apiKey={API_KEY!}
              indices={[INDEX_NAME!]}
              placeholder="Search docs and blog…"
              hitComponent={Hit}
              initialScrollY={initialScrollY}
              onClose={onClose}
              onAskAiToggle={() => {}}
              navigator={{
                navigate({ itemUrl }) {
                  router.push(itemUrl);
                },
              }}
            />,
            document.body,
          )
        : null}
    </>
  );
}
