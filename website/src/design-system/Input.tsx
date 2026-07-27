import { useId, type ComponentPropsWithoutRef, type ReactNode } from "react";
import {
  FormField,
  controlBaseClasses,
  controlBorderClasses,
} from "./FormField";

interface InputProps extends ComponentPropsWithoutRef<"input"> {
  /** Label rendered above the field. Omit for an unlabeled input. */
  label?: ReactNode;
  /** Error message rendered below the field; also flags it as invalid. */
  error?: string;
}

export function Input({
  label,
  error,
  required,
  id,
  name,
  className = "",
  ...props
}: InputProps) {
  const generatedId = useId();
  const inputId = id ?? name ?? generatedId;

  return (
    <FormField
      htmlFor={inputId}
      label={label}
      required={required}
      error={error}
    >
      <input
        id={inputId}
        name={name}
        required={required}
        aria-invalid={error ? true : undefined}
        className={`${controlBaseClasses} ${controlBorderClasses(!!error)} ${className}`.trim()}
        {...props}
      />
    </FormField>
  );
}
