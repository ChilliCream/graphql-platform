import type { ComponentPropsWithoutRef } from "react";

export function Table({
  className = "",
  ...props
}: ComponentPropsWithoutRef<"table">) {
  return (
    <div className="my-6 overflow-x-auto rounded-lg ring-1 ring-violet-200">
      <table
        className={`w-full border-collapse text-left text-sm ${className}`.trim()}
        {...props}
      />
    </div>
  );
}

export function TableHead({
  className = "",
  ...props
}: ComponentPropsWithoutRef<"thead">) {
  return (
    <thead
      className={`bg-violet-600 text-white ${className}`.trim()}
      {...props}
    />
  );
}

export function TableBody({
  className = "",
  ...props
}: ComponentPropsWithoutRef<"tbody">) {
  return (
    <tbody
      className={`divide-y divide-violet-100 [&>tr:nth-child(even)]:bg-violet-50 ${className}`.trim()}
      {...props}
    />
  );
}

export function TableRow(props: ComponentPropsWithoutRef<"tr">) {
  return <tr {...props} />;
}

export function TableHeaderCell({
  className = "",
  ...props
}: ComponentPropsWithoutRef<"th">) {
  return (
    <th
      className={`px-4 py-2 font-semibold uppercase tracking-wide ${className}`.trim()}
      {...props}
    />
  );
}

export function TableCell({
  className = "",
  ...props
}: ComponentPropsWithoutRef<"td">) {
  return (
    <td className={`px-4 py-2 text-stone-800 ${className}`.trim()} {...props} />
  );
}
