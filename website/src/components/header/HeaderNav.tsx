"use client";

import Link from "next/link";
import {
  type MouseEvent,
  type ReactNode,
  useEffect,
  useId,
  useRef,
  useState,
} from "react";
import { ProductArtworkIcon } from "@/src/components/ProductArtworkIcon";
import { formatDate } from "@/src/helpers/formatDate";
import type { BlogPostSummary } from "@/src/helpers/blogPosts";
import { ChevronDownIcon } from "@/src/icons/ChevronDown";
import {
  NAV_ITEMS,
  type NavItem,
  type SubGroup,
  type SubLink,
} from "./navData";

type NavigateHandler = (e: MouseEvent<HTMLAnchorElement>) => void;

interface HeaderNavProps {
  readonly latestBlog: BlogPostSummary | null;
  readonly blogImage: ReactNode;
}

/**
 * Returns true only for a plain primary-button click that navigates in the
 * current tab. Modifier clicks (new tab/window) and `target="_blank"` links
 * keep the menu open; middle-clicks never fire `onClick` to begin with.
 */
function navigatesInCurrentTab(e: MouseEvent<HTMLAnchorElement>): boolean {
  if (e.currentTarget.getAttribute("target") === "_blank") {
    return false;
  }
  if (e.metaKey || e.ctrlKey || e.shiftKey || e.altKey || e.button !== 0) {
    return false;
  }
  return true;
}

export function HeaderNav({ latestBlog, blogImage }: HeaderNavProps) {
  const [expandedHref, setExpandedHref] = useState<string | null>(null);
  const navRef = useRef<HTMLElement>(null);

  useEffect(() => {
    if (expandedHref === null) {
      return;
    }
    const handleKeyDown = (e: globalThis.KeyboardEvent) => {
      if (e.key !== "Escape") {
        return;
      }
      const button = navRef.current?.querySelector<HTMLButtonElement>(
        'button[aria-expanded="true"]',
      );
      if (button?.parentElement?.contains(document.activeElement)) {
        button.focus();
      }
      setExpandedHref(null);
    };
    document.addEventListener("keydown", handleKeyDown);
    return () => document.removeEventListener("keydown", handleKeyDown);
  }, [expandedHref]);

  return (
    <nav
      ref={navRef}
      className="relative hidden h-full flex-1 min-[1060px]:block"
    >
      <ol className="m-0 flex h-full list-none items-stretch p-0">
        {NAV_ITEMS.map((item) =>
          item.groups ? (
            <NavWithSubmenu
              key={item.href}
              item={item}
              latestBlog={latestBlog}
              blogImage={blogImage}
              expanded={expandedHref === item.href}
              onExpandedChange={(expanded) =>
                setExpandedHref((current) =>
                  expanded ? item.href : current === item.href ? null : current,
                )
              }
            />
          ) : (
            <NavSimple key={item.href} item={item} />
          ),
        )}
      </ol>
    </nav>
  );
}

function NavSimple({ item }: { item: NavItem }) {
  return (
    <li className="flex items-stretch">
      <Link
        href={item.href}
        prefetch={false}
        className="text-cc-heading flex items-center px-4 text-sm font-medium no-underline max-[1200px]:px-2.5"
      >
        {item.label}
      </Link>
    </li>
  );
}

interface NavWithSubmenuProps {
  readonly item: NavItem;
  readonly latestBlog: BlogPostSummary | null;
  readonly blogImage: ReactNode;
  readonly expanded: boolean;
  readonly onExpandedChange: (expanded: boolean) => void;
}

