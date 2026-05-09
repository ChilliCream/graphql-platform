import type { ComponentPropsWithoutRef } from "react";

type ListProps = ComponentPropsWithoutRef<"ul"> & { ordered?: boolean };

export function List({ ordered, className = "", ...props }: ListProps) {
  const styles = ordered
    ? "my-4 ml-6 list-decimal marker:text-orange-500 marker:font-bold space-y-1"
    : "my-4 ml-6 list-disc marker:text-teal-500 marker:text-xl space-y-1";

  if (ordered) {
    return <ol className={`${styles} ${className}`.trim()} {...props} />;
  }
  return <ul className={`${styles} ${className}`.trim()} {...props} />;
}

export function ListItem({
  className = "",
  ...props
}: ComponentPropsWithoutRef<"li">) {
  return (
    <li className={`text-stone-800 ${className}`.trim()} {...props} />
  );
}
