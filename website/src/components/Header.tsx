import { SolidButton } from "@/src/design-system/Button";
import { Picture } from "@/src/design-system/Picture";
import Link from "next/link";

import { getLatestBlogPost } from "@/src/helpers/blogPosts";
import { ChilliCreamText } from "@/src/icons/ChilliCreamText";
import { ChilliCreamWinking } from "@/src/icons/ChilliCreamWinking";

import { GitHubStarButton } from "./header/GitHubStarButton";
import { HeaderNav } from "./header/HeaderNav";
import { CONTACT_HREF, MOBILE_ITEMS, TOOLS } from "./header/navData";
import { HeaderShell } from "./HeaderShell";
import { MobileNav } from "./MobileNav";
import { Search } from "./Search";

export default async function Header() {
  const latestBlog = getLatestBlogPost();
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
    <HeaderShell>
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
          <GitHubStarButton />
          <Link
            href={CONTACT_HREF}
            prefetch={false}
            className="text-cc-heading text-sm font-medium no-underline"
          >
            Contact Us
          </Link>
          <SolidButton href={TOOLS.nitro} className="h-10 py-0">
            Launch Nitro
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
    </HeaderShell>
  );
}
