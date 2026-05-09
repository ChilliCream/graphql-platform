// Four IDE / chat clients the Nitro MCP server slots into. Order matches the
// Section 08 row left-to-right. Letters drive the monogram tile in
// `WorksWhereYouWork.tsx`, keeping the visual vocabulary aligned with
// OtelLogoStrip and EnterpriseHero.

export interface IdeClient {
  readonly key: string;
  readonly letter: string;
  readonly name: string;
  readonly setup: string;
}

export const IDE_CLIENTS: readonly IdeClient[] = [
  {
    key: "cursor",
    letter: "C",
    name: "Cursor",
    setup: "/products/nitro/agents/cursor",
  },
  {
    key: "claude-code",
    letter: "K",
    name: "Claude Code",
    setup: "/products/nitro/agents/claude-code",
  },
  {
    key: "copilot-chat",
    letter: "P",
    name: "Copilot Chat",
    setup: "/products/nitro/agents/copilot-chat",
  },
  {
    key: "github-copilot",
    letter: "G",
    name: "GitHub Copilot",
    setup: "/products/nitro/agents/github-copilot",
  },
];
