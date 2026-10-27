import type { ReactNode } from "react";

/** Base styling shared by text inputs and textareas. */
export const controlBaseClasses =
  "w-full rounded-md border bg-cc-control-bg px-3 py-2 text-sm focus:outline-hidden focus:ring-2 disabled:opacity-60";

/** Text, border, and focus classes that switch to an error state when invalid. */
export function controlBorderClasses(hasError: boolean) {
  return hasError
    ? "text-cc-danger placeholder:text-cc-danger border-cc-danger focus:border-cc-danger focus:ring-cc-danger/30"
    : "text-cc-ink placeholder:text-cc-ink-dim border-cc-control-border hover:border-cc-card-border-hover focus:border-cc-accent focus:ring-cc-accent/30";
}

interface FormFieldProps {
  /** `id` of the control this label points at. */
  htmlFor: string;
  label?: ReactNode;
  required?: boolean;
  error?: string;
  children: ReactNode;
}

/**
 * Wraps a form control with an optional label (with a required marker) and an
 * error message. Used by `Input` and `TextArea` to keep their chrome in sync.
 */
export function FormField({
  htmlFor,
  label,
  required,
  error,
  children,
}: FormFieldProps) {
  return (
    <div className="flex flex-col gap-1">
      {label && (
        <label htmlFor={htmlFor} className="text-cc-ink text-sm font-medium">
          {label}
          {required && <span className="text-cc-accent ml-1">*</span>}
        </label>
      )}
      {children}
      {error && <span className="text-cc-danger text-sm">{error}</span>}
    </div>
  );
}
