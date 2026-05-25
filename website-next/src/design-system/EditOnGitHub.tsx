import { GitHubIcon } from "@/src/icons/GitHub";

type EditOnGitHubProps = {
  /** URL to the source file on github.com (web-edit view). */
  href: string;
};

export function EditOnGitHub({ href }: EditOnGitHubProps) {
  return (
    <a
      href={href}
      target="_blank"
      rel="noopener noreferrer"
      className="mt-10 inline-flex items-center gap-2 text-sm font-medium text-slate-600 no-underline transition-colors hover:text-primary-700"
    >
      <GitHubIcon className="h-4 w-4 fill-current" aria-hidden="true" />
      Edit this page on GitHub
    </a>
  );
}
