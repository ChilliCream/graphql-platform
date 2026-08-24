import type { ReactNode } from "react";

import { MockWindowChrome } from "@/src/components/MockWindowChrome";
import { Eyebrow } from "@/src/design-system/Eyebrow";

interface AppWindowProps {
  readonly title: ReactNode;
  readonly disclosure?: string;
  readonly children: ReactNode;
  readonly footer?: ReactNode;
  readonly className?: string;
}

export function AppWindow({ title, disclosure, children, footer, className = "" }: AppWindowProps) {
  return (
    <div className="min-w-0">
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
              <div className="text-cc-ink-dim ml-2 flex items-center gap-2 font-mono text-[0.72rem]">{title}</div>
            </>
          ),
        }}
        headerClassName="flex items-center gap-2 bg-[#0d1b30] px-4 py-2.5"
        footer={footer}
        footerClassName="bg-[#0d1b30] px-4 py-2.5"
        shadow="none"
        rounded="rounded-xl"
        surfaceClassName={`bg-[#0a1426] shadow-[0_24px_70px_-30px_rgba(0,0,0,0.85)] backdrop-blur-md ${className}`}
      >
        {children}
      </MockWindowChrome>
    </div>
  );
}
