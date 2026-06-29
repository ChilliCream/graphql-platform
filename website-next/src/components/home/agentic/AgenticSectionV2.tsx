import Link from "next/link";

import { RevealOnScroll } from "@/src/components/RevealOnScroll";

type TokenColor = "kw" | "key";

interface CodeToken {
  readonly text: string;
  // undefined renders in the base ink color
  readonly color?: TokenColor;
}

interface Pattern {
  readonly label: string;
  readonly lines: readonly (readonly CodeToken[])[];
}

// Restrained GitHub-dark-on-navy: code stays in cc-ink, keywords dim back, and
// teal marks only the one leading attribute or contract identifier per pattern.
const TOKEN_CLASS: Record<TokenColor, string> = {
  kw: "text-cc-ink-dim",
  key: "text-cc-accent",
};

// Token constructors keep the snippet data compact and accurate.
const kw = (text: string): CodeToken => ({ text, color: "kw" });
const key = (text: string): CodeToken => ({ text, color: "key" });
const t = (text: string): CodeToken => ({ text });

// Twelve real platform patterns. Each kind is its own uniform shape; an agent
// working on ChilliCream produces the correct one, the right way, every time.
// Bodies are abbreviated with "..." so the signature and its key token stay
// readable at a tiny size.
const PATTERNS: readonly Pattern[] = [
  {
    label: "Query",
    lines: [
      [t("["), key("QueryType"), t("]")],
      [kw("static partial class "), t("BookQueries")],
      [t("  "), kw("public static "), t("Book GetBook() =>")],
      [t('    new() { Title = "C# in Depth" };')],
    ],
  },
  {
    label: "Mutation",
    lines: [
      [t("["), key("MutationType"), t("]")],
      [kw("static partial class "), t("BookMutations")],
      [t("  "), kw("public static async "), t("Task<Book>")],
      [t("    AddBookAsync("), kw("string "), t("title, ...) { ... }")],
    ],
  },
  {
    label: "DataLoader",
    lines: [
      [t("["), key("DataLoader"), t("]")],
      [kw("public static async")],
      [t("  Task<Dictionary<int, Brand>>")],
      [t("  GetBrandByIdAsync(ids, ...) => ...")],
    ],
  },
  {
    label: "Connection",
    lines: [
      [t("["), key("UseConnection"), t("]")],
      [kw("public static async "), t("Task<")],
      [t("  PageConnection<User>>")],
      [t("  GetUsersAsync(paging, ...) => ...")],
    ],
  },
  {
    label: "Event handler",
    lines: [
      [kw("public class "), t("OrderPlacedHandler")],
      [t("  : "), key("IEventHandler"), t("<OrderPlaced>")],
      [t("  "), kw("public async "), t("ValueTask HandleAsync(")],
      [t("    OrderPlaced message, ...) { ... }")],
    ],
  },
  {
    label: "Field resolver",
    lines: [
      [t("["), key("ObjectType<Product>"), t("]")],
      [kw("static partial class "), t("ProductNode")],
      [t("  "), kw("public static async "), t("Task<Brand>")],
      [t("    GetBrandAsync(["), key("Parent"), t("] p, ...) => ...")],
    ],
  },
  {
    label: "Subscription",
    lines: [
      [t("["), key("SubscriptionType"), t("]")],
      [kw("static partial class "), t("BookSubscriptions")],
      [t("  ["), key("Subscribe"), t("]")],
      [t("  "), kw("public static "), t("Book OnBookAdded(...) => book;")],
    ],
  },
  {
    label: "Node",
    lines: [
      [t("["), key("Node"), t("]")],
      [kw("public class "), t("Product")],
      [t("  "), kw("public static async "), t("Task<Product?>")],
      [t("    GetAsync(int id, ...) => ...")],
    ],
  },
  {
    label: "Request handler",
    lines: [
      [kw("public class "), t("GetProductRequestHandler")],
      [t("  : "), key("IEventRequestHandler"), t("<")],
      [t("    GetProductRequest, GetProductResponse>")],
      [t("  HandleAsync(request, ...) => ...;")],
    ],
  },
  {
    label: "Message record",
    lines: [
      [kw("public sealed record "), t("GetProductRequest")],
      [t("  : "), key("IEventRequest"), t("<GetProductResponse>")],
      [t("  "), kw("public required "), t("Guid ProductId")],
      [t("    { get; init; }")],
    ],
  },
  {
    label: "Filtering + Sorting",
    lines: [
      [t("["), key("QueryType"), t("]")],
      [kw("static partial class "), t("UserQueries")],
      [t("  ["), key("UseFiltering"), t("] ["), key("UseSorting"), t("]")],
      [t("  IQueryable<User> GetUsers() => db.Users;")],
    ],
  },
  {
    label: "Authorization",
    lines: [
      [t("["), key("QueryType"), t("]")],
      [kw("static partial class "), t("UserQueries")],
      [t("  ["), key("Authorize"), t("]")],
      [t("  "), kw("public static "), t("Task<User?> GetMeAsync(...) => ...")],
    ],
  },
];

