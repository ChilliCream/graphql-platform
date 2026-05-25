import { SupportPlan } from "@/types/support";
import { useCallback, useState } from "react";

export interface SupportFormData {
  name: string;
  email: string;
  company: string;
  message: string;
  supportPlan: SupportPlan;
}

export interface SupportFormErrors {
  name?: string;
  email?: string;
  company?: string;
  message?: string;
  supportPlan?: string;
}

interface UseFormSubmissionProps {
  onSuccess?: () => void;
  onError?: (error: Error) => void;
}

interface UseFormSubmissionReturn {
  isSubmitting: boolean;
  submitForm: (data: SupportFormData) => Promise<void>;
}

/**
 * Custom hook for handling form submission
 */
export function useFormSubmission({
  onSuccess,
  onError,
}: UseFormSubmissionProps = {}): UseFormSubmissionReturn {
  const [isSubmitting, setIsSubmitting] = useState(false);

  const submitForm = useCallback(
    async (data: SupportFormData): Promise<void> => {
      setIsSubmitting(true);

      try {
        const submissionData = {
          Name: data.name,
          Email: data.email,
          Company: data.company,
          SupportPlan: data.supportPlan,
          Message: data.message,
        };

        const response = await fetch(
          "https://forms.chillicream.com/api/SupportForm",
          {
            method: "POST",
            headers: {
              "Content-Type": "application/json",
            },
            body: JSON.stringify(submissionData),
          }
        );

        if (!response.ok) {
          throw new Error(`HTTP error! status: ${response.status}`);
        }

        if (onSuccess) {
          onSuccess();
        } else {
          window.location.href = "/services/support/thank-you";
        }
      } catch (error) {
        const errorObj =
          error instanceof Error ? error : new Error("Unknown error occurred");

        if (onError) {
          onError(errorObj);
        } else {
          console.error("Form submission error:", errorObj);
          alert(
            "There was an error submitting your request. Please try again."
          );
        }
      } finally {
        setIsSubmitting(false);
      }
    },
    [onSuccess, onError]
  );

  return {
    isSubmitting,
    submitForm,
  };
}
