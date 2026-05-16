"use client";

import { FONT_FAMILY_HEADING, THEME_COLORS } from "@/style";
import { CONTACT_SUBJECTS, ContactSubject } from "@/types/support";
import React, { FC, FormEvent, useCallback, useState } from "react";
import styled from "styled-components";
import { Button } from "./button";

interface ContactFormData {
  name: string;
  email: string;
  company: string;
  subject: ContactSubject;
  message: string;
}

interface ContactFormErrors {
  name?: string;
  email?: string;
  company?: string;
  message?: string;
}

interface ContactFormProps {
  readonly initialSubject?: ContactSubject;
}

const EMAIL_REGEX = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export const ContactForm: FC<ContactFormProps> = ({
  initialSubject = "Schedule a Demo",
}) => {
  const [formData, setFormData] = useState<ContactFormData>({
    name: "",
    email: "",
    company: "",
    subject: initialSubject,
    message: "",
  });
  const [errors, setErrors] = useState<ContactFormErrors>({});
  const [isSubmitting, setIsSubmitting] = useState(false);

  const updateField = useCallback(
    <K extends keyof ContactFormData>(field: K, value: ContactFormData[K]) => {
      setFormData((prev) => ({ ...prev, [field]: value }));
      if (errors[field as keyof ContactFormErrors]) {
        setErrors((prev) => {
          const next = { ...prev };
          delete next[field as keyof ContactFormErrors];
          return next;
        });
      }
    },
    [errors]
  );

  const validateForm = useCallback((): boolean => {
    const newErrors: ContactFormErrors = {};

    if (!formData.name.trim() || formData.name.trim().length < 2) {
      newErrors.name = "Name is required";
    }
    if (!formData.email.trim() || !EMAIL_REGEX.test(formData.email)) {
      newErrors.email = "Please enter a valid email address";
    }
    if (!formData.company.trim() || formData.company.trim().length < 2) {
      newErrors.company = "Company is required";
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  }, [formData]);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();

    if (!validateForm()) {
      return;
    }

    setIsSubmitting(true);

    try {
      const response = await fetch(
        "https://forms.chillicream.com/api/SupportForm",
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            Name: formData.name,
            Email: formData.email,
            Company: formData.company,
            SupportPlan: formData.subject,
            Message: formData.message,
          }),
        }
      );

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      if (window.gtag) {
        window.gtag("event", "contact_form_submit", {
          event_label: formData.subject,
          page_path: window.location.pathname,
        });
      }

      window.location.href = "/services/support/thank-you";
    } catch {
      alert("There was an error submitting your request. Please try again.");
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <PageLayout>
      <ValueProps>
        <Title>Contact Us</Title>
        <Subtitle>
          Whether you&apos;re evaluating GraphQL or scaling your API platform,
          we can help you build and ship faster.
        </Subtitle>
        <PropList>
          <PropItem>
            <PropIcon>&#9679;</PropIcon>
            <PropText>Discuss your use case</PropText>
          </PropItem>
          <PropItem>
            <PropIcon>&#9679;</PropIcon>
            <PropText>Schedule a demo</PropText>
          </PropItem>
          <PropItem>
            <PropIcon>&#9679;</PropIcon>
            <PropText>Understand our plans</PropText>
          </PropItem>
          <PropItem>
            <PropIcon>&#9679;</PropIcon>
            <PropText>Get technical guidance</PropText>
          </PropItem>
        </PropList>
      </ValueProps>

      <FormCard>
        <Form onSubmit={handleSubmit}>
          <FormRow>
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
          </FormRow>

          <FormRow>
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
              <Label htmlFor="subject">How can we help?</Label>
              <Select
                id="subject"
                value={formData.subject}
                onChange={(e) =>
                  updateField("subject", e.target.value as ContactSubject)
                }
                disabled={isSubmitting}
              >
                {CONTACT_SUBJECTS.map((subject) => (
                  <option key={subject} value={subject}>
                    {subject}
                  </option>
                ))}
              </Select>
            </FormGroup>
          </FormRow>

          <FormGroup>
            <Label htmlFor="message">Message</Label>
            <TextArea
              id="message"
              rows={5}
              value={formData.message}
              onChange={(e) => updateField("message", e.target.value)}
              $hasError={!!errors.message}
              disabled={isSubmitting}
              placeholder="Tell us about your project or question..."
            />
            {errors.message && <ErrorText>{errors.message}</ErrorText>}
          </FormGroup>

          <SubmitButton type="submit" disabled={isSubmitting}>
            {isSubmitting ? "Sending..." : "Submit"}
          </SubmitButton>
        </Form>
      </FormCard>
    </PageLayout>
  );
};

const PageLayout = styled.div`
  display: flex;
  flex-direction: column;
  gap: 48px;
  box-sizing: border-box;
  width: 100%;
  max-width: 1100px;
  margin: 0 auto;
  padding: 60px 16px;

  @media only screen and (min-width: 992px) {
    flex-direction: row;
    gap: 80px;
    align-items: flex-start;
    padding: 100px 16px;
  }
`;

const ValueProps = styled.div`
  display: flex;
  flex-direction: column;
  flex: 0 0 auto;

  @media only screen and (min-width: 992px) {
    flex: 0 0 360px;
    position: sticky;
    top: 120px;
  }
`;

const Title = styled.h1`
  margin: 0 0 24px 0;
  color: ${THEME_COLORS.heading};
  font-family: ${FONT_FAMILY_HEADING};
  font-size: 2.5rem;
  font-weight: 700;
`;

const Subtitle = styled.p.attrs({
  className: "text-2",
})`
  margin: 0 0 40px 0;
  color: ${THEME_COLORS.text};
  line-height: 1.6;
`;

const PropList = styled.ul`
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 16px;
`;

const PropItem = styled.li`
  display: flex;
  align-items: center;
  gap: 12px;
`;

const PropIcon = styled.span`
  color: ${THEME_COLORS.primary};
  font-size: 0.5rem;
`;

const PropText = styled.span`
  color: ${THEME_COLORS.text};
  font-size: 1rem;
`;

const FormCard = styled.div`
  flex: 1 1 auto;
  box-sizing: border-box;
  border: 1px solid ${THEME_COLORS.boxBorder};
  border-radius: var(--box-border-radius);
  padding: 24px;
  backdrop-filter: blur(2px);
  background-image: linear-gradient(
    to right bottom,
    #379dc83d,
    #2b80ad3d,
    #2263903d,
    #1a48743d,
    #112f573d
  );

  @media only screen and (min-width: 600px) {
    padding: 40px;
  }
`;

const Form = styled.form`
  display: flex;
  flex-direction: column;
  gap: 24px;
`;

const FormRow = styled.div`
  display: flex;
  flex-direction: column;
  gap: 24px;

  @media only screen and (min-width: 600px) {
    flex-direction: row;
    gap: 16px;

    & > * {
      flex: 1;
    }
  }
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

const inputStyles = `
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
  ${inputStyles}
  ${({ $hasError }) =>
    $hasError && `border-color: #ef4444; &:focus { border-color: #ef4444; }`}
`;

const TextArea = styled.textarea<{ $hasError?: boolean }>`
  ${inputStyles}
  resize: vertical;
  min-height: 120px;
  ${({ $hasError }) =>
    $hasError && `border-color: #ef4444; &:focus { border-color: #ef4444; }`}
`;

const Select = styled.select`
  ${inputStyles}
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
  margin-top: 8px;

  &:disabled {
    opacity: 0.6;
    cursor: not-allowed;
  }
`;
