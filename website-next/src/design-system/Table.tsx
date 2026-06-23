import type { ComponentPropsWithoutRef } from "react";

interface TableProps extends ComponentPropsWithoutRef<"table"> {
  /**
   * Tint every other body row to make wide tables easier to scan.
   * Defaults to `false`. MDX-rendered tables enable it by default.
   */
  alternating?: boolean;
}

export function Table({
  className = "",
  alternating = false,
  ...props
}: TableProps) {
  const striping = alternating
    ? "[&_tbody_tr:nth-child(even)]:bg-cc-card-bg"
    : "";

  return (
    <div className="my-6 overflow-x-auto">
      <table
        className={`w-full border-collapse text-sm ${striping} ${className}`.trim()}
        {...props}
      />
    </div>
  );
}

type Align = "left" | "center" | "right";

const ALIGN_CLASS: Record<Align, string> = {
  left: "text-left",
  center: "text-center",
  right: "text-right",
};

export function TableHead({
  className = "",
  ...props
}: ComponentPropsWithoutRef<"thead">) {
  return <thead className={`text-cc-ink ${className}`.trim()} {...props} />;
}

export function TableBody(props: ComponentPropsWithoutRef<"tbody">) {
  return <tbody {...props} />;
}

export function TableRow({
  className = "",
  ...props
}: ComponentPropsWithoutRef<"tr">) {
  return (
    <tr
      className={`border-cc-card-border border-b ${className}`.trim()}
      {...props}
    />
  );
}

export function TableHeaderCell({
  className = "",
  align = "left",
  ...props
}: ComponentPropsWithoutRef<"th"> & { align?: Align }) {
  return (
    <th
      className={`text-cc-ink px-4 py-3 font-semibold ${ALIGN_CLASS[align]} ${className}`.trim()}
      {...props}
    />
  );
}

export function TableCell({
  className = "",
  align = "left",
  ...props
}: ComponentPropsWithoutRef<"td"> & { align?: Align }) {
  return (
    <td
      className={`text-cc-ink px-4 py-3 ${ALIGN_CLASS[align]} ${className}`.trim()}
      {...props}
    />
  );
}
