import { AGENTS, AgentLogo } from "@/src/components/AgentLogo";
import { ArrowLink } from "@/src/components/ArrowLink";
import { CopyCommand } from "@/src/components/CopyCommand";
import { KeyValueChip } from "@/src/components/KeyValueChip";
import { PageHero } from "@/src/components/PageHero";
import { SectionHeading } from "@/src/components/SectionHeading";
import { Card } from "@/src/design-system/Card";
import { Eyebrow } from "@/src/design-system/Eyebrow";
import { pageMetadata } from "@/src/helpers/pageMetadata";

import { ReviewSection } from "./ReviewSection";

export const metadata = pageMetadata({
  title: "Agentic Coding",
  description:
    "Build with AI on a platform designed for it. Any coding agent installs the same skills, fills the same Hot Chocolate and Mocha patterns, and gets feedback before the merge.",
  path: "/platform/agentic-coding",
  keywords: [
    "agentic coding platform",
    "AI coding agents GraphQL",
    "consistent AI-generated code",
    "agent skills SKILL.md",
    "skillz agent skills installer",
    "chillicream/agent-skills",
    ".NET GraphQL agentic coding",
    "client registry feedback for agents",
    "GraphQL MCP server",
    "Claude Codex Copilot Cursor",
  ],
});

/**
 * Brand spectrum (cyan -> violet -> coral), used at most once on the page to
 * tint a single phrase in the lead. Everything else stays in the calm
 * cream / grey / teal palette, matching the ScrollScenes treatment.
 */
const SPECTRUM =
  "linear-gradient(100deg,#16b9e4 0%,#7c92c6 33%,#b681a9 63%,#f0786a 100%)";

/** Used exactly once: the wider center beam behind the hero. */
const BEAM_SPECTRUM =
  "linear-gradient(180deg, rgba(22,185,228,0) 0%, rgba(22,185,228,0.10) 6%, rgba(124,146,198,0.09) 38%, rgba(240,120,106,0.06) 60%, rgba(240,120,106,0) 78%)";

interface BeamSpec {
  readonly left: string;
  readonly width: string;
  readonly background: string;
}

/** Five thin beams spread across the full viewport width, teal at the edges,
 * cyan inboard, and the one spectrum beam through the center of the hero. */
const BEAMS: readonly BeamSpec[] = [
  {
    left: "8%",
    width: "1.5px",
    background:
      "linear-gradient(180deg, rgba(94,234,212,0) 0%, rgba(94,234,212,0.10) 8%, rgba(94,234,212,0.04) 40%, transparent 75%)",
  },
  {
    left: "24%",
    width: "1.5px",
    background:
      "linear-gradient(180deg, rgba(22,185,228,0) 0%, rgba(22,185,228,0.10) 8%, rgba(22,185,228,0.05) 40%, transparent 75%)",
  },
  {
    left: "50%",
    width: "2.5px",
    background: BEAM_SPECTRUM,
  },
  {
    left: "72%",
    width: "1.5px",
    background:
      "linear-gradient(180deg, rgba(22,185,228,0) 0%, rgba(22,185,228,0.10) 8%, rgba(22,185,228,0.05) 40%, transparent 75%)",
  },
  {
    left: "92%",
    width: "1.5px",
    background:
      "linear-gradient(180deg, rgba(94,234,212,0) 0%, rgba(94,234,212,0.10) 8%, rgba(94,234,212,0.04) 40%, transparent 75%)",
  },
];

// A single white streak falls down one beam at a time: the one animation
// walks the drop through three legs (the 24%, 50%, and 92% beams), so at
// most one drop is ever visible. The drop stays invisible (opacity 0)
// unless the animation runs, so with prefers-reduced-motion the beams
// simply stay still.
const DROP_CSS = `
@media (prefers-reduced-motion: no-preference) {
  .ac-drop { animation: ac-drop-fall 15s linear infinite; }
}
@keyframes ac-drop-fall {
  0% { left: 24%; transform: translateY(-80px); opacity: 0; }
  2% { opacity: 0.45; }
  24% { opacity: 0.45; }
  28% { left: 24%; transform: translateY(1900px); opacity: 0; }
  33% { left: 50%; transform: translateY(-80px); opacity: 0; }
  35% { opacity: 0.45; }
  57% { opacity: 0.45; }
  61% { left: 50%; transform: translateY(1900px); opacity: 0; }
  66% { left: 92%; transform: translateY(-80px); opacity: 0; }
  68% { opacity: 0.45; }
  90% { opacity: 0.45; }
  94% { left: 92%; transform: translateY(1900px); opacity: 0; }
  100% { left: 92%; transform: translateY(1900px); opacity: 0; }
}
`;

