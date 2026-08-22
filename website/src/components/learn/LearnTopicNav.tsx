import { ArrowLink } from "@/src/components/ArrowLink";
import { Tag } from "@/src/design-system/Tag";
import { TOPICS, topicBrowseHref } from "./editorial";

/**
 * Topic subnav under the masthead (learn-editorial.md section 3.1): one pill
 * per taxonomy topic, matching the facet-bar pill styling, plus a visually
 * distinct "Browse all" arrow link into the catalog. Topic pills point at
 * `/learn/browse` with the topic's mapped filters preapplied until
 * `/learn/topics/[topic]` exists.
 */
export function LearnTopicNav() {
  return (
    <nav
      aria-label="Topics"
      className="border-cc-card-border flex items-center gap-3 overflow-x-auto border-b pb-6 whitespace-nowrap"
    >
      {TOPICS.map((topic) => (
        <Tag key={topic.key} href={topicBrowseHref(topic)}>
          {topic.label}
        </Tag>
      ))}
      <ArrowLink href="/learn/browse" className="ml-auto shrink-0">
        Browse all
      </ArrowLink>
    </nav>
  );
}
