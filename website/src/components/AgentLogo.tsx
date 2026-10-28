import { lightDark } from "@/src/components/lightDark";

interface Agent {
  readonly name: string;
  readonly slug: string;
}

/**
 * The coding agents shown across the agentic-coding surfaces. Every entry has
 * an official brand SVG in public/agent-logos/<slug>.svg.
 */
export const AGENTS: readonly Agent[] = [
  { name: "Claude", slug: "claude" },
  { name: "Codex", slug: "codex" },
  { name: "Copilot", slug: "copilot" },
  { name: "Cursor", slug: "cursor" },
  { name: "Windsurf", slug: "windsurf" },
  { name: "Gemini", slug: "gemini" },
  { name: "Cline", slug: "cline" },
];

interface AgentLogoProps {
  readonly agent: Agent;
  readonly className?: string;
}

/**
 * One agent brand mark, loaded from /public/agent-logos/<slug>.svg. The
 * assets are single-color cream marks, so each renders as a mask filled with
 * the page ink of the resolved appearance. The mark is decorative (every call
 * site pairs it with the visible agent name).
 */
export function AgentLogo({ agent, className }: AgentLogoProps) {
  const mask = `url(/agent-logos/${agent.slug}.svg)`;

  return (
    <span
      aria-hidden="true"
      className={["inline-block", className].filter(Boolean).join(" ")}
      style={{
        backgroundColor: lightDark("#0b0f1a", "#f5f0ea"),
        WebkitMaskImage: mask,
        maskImage: mask,
        WebkitMaskRepeat: "no-repeat",
        maskRepeat: "no-repeat",
        WebkitMaskPosition: "center",
        maskPosition: "center",
        WebkitMaskSize: "contain",
        maskSize: "contain",
      }}
    />
  );
}