/**
 * Full-bleed beamline backdrop: five thin vertical gradients descending from
 * the top of the page, in the v8 beamlines idiom, plus the occasional drop
 * falling down one of the lines. Scrolls with the page (absolute, not
 * fixed). With no positioned ancestor it anchors to the document itself, so
 * the beams spread across the full viewport width (scrollbar excluded, no
 * horizontal overflow) instead of the centered content column. The body
 * paints its own opaque background above negative-z descendants, so the
 * backdrop stays at z-auto and the page content sits in a positioned wrapper
 * that paints after it.
 */
function Backdrop() {
  return (
    <div
      aria-hidden="true"
      className="pointer-events-none absolute top-0 left-0 h-[3000px] w-full overflow-hidden"
    >
      <style>{DROP_CSS}</style>
      {BEAMS.map((beam) => (
        <div
          key={beam.left}
          className="absolute top-0 h-full"
          style={{
            left: beam.left,
            width: beam.width,
            background: beam.background,
          }}
        />
      ))}
      <div
        className="ac-drop absolute top-0 rounded-full opacity-0"
        style={{
          width: "1.5px",
          height: "60px",
          background:
            "linear-gradient(180deg, transparent 0%, rgba(245,241,234,0.9) 100%)",
        }}
      />
    </div>
  );
}

/**
 * HERO: composed from the shared PageHero, with the spectrum-tinted phrase in
 * the teaser and the install command as the only call to action.
 */
function Hero() {
  return (
    <PageHero
      eyebrow="Agentic coding"
      title={
        <>
          Consistently good code,
          <br />
          from any agent.
        </>
      }
      teaser={
        <>
          Agents are strong at filling a known pattern and weak at inventing
          architecture. The platform gives your agent the pattern to fill, your
          conventions as checked-in skills, and feedback it can act on before
          the merge, so what comes back is{" "}
          <span
            className="mx-auto mt-2 block w-fit bg-clip-text text-xl font-semibold text-transparent sm:text-2xl"
            style={{ backgroundImage: SPECTRUM }}
          >
            best-practice code.
          </span>
        </>
      }
    >
      <CopyCommand
        command="dnx skillz add chillicream/agent-skills"
        className="bg-cc-surface/80 mx-auto mt-10 max-w-md text-left backdrop-blur-sm"
      />
      <p className="text-cc-ink-dim mt-4 text-sm">
        One command teaches your agent the platform.
      </p>
    </PageHero>
  );
}

/**
 * AGENT DIRECTORY: every supported agent visible at once. The point is
 * recognition ("it works with mine too"), so the wall stays plain: the real
 * mark, the name, and nothing else. The two integration surfaces are named
 * once in the copy, not repeated per card.
 */
function AgentDirectory() {
  return (
    <section className="py-12">
      <SectionHeading
        align="center"
        eyebrow="Agent directory"
        title="Bring the agent you already use."
        description="Every agent plugs into the platform the same two ways: skills teach it your conventions, MCP lets it call your API. Which agent you run is a preference; the quality of the code that comes back is the same."
      />

      <ul className="mt-10 grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4">
        {AGENTS.map((agent) => (
          <li key={agent.slug}>
            <Card hoverBorder className="flex items-center gap-3 p-4">
              <AgentLogo agent={agent} className="size-7 shrink-0" />
              <span className="text-cc-ink font-mono text-sm">
                {agent.name}
              </span>
            </Card>
          </li>
        ))}
        <li>
          <div className="border-cc-ink-faint flex h-full items-center gap-3 rounded-2xl border border-dashed px-4 py-4">
            <span className="text-cc-ink-dim font-mono text-sm">
              and many more
            </span>
          </div>
        </li>
      </ul>
    </section>
  );
}

/** The current starting set in chillicream/agent-skills, one card each. */
const SKILLS = [
  {
    name: "graphql-schema-design",
    title: "Schema design and review.",
    description:
      "Proposes SDL in design mode and audits schema diffs in review mode, following the team's conventions.",
  },
  {
    name: "prototype-feature",
    title: "Frontend prototype with mock data.",
    description:
      "Builds a clickable, local-only prototype with realistic mock data before any schema or backend work.",
  },
  {
    name: "prototype-to-contract",
    title: "Prototype to backend contract.",
    description:
      "Turns an accepted prototype into colocated GraphQL fragments and a contract for the backend.",
  },
] as const;

