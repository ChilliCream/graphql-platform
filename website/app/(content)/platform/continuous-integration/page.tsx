import { permanentRedirect } from "next/navigation";

export default function ContinuousIntegrationPage() {
  permanentRedirect("/platform/release-safety");
}
