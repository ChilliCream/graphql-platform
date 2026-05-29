import Link from "next/link";
import { ChevronDownIcon } from "@/src/icons/ChevronDown";
import { PRODUCTS } from "@/src/data/products";

type ProductSelectorProps = {
  activeSlug: string;
};

export function ProductSelector({ activeSlug }: ProductSelectorProps) {
  const active = PRODUCTS.find((p) => p.slug === activeSlug) ?? PRODUCTS[0];

  return (
    <details className="group relative w-full">
      <summary
        className={[
          "flex w-full cursor-pointer list-none select-none items-center gap-2",
          "rounded-md border border-white/10 bg-[#0c1322] px-3 py-2",
          "text-left text-sm text-slate-200 transition-colors",
          "hover:border-white/20 hover:bg-white/5",
          "focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--cc-accent)]",
          "[&::-webkit-details-marker]:hidden",
        ].join(" ")}
      >
        <span className="flex min-w-0 flex-1 flex-col">
          <span className="text-[10px] font-semibold uppercase tracking-wide text-slate-500">
            Product
          </span>
          <span className="truncate text-sm font-semibold text-slate-100">
            {active.title}
          </span>
        </span>
        <ChevronDownIcon
          aria-hidden="true"
          className="h-3 w-3 flex-none fill-current text-slate-400 transition-transform duration-200 group-open:rotate-180"
        />
      </summary>
      <div className="absolute inset-x-0 top-full z-20 mt-1 overflow-hidden rounded-md border border-white/10 bg-[#0b0f1a] p-1 shadow-lg shadow-black/40">
        <ul className="m-0 flex list-none flex-col p-0">
          {PRODUCTS.map((product) => {
            const isActive = product.slug === active.slug;
            return (
              <li key={product.slug}>
                <Link
                  href={`/docs/${product.slug}`}
                  aria-current={isActive ? "page" : undefined}
                  className={[
                    "block rounded px-3 py-2 no-underline transition-colors",
                    isActive
                      ? "bg-[var(--cc-accent)]/15 text-[var(--cc-accent)]"
                      : "text-slate-200 hover:bg-white/5 hover:text-white",
                  ].join(" ")}
                >
                  <div className="text-sm font-medium">{product.title}</div>
                  <div
                    className={[
                      "text-xs",
                      isActive ? "text-[var(--cc-accent)]/80" : "text-slate-500",
                    ].join(" ")}
                  >
                    {product.description}
                  </div>
                </Link>
              </li>
            );
          })}
        </ul>
      </div>
    </details>
  );
}
