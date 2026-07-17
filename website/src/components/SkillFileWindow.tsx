import { MockWindowChrome } from "@/src/components/MockWindowChrome";
import { BranchGlyph } from "@/src/icons/BranchGlyph";
import { CheckGlyph } from "@/src/icons/CheckGlyph";

const VIOLET = "#8b8ff0";
const VIOLET_SOFT = "#7c92c6";
const ACCENT = "var(--color-cc-accent)";
const INK_DIM = "var(--color-cc-ink-dim)";

interface SkillToken {
  readonly text: string;
  readonly color: string;
}

interface SkillFileWindowProps {
  /** Skill slug shown after `name:` in the frontmatter (teal). */
  readonly name: string;
  /** The SKILL.md body: the skill's description prose, rendered under the
   *  frontmatter and soft-wrapped. */
  readonly description: string;
  readonly className?: string;
}

/**
 * A reviewed SKILL.md rendered as a compact code-editor window: a violet "MD"
 * badge and SKILL.md title, a `name` frontmatter block, the description as the
 * file body, all over a line gutter, and a "reviewed" footer. Shared by the
 * home page's agent-skills facet and the agentic-coding skills grid so both
 * read as the same artifact.
 */
export function SkillFileWindow({
  name,
  description,
  className,
}: SkillFileWindowProps) {
  const frontmatter: SkillToken[][] = [
    [{ text: "---", color: VIOLET_SOFT }],
    [
      { text: "name", color: VIOLET },
      { text: ": ", color: INK_DIM },
      { text: name, color: ACCENT },
    ],
    [{ text: "---", color: VIOLET_SOFT }],
    [],
  ];

  return (
    <MockWindowChrome
      className={className}
      shadow="none"
      surfaceClassName="bg-cc-card-bg flex h-full flex-col select-none"
      headerClassName="bg-cc-surface/40 flex items-center justify-between gap-2.5 px-3 py-2.5"
      header={{
        variant: "custom",
        content: (
          <span className="inline-flex items-center gap-2">
            <span
              className="inline-flex size-[18px] items-center justify-center rounded-[5px] font-mono text-[0.5rem] font-bold"
              style={{
                background: "rgba(139, 143, 240, 0.14)",
                border: "1px solid rgba(139, 143, 240, 0.4)",
                color: VIOLET,
              }}
            >
              MD
            </span>
            <span className="font-mono text-xs">
              <span className="text-cc-heading">SKILL</span>
              <span className="text-cc-ink-dim">.md</span>
            </span>
          </span>
        ),
      }}
      headerRight={
        <span className="text-cc-nav-label font-mono text-[0.6rem] whitespace-nowrap">
          skills/
        </span>
      }
      footerClassName="bg-cc-surface/40 flex items-center justify-between gap-2.5 px-3 py-1.5"
      footer={
        <>
          <span className="inline-flex items-center gap-3">
            <span className="text-cc-nav-label inline-flex items-center gap-1.5 font-mono text-[0.6rem] whitespace-nowrap">
              <BranchGlyph className="text-cc-ink-dim size-3 shrink-0" />
              main
            </span>
            <span className="text-cc-nav-label font-mono text-[0.6rem]">
              markdown
            </span>
          </span>
          <span className="text-cc-ink-dim inline-flex items-center gap-1.5 font-mono text-[0.6rem] whitespace-nowrap">
            <CheckGlyph className="text-cc-success size-3" />
            reviewed
          </span>
        </>
      }
    >
      <div className="flex flex-1 flex-col py-2">
        {frontmatter.map((tokens, i) => (
          <div key={`skill-line-${i}`} className="flex items-stretch">
            <span className="text-cc-nav-label border-cc-ink-faint w-7 shrink-0 border-r pr-2 text-right font-mono text-[0.6rem] leading-[19px]">
              {i + 1}
            </span>
            <span className="pl-3 font-mono text-[0.7rem] leading-[19px] whitespace-pre">
              {tokens.map((token, j) => (
                <span
                  key={`skill-tok-${i}-${j}`}
                  style={{ color: token.color }}
                >
                  {token.text}
                </span>
              ))}
            </span>
          </div>
        ))}
        <div className="flex flex-1 items-stretch">
          <span className="text-cc-nav-label border-cc-ink-faint w-7 shrink-0 border-r pr-2 text-right font-mono text-[0.6rem] leading-[19px]">
            {frontmatter.length + 1}
          </span>
          <span className="text-cc-ink min-w-0 flex-1 pr-3 pl-3 font-mono text-[0.7rem] leading-[19px] break-words">
            {description}
          </span>
        </div>
      </div>
    </MockWindowChrome>
  );
}