/**
 * SKILLS: the concrete content behind "your conventions are checked in". One
 * card per skill in chillicream/agent-skills, each explaining what the agent
 * can do once the skill is installed.
 */
function SkillsSection() {
  return (
    <section id="skills" className="scroll-mt-24 py-12">
      <SectionHeading
        align="center"
        eyebrow="Skills"
        title="Skills give your agent a head start."
        description="Prototype a feature, derive the contract, evolve the schema: on this platform those are workflows an agent can run, not rituals a developer performs. Skills package that working knowledge so any agent starts productive, and your own conventions ship the same way, reviewed like code."
      />

      <div className="mt-10 grid grid-cols-1 gap-5 md:grid-cols-3">
        {SKILLS.map((skill) => (
          <Card
            key={skill.name}
            as="article"
            variant="panel"
            hoverBorder
            className="flex flex-col"
          >
            <Eyebrow size="xs" className="text-[0.58rem]">
              SKILL.md
            </Eyebrow>
            <h3 className="text-cc-accent mt-3 font-mono text-sm break-words">
              {skill.name}
            </h3>
            <p className="text-cc-heading font-heading mt-3 text-base font-semibold">
              {skill.title}
            </p>
            <p className="text-cc-ink-dim mt-2 text-sm/relaxed">
              {skill.description}
            </p>
            {/* Try it: install just this skill. */}
            <div className="mt-auto pt-5">
              <CopyCommand
                size="sm"
                command={`dnx skillz add chillicream/agent-skills --skill ${skill.name}`}
                className="bg-cc-surface"
              />
            </div>
          </Card>
        ))}
      </div>

      <div className="mt-8 flex flex-wrap items-center justify-center gap-x-8 gap-y-3">
        <ArrowLink href="https://github.com/chillicream/agent-skills">
          Browse chillicream/agent-skills
        </ArrowLink>
        <ArrowLink href="/docs/skillz">Read the skillz docs</ArrowLink>
      </div>
    </section>
  );
}

/** The recurring platform shapes an agent fills, one chip per pattern. */
const PATTERN_CHIPS = [
  { label: "Query", attr: "[Query]" },
  { label: "Mutation", attr: "[Mutation]" },
  { label: "DataLoader", attr: "[DataLoader]" },
  { label: "Pagination", attr: "[UseConnection]" },
  { label: "Authorization", attr: "[Authorize]" },
  { label: "Filtering", attr: "[UseFiltering]" },
  { label: "Event handler", attr: "IEventHandler" },
  { label: "Request handler", attr: "IEventRequestHandler" },
] as const;

/**
 * PATTERNS TILE: compact version of the "one pattern per problem" argument.
 * A chip per recurring shape is enough; the review section below shows the
 * payoff.
 */
function PatternsTile() {
  return (
    <Card as="article" variant="panel" hoverBorder className="flex flex-col">
      <Eyebrow size="xs">Patterns</Eyebrow>
      <h2 className="font-heading text-cc-heading text-h5 sm:text-h4 mt-3 leading-tight font-semibold text-balance">
        One pattern per problem.
      </h2>
      <p className="text-cc-ink-dim mt-3 text-sm/relaxed">
        Attributes mark queries, mutations, DataLoaders, and pagination; event
        handlers implement a single interface. The agent fills a known shape
        instead of inventing structure, so two features written weeks apart come
        back looking the same.
      </p>

      <div className="mt-5 grid grid-cols-1 gap-2 sm:grid-cols-2">
        {PATTERN_CHIPS.map((chip) => (
          <KeyValueChip
            key={chip.label}
            label={chip.label}
            value={chip.attr}
            labelTruncate
          />
        ))}
      </div>
    </Card>
  );
}

/** The registry check on an agent edit, shown as the actual exchange, down
 * to the agent acting on the feedback. */
const FEEDBACK_ROWS = [
  { label: "agent patch", value: "remove Product.price" },
  { label: "published ops", value: "ProductCard { id name price }" },
  { label: "feedback", value: "breaking: published clients affected" },
  { label: "agent fix", value: "deprecate Product.price instead" },
] as const;

