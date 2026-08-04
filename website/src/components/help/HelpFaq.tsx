import { FaqSection } from "@/src/components/FaqSection";

const FAQ = [
  {
    q: "Which option gets me unblocked fastest?",
    a: "For a defined problem with a deadline, Consultancy is the most reliable path. You bring a defined question to a ChilliCream engineer and work through it together. For lighter or open-ended questions, Slack is faster than you might expect because the community is large and active.",
  },
  {
    q: "What response time can I expect on Slack?",
    a: "Slack is best effort. Answers are usually quick during European working hours, but there is no guarantee. If your team has a hard deadline, do not rely on Slack alone. Take the Consultancy route or move to a Support plan with a contractual SLA.",
  },
  {
    q: "When should we move from Consultancy to a Support plan?",
    a: "Consultancy is great for one-off questions and design reviews. A Support plan makes sense once GraphQL is on a critical path in production and you need a named contact, a private Slack channel, and a response time you can write into an internal runbook.",
  },
  {
    q: "How do I escalate something urgent in production?",
    a: "Customers on a Support plan escalate through their dedicated account manager and private Slack channel, following the SLA in their plan. Without a Support plan, the fastest path is to take the Consultancy route and post a clear repro in the public Slack in parallel.",
  },
  {
    q: "Can ChilliCream help us design a schema or migration?",
    a: "Yes. Design reviews, schema audits, and migration planning are common Consultancy topics. For larger engagements, our advisory service wraps that work in a structured engagement with deliverables.",
  },
  {
    q: "Is the community Slack the right place for bug reports?",
    a: "Slack is good for triage and reproductions. Once a bug is confirmed, please file it on GitHub so it gets a tracking issue, a label, and a place to land the fix.",
  },
];

/**
 * The help FAQ, rendered with the shared `FaqSection` disclosure list.
 */
export function HelpFaq() {
  return (
    <FaqSection
      id="faq"
      className="py-16"
      eyebrow="FAQ"
      heading="Answers to common questions."
      items={FAQ.map((item) => ({ question: item.q, answer: item.a }))}
    />
  );
}
