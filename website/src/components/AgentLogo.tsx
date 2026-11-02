interface Agent {
  readonly name: string;
  readonly slug: string;
}

/**
 * The coding agents shown across the agentic-coding surfaces. Every entry has
 * an official brand SVG in public/agent-logos/<slug>.svg (see the README
 * there), so the mark renders as-is on the dark navy surface.
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
 * One agent brand mark, loaded from /public/agent-logos/<slug>.svg. Brand
 * logos stay as static assets (not inline icons) because they are third-party
 * marks used verbatim, never tinted via currentColor. The mark is decorative
 * (every call site pairs it with the visible agent name), so alt stays empty;
 * if a standalone usage ever appears, give it a real alt at that point.
 */
export function AgentLogo({ agent, className }: AgentLogoProps) {
  return (
    // eslint-disable-next-line @next/next/no-img-element
    <img
      src={`/agent-logos/${agent.slug}.svg`}
      alt=""
      className={["object-contain", className].filter(Boolean).join(" ")}
    />
  );
}
