"use client";

import type {
  InternalDocSearchHit,
  StoredDocSearchHit,
} from "@docsearch/react";
import { useDocSearchKeyboardEvents } from "@docsearch/react/useDocSearchKeyboardEvents";
import Link from "next/link";
import { useRouter } from "next/navigation";
import {
  useCallback,
  useEffect,
  useRef,
  useState,
  type ReactNode,
} from "react";
import { createPortal } from "react-dom";
import { SearchIcon } from "@/src/icons/Search";

// The DocSearch modal (and its ~120 KB of JS plus CSS) is code-split into its
// own chunk and loaded on demand, so it stays out of every page's first load.
//
// It is loaded manually rather than via `next/dynamic` on purpose. On iOS the
// modal focuses its input from a mount effect, and iOS only raises the keyboard
// when that focus runs within the same task as the opening tap. `next/dynamic`
// resolves its import through a promise and renders a fallback tick on the
// modal's first mount, which pushes the focus past that window, so the keyboard
// does not appear the first time it is opened (re-opening "works" only because
// the component is resolved by then and mounts synchronously). By resolving the
// module ourselves during idle and keeping the resolved component, the first
// open renders it synchronously, exactly like the working re-open.
type SearchModalComponent = (typeof import("./SearchModal"))["default"];

let loadedModal: SearchModalComponent | null = null;
let modalImport: Promise<SearchModalComponent> | undefined;

function loadSearchModal(): Promise<SearchModalComponent> {
  modalImport ??= import("./SearchModal").then((module) => {
    loadedModal = module.default;
    return module.default;
  });
  return modalImport;
}

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
  // Seed from the module-level cache so later instances (and re-renders) start
  // with the already-resolved component.
  const [ModalComponent, setModalComponent] =
    useState<SearchModalComponent | null>(() => loadedModal);
  const router = useRouter();
  const searchButtonRef = useRef<HTMLButtonElement>(null);

  // Prefetch and resolve the modal once the page is idle so the first tap can
  // mount it synchronously and focus the input within the gesture's turn (see
  // note above). Runs after first paint, so it does not regress initial load.
  useEffect(() => {
    if (!IS_CONFIGURED || ModalComponent) {
      return;
    }
    const request = window.requestIdleCallback ?? ((cb) => setTimeout(cb, 1));
    const cancel = window.cancelIdleCallback ?? clearTimeout;
    const handle = request(() => {
      loadSearchModal().then((component) => setModalComponent(() => component));
    });
    return () => cancel(handle as number);
  }, [ModalComponent]);

  const onOpen = useCallback(() => {
    if (typeof window !== "undefined") {
      setInitialScrollY(window.scrollY);
    }
    // Mount the modal in this same (gesture) render when it is already loaded so
    // iOS keeps the keyboard up; otherwise load it and mount once it resolves.
    if (loadedModal) {
      setModalComponent(() => loadedModal);
    } else {
      loadSearchModal().then((component) => setModalComponent(() => component));
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
        onPointerEnter={IS_CONFIGURED ? loadSearchModal : undefined}
        onPointerDown={IS_CONFIGURED ? loadSearchModal : undefined}
        onFocus={IS_CONFIGURED ? loadSearchModal : undefined}
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
      {IS_CONFIGURED &&
      open &&
      ModalComponent &&
      typeof document !== "undefined"
        ? createPortal(
            <ModalComponent
              appId={APP_ID!}
              apiKey={API_KEY!}
              indices={[INDEX_NAME!]}
              placeholder="Search docs and blog..."
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
