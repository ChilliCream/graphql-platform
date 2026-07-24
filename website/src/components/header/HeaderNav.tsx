"use client";

import type { BlogPostSummary } from "@/src/helpers/blogPosts";
import { formatDate } from "@/src/helpers/formatDate";
import { IconElement } from "@/src/icons/IconElement";
import Link from "next/link";
import { type MouseEvent, type ReactNode, useState } from "react";
import {
  NAV_ITEMS,
  type NavItem,
  type SubGroup,
  type SubLink,
} from "./navData";

type NavigateHandler = (e: MouseEvent<HTMLAnchorElement>) => void;

interface HeaderNavProps {
  latestBlog: BlogPostSummary | null;
  blogImage: ReactNode;
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
  return (
    <nav className="relative hidden h-full flex-1 min-[1060px]:block">
      <ol className="m-0 flex h-full list-none items-stretch p-0">
        {NAV_ITEMS.map((item) =>
          item.groups ? (
            <NavWithSubmenu
              key={item.href}
              item={item}
              latestBlog={latestBlog}
              blogImage={blogImage}
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
        className="text-cc-heading flex items-center px-4 text-sm font-medium no-underline"
      >
        {item.label}
      </Link>
    </li>
  );
}

function NavWithSubmenu({
  item,
  latestBlog,
  blogImage,
}: {
  item: NavItem;
  latestBlog: BlogPostSummary | null;
  blogImage: ReactNode;
}) {
  const [closed, setClosed] = useState(false);

  const handleNavigate: NavigateHandler = (e) => {
    if (navigatesInCurrentTab(e)) {
      setClosed(true);
    }
  };

  return (
    <li
      className="group/nav flex items-stretch"
      onMouseLeave={() => setClosed(false)}
    >
      <Link
        href={item.href}
        prefetch={false}
        onClick={handleNavigate}
        className="text-cc-heading flex items-center gap-1.5 px-4 text-sm font-medium no-underline"
      >
        {item.label}
        <IconElement icon="chevron-down" widthAuto size="sm" />
      </Link>

      <SubmenuPanel
        item={item}
        latestBlog={latestBlog}
        blogImage={blogImage}
        closed={closed}
        onNavigate={handleNavigate}
      />
    </li>
  );
}

function SubmenuPanel({
  item,
  latestBlog,
  blogImage,
  closed,
  onNavigate,
}: {
  item: NavItem;
  latestBlog: BlogPostSummary | null;
  blogImage: ReactNode;
  closed: boolean;
  onNavigate: NavigateHandler;
}) {
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
      className={[
        "pointer-events-none invisible absolute top-full left-1/2 -translate-x-1/2 pt-2 opacity-0 transition-[opacity,visibility] duration-200",
        closed
          ? ""
          : "group-hover/nav:pointer-events-auto group-hover/nav:visible group-hover/nav:opacity-100",
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
  // const Icon = link.icon;
  const { icon } = link;

  return (
    <Link
      href={link.href}
      prefetch={false}
      onClick={onNavigate}
      {...linkProps}
      className="group/link text-cc-ink-dim hover:bg-cc-hover flex items-start gap-3 rounded-md px-2 py-2 no-underline transition-colors"
    >
      {icon && (
        <span className="text-cc-ink-dim group-hover/link:text-cc-ink mt-0.5 flex h-5 w-5 flex-none items-center justify-center transition-colors">
          <SubLinkRowIcon link={link} />
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

function SubLinkRowIcon({ link }: { link: SubLink }) {
  const { icon } = link;
  if (!icon) {
    return null;
  }
  if (typeof icon === "string") {
    return <IconElement icon={icon} />;
  }

  const IconComponent = icon;
  return <IconComponent className="h-4 w-4 fill-current" />;
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
        className="group/blog text-cc-ink flex flex-col gap-2 rounded-md no-underline"
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
