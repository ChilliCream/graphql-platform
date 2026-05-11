import type { ComponentPropsWithoutRef } from "react";

export function Table({
  className = "",
  ...props
}: ComponentPropsWithoutRef<"table">) {
  return (
    <div className="my-6 overflow-x-auto rounded-md ring-1 ring-slate-200">
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
      className={`bg-slate-50 text-slate-900 ${className}`.trim()}
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
      className={`divide-y divide-slate-200 ${className}`.trim()}
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
      className={`px-4 py-2 font-semibold ${className}`.trim()}
      {...props}
    />
  );
}

export function TableCell({
  className = "",
  ...props
}: ComponentPropsWithoutRef<"td">) {
  return (
    <td className={`px-4 py-2 text-slate-700 ${className}`.trim()} {...props} />
  );
}
