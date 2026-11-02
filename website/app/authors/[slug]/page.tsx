import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { BlogTeaserGrid } from "@/src/components/BlogTeaserGrid";
import { PageStructuredData } from "@/src/components/PageStructuredData";
import { Picture } from "@/src/design-system/Picture";
import {
  AUTHOR_PROFILES,
  AUTHORS_PATH,
  type AuthorProfileLink,
  authorPageUrl,
  authorPersonId,
  createAuthorPersonNode,
} from "@/src/data/authors";
import { listBlogPostSummaries } from "@/src/helpers/blogPosts";
import { pageMetadata } from "@/src/helpers/pageMetadata";
import { schemaRef } from "@/src/helpers/structuredData";
import { ArrowRightIcon } from "@/src/icons/ArrowRight";
import { GitHubIcon } from "@/src/icons/GitHub";
import { LinkedInIcon } from "@/src/icons/LinkedIn";
import { XIcon } from "@/src/icons/X";

interface Params {
  readonly slug: string;
}

interface PageProps {
  readonly params: Promise<Params>;
}

export const dynamicParams = false;

export function generateStaticParams(): Params[] {
  return AUTHOR_PROFILES.map(({ slug }) => ({ slug }));
}

function getAuthor(slug: string) {
  return AUTHOR_PROFILES.find((author) => author.slug === slug);
}

const PROFILE_LABELS: Record<AuthorProfileLink["provider"], string> = {
  github: "GitHub",
  linkedin: "LinkedIn",
  x: "X",
};

interface ProfileIconProps {
  readonly provider: AuthorProfileLink["provider"];
}

function ProfileIcon({ provider }: ProfileIconProps) {
  switch (provider) {
    case "github":
      return <GitHubIcon className="size-4 fill-current" aria-hidden="true" />;
    case "linkedin":
      return (
        <LinkedInIcon className="size-4 fill-current" aria-hidden="true" />
      );
    case "x":
      return <XIcon className="size-4 fill-current" aria-hidden="true" />;
  }
}

export async function generateMetadata({
  params,
}: PageProps): Promise<Metadata> {
  const { slug } = await params;
  const author = getAuthor(slug);
  if (!author) {
    return {};
  }

  return pageMetadata({
    title: `${author.name}, ChilliCream author`,
    description: author.bio,
    path: authorPageUrl(author),
  });
}

export default async function AuthorPage({ params }: PageProps) {
  const { slug } = await params;
  const author = getAuthor(slug);
  if (!author) {
    notFound();
  }

  const profilePath = authorPageUrl(author);
  const personId = authorPersonId(author);
  const posts = listBlogPostSummaries().filter(
    (post) => post.authorProfile?.slug === author.slug,
  );
  const person = createAuthorPersonNode(author);

  return (
    <>
      <PageStructuredData
        title={`${author.name}, ChilliCream author`}
        description={author.bio}
        path={profilePath}
        pageType="ProfilePage"
        breadcrumbs={[
          { name: "Home", path: "/" },
          { name: "Authors", path: AUTHORS_PATH },
          { name: author.name },
        ]}
        mainEntity={schemaRef(personId)}
        additionalNodes={[person]}
      />
      <div className="px-5 py-12 sm:px-12 sm:py-20">
        <div className="mx-auto max-w-5xl">
          <Link
            href={AUTHORS_PATH}
            className="text-cc-accent hover:text-cc-accent-hover inline-flex items-center gap-1.5 text-sm font-medium no-underline"
          >
            <ArrowRightIcon className="size-3.5 rotate-180" />
            All authors
          </Link>
          <header className="border-cc-card-border mt-10 flex flex-col gap-6 border-b pb-10 sm:flex-row sm:items-center">
            <Picture
              src={author.imageUrl}
              alt={`${author.name}'s avatar`}
              width={128}
              height={128}
              sizes="128px"
              className="size-32 rounded-full object-cover"
            />
            <div>
              <h1 className="text-cc-heading font-heading text-h1 font-semibold tracking-tight">
                {author.name}
              </h1>
              <p className="text-cc-ink-dim mt-4 max-w-2xl text-base leading-relaxed">
                {author.bio}
              </p>
              <ul
                className="mt-4 flex items-center gap-2"
                aria-label="Profiles"
              >
                {author.profileLinks.map(({ provider, url }) => {
                  const label = PROFILE_LABELS[provider];
                  return (
                    <li key={url}>
                      <a
                        href={url}
                        target="_blank"
                        rel="me noopener noreferrer"
                        aria-label={`${author.name} on ${label}`}
                        title={label}
                        className="border-cc-card-border text-cc-ink-dim hover:border-cc-accent hover:text-cc-accent inline-flex size-9 items-center justify-center rounded-full border no-underline transition-colors"
                      >
                        <ProfileIcon provider={provider} />
                      </a>
                    </li>
                  );
                })}
              </ul>
            </div>
          </header>
          <section className="mt-12">
            <h2 className="text-cc-heading font-heading text-h3 font-semibold">
              Published articles
            </h2>
            <div className="mt-5">
              <BlogTeaserGrid posts={posts} />
            </div>
          </section>
        </div>
      </div>
    </>
  );
}
