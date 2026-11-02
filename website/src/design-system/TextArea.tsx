import { useId, type ComponentPropsWithoutRef, type ReactNode } from "react";
import {
  FormField,
  controlBaseClasses,
  controlBorderClasses,
} from "./FormField";

interface TextAreaProps extends ComponentPropsWithoutRef<"textarea"> {
  /** Label rendered above the field. Omit for an unlabeled textarea. */
  label?: ReactNode;
  /** Error message rendered below the field; also flags it as invalid. */
  error?: string;
}

export function TextArea({
  label,
  error,
  required,
  id,
  name,
  rows = 5,
  className = "",
  ...props
}: TextAreaProps) {
  const generatedId = useId();
  const textAreaId = id ?? name ?? generatedId;

  return (
    <FormField
      htmlFor={textAreaId}
      label={label}
      required={required}
      error={error}
    >
      <textarea
        id={textAreaId}
        name={name}
        rows={rows}
        required={required}
        aria-invalid={error ? true : undefined}
        className={`${controlBaseClasses} ${controlBorderClasses(!!error)} ${className}`.trim()}
        {...props}
      />
    </FormField>
  );
}