/** Subtle teal "follows the pattern" mark shown on each catalog tile. */
function FollowsMark() {
  return (
    <svg
      viewBox="0 0 16 16"
      fill="none"
      aria-hidden="true"
      className="text-cc-accent/70 size-3.5 shrink-0"
    >
      <path
        d="M3 8.5 6.5 12 13 4.5"
        stroke="currentColor"
        strokeWidth={1.6}
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

/** One pattern tile: a mono label plus a tiny, accurate C# / Mocha snippet. */
function PatternTile({ pattern }: { readonly pattern: Pattern }) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover flex flex-col rounded-2xl border p-4 transition-colors">
      <div className="flex items-center justify-between gap-2">
        <span className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.16em] uppercase">
          {pattern.label}
        </span>
        <FollowsMark />
      </div>

      <div className="border-cc-ink-faint bg-cc-surface/40 mt-3 overflow-hidden rounded-lg border p-3">
        <code className="block font-mono text-[0.6rem] leading-[1.65]">
          {pattern.lines.map((tokens, lineIndex) => (
            <span key={lineIndex} className="block whitespace-pre">
              {tokens.map((token, tokenIndex) => (
                <span
                  key={tokenIndex}
                  className={
                    token.color ? TOKEN_CLASS[token.color] : "text-cc-ink"
                  }
                >
                  {token.text}
                </span>
              ))}
            </span>
          ))}
        </code>
      </div>
    </div>
  );
}

/**
 * Agentic coding (take 2): a catalog of the dozen distinct platform patterns.
 * Each kind is its own uniform shape, and an agent working on ChilliCream
 * produces the correct one, the right way, every time. Everything is on screen
 * at once: the heading block leads, the twelve-tile pattern grid follows.
 */
export function AgenticSectionV2() {
  return (
    <section className="mx-auto max-w-7xl px-5 pt-16 sm:px-12 sm:pt-24">
      <RevealOnScroll>
        <div className="max-w-3xl">
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.2em] uppercase">
            Agentic coding
          </p>
          <h2 className="font-heading text-cc-heading text-h3 sm:text-h2 mt-5 leading-[1.1] font-semibold text-balance">
            Best practices your agent actually follows.
          </h2>
          <p className="text-cc-ink mt-6 text-base text-pretty sm:text-lg">
            Agents are strong at filling in a known pattern and weak at
            inventing architecture. ChilliCream gives them the pattern, so what
            comes back follows the same best practices every time, not whatever
            the model improvised that day.
          </p>
          <Link
            href="/platform/agentic-coding"
            className="text-cc-accent hover:text-cc-accent-hover mt-6 inline-flex items-center gap-1.5 text-sm font-medium transition-colors"
          >
            Open agentic coding
            <span aria-hidden="true">-&gt;</span>
          </Link>
        </div>

        <div className="mt-12 grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {PATTERNS.map((pattern) => (
            <PatternTile key={pattern.label} pattern={pattern} />
          ))}
        </div>

        <p className="text-cc-ink-dim mt-5 font-mono text-xs">
          A dozen patterns, used the same way every time.
        </p>
      </RevealOnScroll>
    </section>
  );
}
