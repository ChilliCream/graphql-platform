import { GitHubIcon } from "@/src/icons/GitHub";
import { SlackIcon } from "@/src/icons/Slack";

type DocCommunityProps = {
  editUrl: string;
  slackUrl: string;
};

export function DocCommunity({ editUrl, slackUrl }: DocCommunityProps) {
  return (
    <section className="px-5 pt-8 max-w-[21rem]">
      <p className="text-xs font-semibold uppercase tracking-widest text-slate-400 mb-3">
        Help us improving our content
      </p>
      <ul className="space-y-2 text-sm">
        <li>
          <a
            href={editUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="flex items-center gap-3 text-slate-300 hover:text-white transition-colors"
          >
            <GitHubIcon className="h-5 w-5 fill-current" />
            Edit on GitHub
          </a>
        </li>
        <li>
          <a
            href={slackUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="flex items-center gap-3 text-slate-300 hover:text-white transition-colors"
          >
            <SlackIcon className="h-5 w-5 fill-current" />
            Discuss on Slack
          </a>
        </li>
      </ul>
    </section>
  );
}
