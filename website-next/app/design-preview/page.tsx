/**
 * Dev-only reference page: renders the full design mockup SVG as the page body
 * so it can be compared 1:1 against the live home page. Visit /design-preview.
 */
export default function DesignPreviewPage() {
  return (
    <main className="bg-cc-bg min-h-screen">
      {/* eslint-disable-next-line @next/next/no-img-element */}
      <img
        src="/website-redesign.svg"
        alt="Website redesign reference mockup"
        className="mx-auto block w-full max-w-[1360px]"
      />
    </main>
  );
}
