import Script from "next/script";

const COOKIEBOT_CBID = process.env.NEXT_PUBLIC_COOKIEBOT_CBID;
const GTM_ID = process.env.NEXT_PUBLIC_GTM_ID;

/**
 * Loads the cookie consent manager (Cookiebot) and Google Tag Manager with
 * Google Consent Mode. Renders nothing unless the corresponding environment
 * variables are configured, so local and unconfigured deployments stay free of
 * any third-party requests.
 *
 * - `NEXT_PUBLIC_COOKIEBOT_CBID`: Cookiebot domain group id (enables consent).
 * - `NEXT_PUBLIC_GTM_ID`: Google Tag Manager container id (enables analytics).
 *
 * Loading strategy: the tiny Consent Mode default and the Cookiebot manager run
 * `beforeInteractive`. Cookiebot's `auto` blocking mode rewrites cookie-setting
 * scripts before they execute, so it must load first to do its job; this is the
 * placement Cookiebot documents. The GTM container loads `lazyOnload` (browser
 * idle): analytics is not needed for first paint, so deferring it keeps the
 * ~295 KB of GTM + GA off the main thread during the critical render window. A
 * `preconnect` to the consent origins (in the root layout) keeps the connection
 * cost of the first-party-blocking Cookiebot script low.
 */
export function AnalyticsScripts() {
  if (!COOKIEBOT_CBID) {
    return null;
  }

  return (
    <>
      {/* Deny all storage by default until the user consents (Consent Mode v2). */}
      <Script
        id="google-consent-default"
        strategy="beforeInteractive"
        dangerouslySetInnerHTML={{
          __html: `
            window.dataLayer = window.dataLayer || [];
            function gtag(){dataLayer.push(arguments);}
            gtag('consent', 'default', {
              analytics_storage: 'denied',
              ad_storage: 'denied',
              ad_user_data: 'denied',
              ad_personalization: 'denied',
              wait_for_update: 500
            });
          `,
        }}
      />
      <Script
        id="Cookiebot"
        src="https://consent.cookiebot.com/uc.js"
        data-cbid={COOKIEBOT_CBID}
        data-blockingmode="auto"
        strategy="beforeInteractive"
      />
      {GTM_ID && (
        <>
          <Script
            id="gtm-script"
            strategy="lazyOnload"
            dangerouslySetInnerHTML={{
              __html: `
                (function(w,d,s,l,i){w[l]=w[l]||[];w[l].push({'gtm.start':
                new Date().getTime(),event:'gtm.js'});var f=d.getElementsByTagName(s)[0],
                j=d.createElement(s),dl=l!='dataLayer'?'&l='+l:'';j.async=true;j.src=
                'https://www.googletagmanager.com/gtm.js?id='+i+dl;f.parentNode.insertBefore(j,f);
                })(window,document,'script','dataLayer','${GTM_ID}');
              `,
            }}
          />
          <noscript>
            <iframe
              src={`https://www.googletagmanager.com/ns.html?id=${GTM_ID}`}
              height="0"
              width="0"
              style={{ display: "none", visibility: "hidden" }}
            />
          </noscript>
        </>
      )}
    </>
  );
}
