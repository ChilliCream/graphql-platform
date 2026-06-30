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
 * Loading strategy: the tiny Consent Mode default runs `beforeInteractive` so
 * the denied-by-default consent state exists before any tag can fire. Cookiebot
 * runs in `manual` blocking mode and loads `afterInteractive`: every
 * cookie-setting script on this site is consent-gated at the load level (GTM
 * below, YouTube via click-to-load facades on youtube-nocookie.com), so
 * Cookiebot's `auto` mode script-rewriting is redundant and its
 * `beforeInteractive` placement (a high-priority preload of ~141 KB uc.js +
 * cc.js) only contends with CSS, fonts, and the LCP image on slow connections.
 *
 * GTM is hard-gated on consent (basic Consent Mode): the container is injected
 * only after Cookiebot reports statistics or marketing consent, so no request
 * reaches Google before the user opts in. Advanced Consent Mode (loading GTM
 * up front with denied defaults) still sends cookieless pings carrying the
 * user's IP pre-consent, which is not acceptable here. The Consent Mode
 * defaults above stay as defense in depth for tags that fire between the grant
 * and a later revocation. Tags inside the GTM container must additionally have
 * per-tag consent checks configured, since nothing on the page rewrites
 * scripts that GTM injects.
 *
 * A `preconnect` to the consent origins (in the root layout) keeps the
 * connection cost of the Cookiebot script low.
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
        data-blockingmode="manual"
        strategy="afterInteractive"
      />
      {GTM_ID && (
        <Script
          id="gtm-script"
          strategy="afterInteractive"
          dangerouslySetInnerHTML={{
            __html: `
              (function(){
                function loadGtm(){
                  var consent = window.Cookiebot && window.Cookiebot.consent;
                  if (window.__gtmLoaded || !consent || !(consent.statistics || consent.marketing)) {
                    return;
                  }
                  window.__gtmLoaded = true;
                  (function(w,d,s,l,i){w[l]=w[l]||[];w[l].push({'gtm.start':
                  new Date().getTime(),event:'gtm.js'});var f=d.getElementsByTagName(s)[0],
                  j=d.createElement(s),dl=l!='dataLayer'?'&l='+l:'';j.async=true;j.src=
                  'https://www.googletagmanager.com/gtm.js?id='+i+dl;f.parentNode.insertBefore(j,f);
                  })(window,document,'script','dataLayer','${GTM_ID}');
                }
                window.addEventListener('CookiebotOnConsentReady', loadGtm);
                loadGtm();
              })();
            `,
          }}
        />
      )}
    </>
  );
}
