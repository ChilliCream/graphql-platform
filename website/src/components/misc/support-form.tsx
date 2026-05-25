import { useFormSubmission, useSupportForm } from "@/hooks";
import { FONT_FAMILY_HEADING, THEME_COLORS } from "@/style";
import { SUPPORT_PLANS, SupportPlan } from "@/types/support";
import React, { FC, FormEvent } from "react";
import styled from "styled-components";
import { Button } from "./button";

interface SupportFormProps {
  readonly className?: string;
  readonly initialPlan?: SupportPlan;
}

export const SupportForm: FC<SupportFormProps> = ({
  className,
  initialPlan = "Startup",
}) => {
  const { formData, errors, updateField, validateForm } = useSupportForm({
    initialPlan,
  });

  const { isSubmitting, submitForm } = useFormSubmission();

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();

    if (!validateForm()) {
      return;
    }

    await submitForm(formData);
  };

  return (
    <FormContainer className={className}>
      <FormTitle>Contact Support</FormTitle>
      <FormDescription>
        Fill out the form below and we'll get back to you as soon as possible.
      </FormDescription>

      <Form onSubmit={handleSubmit}>
        <FormGroup>
          <Label htmlFor="name">Name *</Label>
          <Input
            id="name"
            type="text"
            value={formData.name}
            onChange={(e) => updateField("name", e.target.value)}
            $hasError={!!errors.name}
            disabled={isSubmitting}
          />
          {errors.name && <ErrorText>{errors.name}</ErrorText>}
        </FormGroup>

        <FormGroup>
          <Label htmlFor="email">Email *</Label>
          <Input
            id="email"
            type="email"
            value={formData.email}
            onChange={(e) => updateField("email", e.target.value)}
            $hasError={!!errors.email}
            disabled={isSubmitting}
          />
          {errors.email && <ErrorText>{errors.email}</ErrorText>}
        </FormGroup>

        <FormGroup>
          <Label htmlFor="company">Company *</Label>
          <Input
            id="company"
            type="text"
            value={formData.company}
            onChange={(e) => updateField("company", e.target.value)}
            $hasError={!!errors.company}
            disabled={isSubmitting}
          />
          {errors.company && <ErrorText>{errors.company}</ErrorText>}
        </FormGroup>

        <FormGroup>
          <Label htmlFor="supportPlan">Support Plan</Label>
          <Select
            id="supportPlan"
            value={formData.supportPlan}
            onChange={(e) => updateField("supportPlan", e.target.value)}
            disabled={isSubmitting}
          >
            {SUPPORT_PLANS.map((plan) => (
              <option key={plan} value={plan}>
                {plan}
              </option>
            ))}
          </Select>
        </FormGroup>

        <FormGroup>
          <Label htmlFor="message">Message *</Label>
          <TextArea
            id="message"
            rows={5}
            value={formData.message}
            onChange={(e) => updateField("message", e.target.value)}
            $hasError={!!errors.message}
            disabled={isSubmitting}
            placeholder="Please describe how we can help you..."
          />
          {errors.message && <ErrorText>{errors.message}</ErrorText>}
        </FormGroup>

        <SubmitButton type="submit" disabled={isSubmitting}>
          {isSubmitting ? "Sending..." : "Send Message"}
        </SubmitButton>
      </Form>
    </FormContainer>
  );
};

const FormContainer = styled.div`
  display: flex;
  flex-direction: column;
  border: 1px solid ${THEME_COLORS.boxBorder};
  border-radius: var(--box-border-radius);
  padding: 40px;
  backdrop-filter: blur(2px);
  background-image: linear-gradient(
    to right bottom,
    #379dc83d,
    #2b80ad3d,
    #2263903d,
    #1a48743d,
    #112f573d
  );
  max-width: 600px;
  margin: 0 auto;
`;

const FormTitle = styled.h3`
  margin: 0 0 16px 0;
  color: ${THEME_COLORS.heading};
  font-family: ${FONT_FAMILY_HEADING};
`;

const FormDescription = styled.p.attrs({
  className: "text-2",
})`
  margin: 0 0 32px 0;
  color: ${THEME_COLORS.text};
`;

const Form = styled.form`
  display: flex;
  flex-direction: column;
  gap: 24px;
`;

const FormGroup = styled.div`
  display: flex;
  flex-direction: column;
`;

const Label = styled.label`
  margin-bottom: 8px;
  color: ${THEME_COLORS.text};
  font-weight: 500;
  font-size: 0.875rem;
`;

const baseInputStyles = `
  padding: 12px 16px;
  border: 1px solid ${THEME_COLORS.boxBorder};
  border-radius: var(--border-radius);
  background-color: rgba(255, 255, 255, 0.05);
  color: ${THEME_COLORS.text};
  font-size: 1rem;
  transition: border-color 0.2s ease-in-out, background-color 0.2s ease-in-out;

  &:focus {
    outline: none;
    border-color: ${THEME_COLORS.primary};
    background-color: rgba(255, 255, 255, 0.08);
  }

  &:disabled {
    opacity: 0.6;
    cursor: not-allowed;
  }

  &::placeholder {
    color: ${THEME_COLORS.text}80;
  }
`;

const Input = styled.input<{ $hasError?: boolean }>`
  ${baseInputStyles}

  ${({ $hasError }) =>
    $hasError &&
    `
    border-color: #ef4444;

    &:focus {
      border-color: #ef4444;
    }
  `}
`;

const TextArea = styled.textarea<{ $hasError?: boolean }>`
  ${baseInputStyles}
  resize: vertical;
  min-height: 120px;

  ${({ $hasError }) =>
    $hasError &&
    `
    border-color: #ef4444;

    &:focus {
      border-color: #ef4444;
    }
  `}
`;

const Select = styled.select`
  ${baseInputStyles}
  cursor: pointer;
`;

const ErrorText = styled.span`
  margin-top: 4px;
  color: #ef4444;
  font-size: 0.875rem;
`;

const SubmitButton = styled(Button)`
  align-self: flex-start;
  padding: 12px 32px;
  font-size: 1rem;
  font-weight: 600;
  margin-top: 16px;

  &:disabled {
    opacity: 0.6;
    cursor: not-allowed;
  }
`;