function NavWithSubmenu({
  item,
  latestBlog,
  blogImage,
  expanded,
  onExpandedChange,
}: NavWithSubmenuProps) {
  const panelId = useId();
  const pressRef = useRef<boolean | null>(null);

  const handleNavigate: NavigateHandler = (e) => {
    if (navigatesInCurrentTab(e)) {
      onExpandedChange(false);
    }
  };

  return (
    <li
      className="flex items-stretch"
      onMouseEnter={() => onExpandedChange(true)}
      onMouseLeave={(e) => {
        if (!e.currentTarget.querySelector(":focus-visible")) {
          onExpandedChange(false);
        }
      }}
      onBlur={(e) => {
        if (
          !e.currentTarget.contains(e.relatedTarget) &&
          !e.currentTarget.matches(":hover")
        ) {
          onExpandedChange(false);
        }
      }}
    >
      <Link
        href={item.href}
        prefetch={false}
        onClick={handleNavigate}
        className="text-cc-heading flex items-center pl-4 text-sm font-medium no-underline max-[1200px]:pl-2.5"
      >
        {item.label}
      </Link>
      <button
        type="button"
        aria-label={`Show ${item.label} menu`}
        aria-expanded={expanded}
        aria-controls={panelId}
        // A mouse or pen press follows the hover-open and keeps it open; touch fires a
        // compat mouseenter before the click, so it toggles from the state at pointerdown.
        onPointerDown={(e) => {
          pressRef.current = e.pointerType === "touch" ? !expanded : true;
        }}
        onPointerCancel={() => {
          pressRef.current = null;
        }}
        onPointerLeave={(e) => {
          if (e.pointerType !== "touch") {
            pressRef.current = null;
          }
        }}
        onClick={() => {
          const next = pressRef.current ?? !expanded;
          pressRef.current = null;
          onExpandedChange(next);
        }}
        className="text-cc-heading focus-visible:ring-cc-accent/50 mr-2.5 cursor-pointer self-center rounded-md p-1.5 focus-visible:ring-2 focus-visible:outline-none max-[1200px]:mr-1"
      >
        <ChevronDownIcon className="h-3 w-3 fill-current" />
      </button>

      <SubmenuPanel
        id={panelId}
        item={item}
        latestBlog={latestBlog}
        blogImage={blogImage}
        expanded={expanded}
        onNavigate={handleNavigate}
      />
    </li>
  );
}

interface SubmenuPanelProps {
  readonly id: string;
  readonly item: NavItem;
  readonly latestBlog: BlogPostSummary | null;
  readonly blogImage: ReactNode;
  readonly expanded: boolean;
  readonly onNavigate: NavigateHandler;
}

function SubmenuPanel({
  id,
  item,
  latestBlog,
  blogImage,
  expanded,
  onNavigate,
}: SubmenuPanelProps) {
  const aside =
    item.aside === "blog" && latestBlog ? (
      <LatestBlogPanel
        post={latestBlog}
        image={blogImage}
        onNavigate={onNavigate}
      />
    ) : item.aside === "get-in-touch" ? (
      <GetInTouchPanel />
    ) : null;
  const showAside = aside !== null;

  return (
    <div
      id={id}
      className={[
        "pointer-events-none invisible absolute top-full left-1/2 -translate-x-1/2 pt-2 opacity-0 transition-[opacity,visibility] duration-200 max-[1273px]:left-0 max-[1273px]:translate-x-0",
        // Visibility must not transition on open, or the links are untabbable for a frame.
        expanded
          ? "pointer-events-auto! visible! opacity-100! transition-[opacity]!"
          : "",
      ].join(" ")}
    >
      <div
        className={[
          "border-cc-white/10 bg-cc-surface/95 grid gap-8 rounded-lg border p-6 shadow-2xl backdrop-blur-md",
          showAside ? "grid-cols-[1fr_280px]" : "grid-cols-1",
          item.panelWidth ?? "w-120",
        ].join(" ")}
      >
        <div
          className={
            (item.groups?.length ?? 0) > 1
              ? "grid grid-cols-2 gap-x-8 gap-y-6"
              : "grid grid-cols-1 gap-y-6"
          }
        >
          {item.groups!.map((group) => (
            <SubGroupBlock
              key={group.title}
              group={group}
              onNavigate={onNavigate}
            />
          ))}
        </div>
        {aside}
      </div>
    </div>
  );
}

function SubGroupBlock({
  group,
  onNavigate,
}: {
  group: SubGroup;
  onNavigate: NavigateHandler;
}) {
  return (
    <div>
      <div
        role="heading"
        aria-level={2}
        className="text-cc-ink-dim mb-3 text-xs font-semibold tracking-[0.18em] uppercase"
      >
        {group.title}
      </div>
      <ul className="m-0 flex list-none flex-col gap-1 p-0">
        {group.links.map((link) => (
          <li key={link.href} className="m-0">
            <SubLinkRow link={link} onNavigate={onNavigate} />
          </li>
        ))}
      </ul>
    </div>
  );
}

