import type { CSSProperties, ReactNode } from "react";

import { lightDark } from "@/src/components/lightDark";
import { MockWindowChrome } from "@/src/components/MockWindowChrome";
import { Eyebrow } from "@/src/design-system/Eyebrow";

const WINDOW_VARS = {
  "--aw-chrome": lightDark("var(--color-cc-code-header)", "#0d1b30"),
  "--aw-surface": lightDark("var(--color-cc-surface)", "#0a1426"),
  "--aw-shadow": `0 24px 70px -30px ${lightDark("rgba(60,48,30,0.4)", "rgba(0,0,0,0.85)")}`,
} as CSSProperties;

interface AppWindowProps {
  readonly title: ReactNode;
  readonly disclosure?: string;
  readonly children: ReactNode;
  readonly footer?: ReactNode;
  readonly className?: string;
}

export function AppWindow({
  title,
  disclosure,
  children,
  footer,
  className = "",
}: AppWindowProps) {
  return (
    <div className="min-w-0" style={WINDOW_VARS}>
      {disclosure !== undefined && (
        <Eyebrow color="ink-dim" size="2xs" className="mb-3">
          {disclosure}
        </Eyebrow>
      )}
      <MockWindowChrome
        header={{
          variant: "custom",
          content: (
            <>
              <span className="flex gap-1.5" aria-hidden>
                <span className="bg-cc-danger/60 h-2.5 w-2.5 rounded-full" />
                <span className="bg-cc-warning/60 h-2.5 w-2.5 rounded-full" />
                <span className="bg-cc-success/60 h-2.5 w-2.5 rounded-full" />
              </span>
              <div className="text-cc-ink-dim ml-2 flex items-center gap-2 font-mono text-[0.72rem]">
                {title}
              </div>
            </>
          ),
        }}
        headerClassName="flex items-center gap-2 bg-(--aw-chrome) px-4 py-2.5"
        footer={footer}
        footerClassName="bg-(--aw-chrome) px-4 py-2.5"
        shadow="none"
        rounded="rounded-xl"
        surfaceClassName={`bg-(--aw-surface) shadow-(--aw-shadow) backdrop-blur-md ${className}`}
      >
        {children}
      </MockWindowChrome>
    </div>
  );
}
