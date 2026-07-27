import { NextStepsSection } from "@/src/components/NextStepsSection";

/**
 * The closing call to action: invites the reader to send a short note about
 * their team, with the contact address echoed underneath the buttons.
 */
export function TrainingClosingCta() {
  return (
    <NextStepsSection
      title="Tell us about your team and we will shape the week around it."
      text="Send a short note with the rough team size, current GraphQL level, and a couple of dates that work. We will reply with a concrete proposal, not a form to fill in."
      primaryLink="mailto:contact@chillicream.com?subject=Training"
      primaryLinkText="Email a trainer"
      secondaryLink="#offers"
      secondaryLinkText="See the two offers again"
    />
  );
}
