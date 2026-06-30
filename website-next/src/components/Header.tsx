import Link from "next/link";
import { SolidButton } from "@/src/design-system/Button";
import { Picture } from "@/src/design-system/Picture";

import { getLatestBlogPost } from "@/src/helpers/blogPosts";
import { getGitHubStarCount } from "@/src/helpers/githubStars";
import { ChilliCreamText } from "@/src/icons/ChilliCreamText";
import { ChilliCreamWinking } from "@/src/icons/ChilliCreamWinking";
import { GitHubIcon } from "@/src/icons/GitHub";

import { HeaderNav } from "./header/HeaderNav";
import {
  CONTACT_HREF,
  GITHUB_REPO_URL,
  GITHUB_STARGAZERS_URL,
  MOBILE_ITEMS,
  TOOLS,
} from "./header/navData";
import { MobileNav } from "./MobileNav";
import { Search } from "./Search";

export default async function Header() {
  const latestBlog = getLatestBlogPost();
  const starCount = await getGitHubStarCount();
  // The optimized <Picture> is built here (server-only: it reads the image
  // manifest from disk) and handed to the client nav as a ready-made node.
  const blogImage = latestBlog?.featuredImage ? (
    <Picture
      src={latestBlog.featuredImage}
      alt={latestBlog.title}
      width={320}
      height={180}
      className="block h-auto w-full"
    />
  ) : null;

  return (
    <header className="border-cc-white/10 bg-cc-card-bg sticky top-0 z-40 flex h-18 w-full justify-center border-b shadow-[inset_0_1px_0_var(--cc-highlight)] backdrop-blur-[18px] backdrop-saturate-150">
      <div className="relative flex h-full w-full max-w-7xl items-center justify-between px-4 lg:gap-8">
        <Link
          href="/"
          prefetch={false}
          aria-label="ChilliCream Home"
          className="text-cc-heading flex h-full flex-none items-center gap-2.5"
        >
          <ChilliCreamWinking className="h-8 w-8 fill-current" />
          <ChilliCreamText className="h-6 w-auto fill-current min-[1060px]:hidden" />
        </Link>

        <HeaderNav latestBlog={latestBlog} blogImage={blogImage} />

        <div className="hidden flex-none items-center gap-5 min-[1060px]:flex">
          <span className="border-cc-card-border bg-cc-hover text-cc-heading inline-flex items-stretch overflow-hidden rounded-md border text-xs font-medium">
            <a
              href={GITHUB_REPO_URL}
              target="_blank"
              rel="noopener noreferrer"
              className="hover:bg-cc-ink-faint inline-flex items-center gap-1.5 px-2 py-1 no-underline transition-colors"
              aria-label="Star ChilliCream on GitHub"
            >
              <GitHubIcon className="text-cc-heading h-3.5 w-3.5 fill-current" />
              Star
            </a>
            {starCount !== null && (
              <a
                href={GITHUB_STARGAZERS_URL}
                target="_blank"
                rel="noopener noreferrer"
                className="border-cc-card-border text-cc-heading hover:bg-cc-ink-faint inline-flex items-center border-l px-2 py-1 tabular-nums no-underline transition-colors"
                aria-label={`${starCount.toLocaleString("en-US")} stargazers on GitHub`}
              >
                {starCount.toLocaleString("en-US")}
              </a>
            )}
          </span>
          <Link
            href={CONTACT_HREF}
            prefetch={false}
            className="text-cc-heading text-sm font-medium no-underline"
          >
            Contact Us
          </Link>
          <SolidButton href={TOOLS.nitro} className="h-10 py-0">
            Launch
          </SolidButton>
          <Search
            ariaLabel="Search"
            className="text-cc-heading flex h-full cursor-pointer items-center"
          />
        </div>

        <MobileNav
          items={MOBILE_ITEMS}
          demoHref={CONTACT_HREF}
          nitroHref={TOOLS.nitro}
        />
      </div>
    </header>
  );
}
