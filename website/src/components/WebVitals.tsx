"use client";

import { useReportWebVitals } from "next/web-vitals";
import { sendAnalyticsEvent } from "@/src/helpers/analytics";

type ReportWebVitalsCallback = Parameters<typeof useReportWebVitals>[0];

const reportWebVitals: ReportWebVitalsCallback = (metric) => {
  const scale = metric.name === "CLS" ? 1000 : 1;
  const parameters = {
    metric_id: metric.id,
    metric_name: metric.name,
    metric_value: Math.round(metric.value * scale),
    metric_delta: Math.round(metric.delta * scale),
    metric_rating: metric.rating,
    navigation_type: metric.navigationType,
    page_path: window.location.pathname,
    non_interaction: true,
  };

  sendAnalyticsEvent("web_vitals", parameters);
};

/** Reports real-user performance only after analytics consent is available. */
export function WebVitals() {
  useReportWebVitals(reportWebVitals);
  return null;
}
