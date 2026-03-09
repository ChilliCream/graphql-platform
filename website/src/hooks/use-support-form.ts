import { SupportPlan } from "@/types/support";
import { useCallback, useState } from "react";
import { SupportFormData, SupportFormErrors } from "./use-form-submission";

interface UseSupportFormProps {
  initialPlan?: SupportPlan;
}

interface UseSupportFormReturn {
  formData: SupportFormData;
  errors: SupportFormErrors;
  isValid: boolean;
  updateField: (field: keyof SupportFormData, value: string) => void;
  validateForm: () => boolean;
  resetForm: () => void;
}

/**
 * Custom hook for managing support form state and validation
 */
export function useSupportForm({
  initialPlan = "Startup",
}: UseSupportFormProps = {}): UseSupportFormReturn {
  const [formData, setFormData] = useState<SupportFormData>({
    name: "",
    email: "",
    company: "",
    message: "",
    supportPlan: initialPlan,
  });

  const [errors, setErrors] = useState<SupportFormErrors>({});

  const updateField = useCallback(
    (field: keyof SupportFormData, value: string) => {
      setFormData((prev: SupportFormData) => ({ ...prev, [field]: value }));

      if (errors[field]) {
        setErrors((prev: SupportFormErrors) => {
          const newErrors = { ...prev };
          delete newErrors[field];
          return newErrors;
        });
      }
    },
    [errors]
  );

  const validateForm = useCallback((): boolean => {
    const formErrors = validateSupportForm(formData);
    setErrors(formErrors);
    return !hasFormErrors(formErrors);
  }, [formData]);

  const resetForm = useCallback(() => {
    setFormData({
      name: "",
      email: "",
      company: "",
      message: "",
      supportPlan: initialPlan,
    });
    setErrors({});
  }, [initialPlan]);

  return {
    formData,
    errors,
    isValid: !hasFormErrors(errors),
    updateField,
    validateForm,
    resetForm,
  };
}

const EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export function validateField(
  fieldName: keyof SupportFormData,
  value: string
): string | undefined {
  switch (fieldName) {
    case "name":
      return validateName(value);
    case "email":
      return validateEmail(value);
    case "company":
      return validateCompany(value);
    default:
      return undefined;
  }
}

/**
 * Validates the entire form data
 */
export function validateSupportForm(data: SupportFormData): SupportFormErrors {
  const errors: SupportFormErrors = {};

  const nameError = validateName(data.name);
  if (nameError) {
    errors.name = nameError;
  }

  const emailError = validateEmail(data.email);
  if (emailError) {
    errors.email = emailError;
  }

  const companyError = validateCompany(data.company);
  if (companyError) {
    errors.company = companyError;
  }

  return errors;
}

/**
 * Checks if the form has any validation errors
 */
export function hasFormErrors(errors: SupportFormErrors): boolean {
  return Object.keys(errors).length > 0;
}

function validateName(name: string): string | undefined {
  if (!name.trim()) {
    return "Name is required";
  }
  if (name.trim().length < 2) {
    return "Name must be at least 2 characters";
  }
  return undefined;
}

function validateEmail(email: string): string | undefined {
  if (!email.trim()) {
    return "Email is required";
  }
  if (!EMAIL_REGEX.test(email)) {
    return "Please enter a valid email address";
  }
  return undefined;
}

function validateCompany(company: string): string | undefined {
  if (!company.trim()) {
    return "Company is required";
  }
  if (company.trim().length < 2) {
    return "Company name must be at least 2 characters";
  }
  return undefined;
}
