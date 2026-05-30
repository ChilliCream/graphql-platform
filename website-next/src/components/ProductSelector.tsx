import Link from "next/link";
import { Dropdown } from "../design-system/Dropdown";
import { PRODUCTS } from "@/src/data/products";

type ProductSelectorProps = {
  activeSlug: string;
};

export function ProductSelector({ activeSlug }: ProductSelectorProps) {
  const active = PRODUCTS.find((p) => p.slug === activeSlug) ?? PRODUCTS[0];

  return (
    <Dropdown
      className="w-full"
      panelClassName="p-1"
      trigger={
        <span className="flex flex-col">
          <span className="text-[10px] font-semibold uppercase tracking-wide text-cc-ink-dim">
            Product
          </span>
          <span className="text-sm font-semibold text-cc-ink">
            {active.title}
          </span>
        </span>
      }
    >
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
                    ? "bg-cc-accent/10 text-cc-accent"
                    : "text-cc-ink hover:bg-cc-hover",
                ].join(" ")}
              >
                <div className="text-sm font-medium">{product.title}</div>
                <div
                  className={[
                    "text-xs",
                    isActive ? "text-cc-accent/80" : "text-cc-ink-dim",
                  ].join(" ")}
                >
                  {product.description}
                </div>
              </Link>
            </li>
          );
        })}
      </ul>
    </Dropdown>
  );
}
