import { Dropdown, DropdownItem } from "../design-system/Dropdown";
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
          <span className="text-cc-nav-label text-xs font-semibold tracking-wide uppercase">
            Product
          </span>
          <span className="text-cc-ink text-sm font-semibold">
            {active.title}
          </span>
        </span>
      }
    >
      <ul className="m-0 flex list-none flex-col p-0">
        {PRODUCTS.map((product) => (
          <DropdownItem
            key={product.slug}
            href={`/docs/${product.slug}`}
            active={product.slug === active.slug}
            description={product.description}
          >
            {product.title}
          </DropdownItem>
        ))}
      </ul>
    </Dropdown>
  );
}
