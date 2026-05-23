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

const APP_ID = process.env.NEXT_PUBLIC_ALGOLIA_APP_ID;
const API_KEY = process.env.NEXT_PUBLIC_ALGOLIA_API_KEY;
const INDEX_NAME = process.env.NEXT_PUBLIC_ALGOLIA_INDEX;
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

  if (!IS_CONFIGURED) {
    return null;
  }

  return (
    <>
      <button
        ref={searchButtonRef}
        type="button"
        aria-label={ariaLabel}
        onClick={onOpen}
        className={className}
      >
        <SearchIcon className="h-5 w-5 fill-current" aria-hidden="true" />
      </button>
      {open && typeof document !== "undefined"
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
