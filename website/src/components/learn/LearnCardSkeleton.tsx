/** Loading placeholder matching LearnCard's geometry, for the /learn Suspense fallback. */
export function LearnCardSkeleton() {
  return (
    <div className="border-cc-card-border bg-cc-card-bg flex h-full flex-col rounded-2xl border p-6">
      <div className="flex items-center justify-between gap-3">
        <span className="bg-cc-hover h-4 w-16 animate-pulse rounded-[5px]" />
        <span className="bg-cc-hover h-4 w-12 animate-pulse rounded-full" />
      </div>
      <span className="bg-cc-hover mt-5 h-5 w-3/4 animate-pulse rounded" />
      <span className="bg-cc-hover mt-3 h-4 w-full animate-pulse rounded" />
      <span className="bg-cc-hover mt-2 h-4 w-5/6 animate-pulse rounded" />
      <div className="border-cc-card-border mt-auto flex items-center justify-between gap-3 border-t pt-4">
        <span className="bg-cc-hover h-7 w-16 animate-pulse rounded-lg" />
        <span className="bg-cc-hover h-4 w-24 animate-pulse rounded" />
      </div>
    </div>
  );
}