/**
 * Height of the whole drink sheet in a menu row, i.e. the height of its tallest
 * drink (Strawberry Shake). Every product icon is scaled from it by the same
 * units-per-rem, so the shake stands taller than the cups and the cups wider
 * than the Nitro can, exactly like the start page hero. 1.625rem puts the
 * 59x84 cups at 1.25rem, the height of the square icons in the other groups.
 * The sheet is taller than the icon slot, so it overflows the row's padding
 * evenly above and below while the set stays centred on the title line.
 */
const PRODUCT_ICON_SHEET_REM = 1.625;

/**
 * One menu row. The icon slot is `h-5`, the line-height of the `text-sm` title,
 * and sits at the top of a row aligned with `items-start`, so it shares the
 * title line's centre — no matter how long the description below it runs. A
 * product icon is bottom-aligned inside a sheet-height box, which is itself
 * centred in the slot: the drinks keep their bases on one line and the set as a
 * whole is centred on the title line.
 */
function SubLinkRow({
  link,
  onNavigate,
}: {
  link: SubLink;
  onNavigate: NavigateHandler;
}) {
  const isExternal = link.href.startsWith("http");
  const linkProps = isExternal
    ? { target: "_blank" as const, rel: "noopener noreferrer" as const }
    : {};
  const Icon = link.icon;
  const artwork = link.iconSize;

  return (
    <Link
      href={link.href}
      prefetch={false}
      onClick={onNavigate}
      {...linkProps}
      className="group/link text-cc-ink-dim hover:bg-cc-hover focus-visible:ring-cc-accent/50 flex items-start gap-3 rounded-md px-2 py-2 no-underline transition-colors focus-visible:ring-2 focus-visible:outline-none"
    >
      {Icon && (
        <span className="text-cc-ink-dim group-hover/link:text-cc-ink flex h-5 w-5 flex-none items-center justify-center transition-colors">
          {artwork ? (
            <ProductArtworkIcon
              Icon={Icon}
              artwork={artwork}
              slotHeightRem={PRODUCT_ICON_SHEET_REM}
            />
          ) : (
            <Icon className="h-4 w-4 fill-current" />
          )}
        </span>
      )}
      <div>
        <div className="text-cc-ink text-sm font-medium">{link.label}</div>
        {link.description && (
          <div className="text-cc-ink-dim text-xs font-normal">
            {link.description}
          </div>
        )}
      </div>
    </Link>
  );
}

function LatestBlogPanel({
  post,
  image,
  onNavigate,
}: {
  post: BlogPostSummary;
  image: ReactNode;
  onNavigate: NavigateHandler;
}) {
  return (
    <div className="flex flex-col gap-3">
      <div
        role="heading"
        aria-level={2}
        className="text-cc-ink-dim text-xs font-semibold tracking-[0.18em] uppercase"
      >
        Latest Blog Post
      </div>
      <Link
        href={post.href}
        prefetch={false}
        onClick={onNavigate}
        className="group/blog text-cc-ink focus-visible:ring-cc-accent/50 flex flex-col gap-2 rounded-md no-underline focus-visible:ring-2 focus-visible:outline-none"
      >
        {image && (
          <div className="border-cc-white/10 overflow-hidden rounded-md border">
            {image}
          </div>
        )}
        <div className="text-cc-ink-dim text-xs">{formatDate(post.date)}</div>
        <div className="text-cc-ink group-hover/blog:text-cc-accent text-sm leading-snug font-medium">
          {post.title}
        </div>
      </Link>
    </div>
  );
}

function GetInTouchPanel() {
  return (
    <div className="flex flex-col gap-3">
      <div
        role="heading"
        aria-level={2}
        className="text-cc-ink-dim text-xs font-semibold tracking-[0.18em] uppercase"
      >
        Get in touch
      </div>
      <div className="border-cc-white/10 flex h-45 items-center justify-center rounded-md border bg-(image:--cc-promo-gradient)">
        <div className="text-cc-ink text-center text-sm leading-snug font-medium">
          Your technology journey.
          <br />
          Our expertise.
        </div>
      </div>
      <p className="text-cc-ink-dim text-xs leading-relaxed">
        <span className="text-cc-ink font-semibold">ChilliCream</span> helps you
        unlock your full potential, delivering on its promise to transform your
        business.
      </p>
    </div>
  );
}