/**
 * FEEDBACK TILE: the strongly typed stack catches most bad edits at compile
 * time; the client registry catches the rest in CI while the agent can still
 * fix them.
 */
function FeedbackTile() {
  return (
    <Card as="article" variant="panel" hoverBorder className="flex flex-col">
      <Eyebrow size="xs">Feedback</Eyebrow>
      <h2 className="font-heading text-cc-heading text-h5 sm:text-h4 mt-3 leading-tight font-semibold text-balance">
        Feedback before the merge.
      </h2>
      <p className="text-cc-ink-dim mt-3 text-sm/relaxed">
        A schema-first, strongly typed stack turns most bad edits into compile
        errors. <code className="text-cc-accent">nitro</code> checks the rest in
        CI against the client registry, so a risky change comes back as feedback
        while the agent can still fix it.
      </p>

      <div className="mt-5 space-y-2">
        {FEEDBACK_ROWS.map((row) => (
          <KeyValueChip
            key={row.label}
            label={row.label}
            value={row.value}
            valueAs="span"
            justify="start"
            density="cozy"
            labelTracking="normal"
            labelWidth="6rem"
          />
        ))}
      </div>
    </Card>
  );
}

/** The operations an agent can call on the running API, with behavior hints. */
const MCP_TOOLS = [
  { name: "getProduct", hint: "idempotent" },
  { name: "searchOrders", hint: "idempotent" },
  { name: "createReview", hint: "destructive" },
] as const;

/**
 * MCP SECTION: supporting beat, not the headline. The same server that shapes
 * the code agents write also serves them at runtime.
 */
function McpSection() {
  return (
    <section className="py-12">
      <div className="grid grid-cols-1 items-start gap-8 lg:grid-cols-2 lg:gap-12">
        <div>
          <SectionHeading
            eyebrow="At runtime"
            title="Your API is a tool, too."
            description={
              <>
                The same server that shapes the code agents write also serves
                them at runtime.{" "}
                <code className="text-cc-accent">AddMcp()</code> and{" "}
                <code className="text-cc-accent">MapGraphQLMcp()</code> expose
                your operations as MCP tools at{" "}
                <code className="text-cc-accent">/graphql/mcp</code>, so an
                agent can query the running API while it works instead of
                guessing at your data.
              </>
            }
          />
          <ArrowLink
            href="/docs/hotchocolate/build/adapters/mcp"
            className="mt-6"
          >
            Read the MCP adapter docs
          </ArrowLink>
        </div>

        <Card variant="panel">
          <Eyebrow size="xs">Tool exposure</Eyebrow>
          <div className="mt-4 space-y-2">
            {MCP_TOOLS.map((tool) => (
              <KeyValueChip
                key={tool.name}
                label={tool.hint}
                value={tool.name}
                valueAs="span"
                order="value-first"
                labelTracking="normal"
              />
            ))}
          </div>
        </Card>
      </div>
    </section>
  );
}

/**
 * Closing beat: one short paragraph, then the same single command closes the
 * loop. No button row and no checklist; the command is the call to action.
 */
function ClosingCta() {
  return (
    <section className="py-12 text-center">
      <h2 className="font-heading text-cc-heading text-h3 leading-tight font-semibold text-balance">
        Point your agent at the platform.
      </h2>
      <p className="text-cc-ink-dim mx-auto mt-5 max-w-2xl text-base/relaxed">
        The patterns, the feedback, and the checks are already in place;
        whatever agent your team uses writes against them. One command installs
        the helpers, and your conventions ride along the same way.
      </p>
      <CopyCommand
        command="dnx skillz add chillicream/agent-skills"
        className="bg-cc-surface/80 mx-auto mt-10 max-w-md text-left"
      />
    </section>
  );
}

export default function AgenticCodingPage() {
  return (
    <>
      <Backdrop />
      {/* Positioned so it paints above the document-anchored backdrop. */}
      <div className="relative">
        <Hero />
        <AgentDirectory />
        <ReviewSection />
        <SkillsSection />

        {/* The two guardrails behind the consistency claim: a known shape to
            fill and feedback before the merge. Side by side because they
            answer the same question from two directions. */}
        <section id="patterns" className="scroll-mt-24 py-12">
          <div className="grid grid-cols-1 items-stretch gap-5 lg:grid-cols-2">
            <PatternsTile />
            <FeedbackTile />
          </div>
        </section>

        <McpSection />
        <ClosingCta />
      </div>
    </>
  );
}
