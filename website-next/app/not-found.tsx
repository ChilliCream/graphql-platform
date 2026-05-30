// TODO: Create a proper illustration

import { NotFoundActions } from "@/src/components/NotFoundActions";

export default function NotFound() {
  return (
    <div className="flex min-h-[calc(100vh-72px)] flex-col items-center justify-start px-6 pt-16 pb-16 text-center sm:pt-24">
      <div className="flex aspect-3/2 w-75 max-w-full items-center justify-center rounded-xl border border-dashed border-cc-card-border text-sm font-medium text-cc-ink-dim sm:w-90">
        [illustration here]
      </div>

      <p className="mt-8 font-mono text-sm font-semibold uppercase tracking-[0.3em] text-cc-ink-dim">
        Error 404
      </p>
      <h1 className="mt-3 text-4xl font-bold tracking-tight text-cc-ink sm:text-5xl">
        Well, that&apos;s a spill.
      </h1>
      <p className="mt-4 max-w-md text-base leading-7 text-cc-ink-dim">
        We knocked this page over and the drink went everywhere. Whatever you
        were looking for isn&apos;t here anymore.
      </p>

      <NotFoundActions />
    </div>
  );
}
