import type { CSSProperties } from "react";

interface FusionProps {
  readonly className?: string;
  readonly style?: CSSProperties;
}

/**
 * Fusion product drink, inlined as SVG so it ships in the HTML and can be
 * sized and positioned with CSS. Decorative by default.
 */
export function Fusion({ className, style }: FusionProps) {
  return (
    <svg
      viewBox="307 32 59 84"
      aria-hidden="true"
      className={className}
      style={style}
    >
      <defs><linearGradient id="fusion-uuid-e68ffeca-bc53-4ec0-9114-dcc15de03550" x1="336.77" y1="74.17" x2="361.81" y2="74.17" href="#fusion-uuid-8c8812fc-0711-41d3-905b-5d4de255d51b"></linearGradient><linearGradient id="fusion-uuid-8c8812fc-0711-41d3-905b-5d4de255d51b" x1="723.14" y1="85.53" x2="745.91" y2="85.53" gradientUnits="userSpaceOnUse"><stop offset="0" stopColor="#0e1522" stopOpacity="0"></stop><stop offset=".4" stopColor="#0e1522" stopOpacity=".1"></stop></linearGradient><linearGradient id="fusion-uuid-ef31e7b8-6975-4b8f-ad83-eacbbd955bb8" x1="336.77" y1="52.57" x2="336.77" y2="105.59" href="#fusion-uuid-93ed51b8-5d21-49ea-91db-31414340fc86"></linearGradient><linearGradient id="fusion-uuid-93ed51b8-5d21-49ea-91db-31414340fc86" x1="336.77" y1="7.86" x2="336.77" y2="124.23" gradientUnits="userSpaceOnUse"><stop offset=".1" stopColor="#f27765"></stop><stop offset=".3" stopColor="#eabd21"></stop><stop offset=".5" stopColor="#66be77"></stop><stop offset=".7" stopColor="#00bce5"></stop><stop offset=".9" stopColor="#a983ba"></stop></linearGradient></defs><g id="fusion-uuid-f31d5226-8a00-4c71-a6dc-79bebb400524"><path id="fusion-uuid-f3fb996f-0998-48f4-bec4-4ac11d66ff42" d="M336.77,35.33c-6.33,0-12.71.46-18.97,1.38-.51.03-1.02.21-1.43.52-.45.34-.78.81-.94,1.35l-1.31,4.49-1.38.04c-1.42.08-2.5,1.23-2.5,2.6v4.65c0,1.32.99,2.43,2.3,2.59h.05s.05,0,.05,0l1.26.07,5.92,56.28c.13,1.27,1.16,2.25,2.43,2.33,4.78.9,9.67,1.36,14.54,1.36h.16c4.53,0,11.67-.98,14.54-1.4,1.19-.17,2.11-1.13,2.24-2.33l5.95-56.24,1.26-.07h.05s.05,0,.05,0c1.31-.15,2.3-1.27,2.3-2.59v-4.65c0-1.38-1.08-2.52-2.45-2.6l-1.43-.04-1.31-4.49c-.31-1.06-1.27-1.82-2.37-1.87-6.24-.91-12.61-1.37-18.92-1.38h-.06Z" fill="#f5f0ea"></path><g id="fusion-uuid-2702490c-a4c5-49bb-8a76-36c646f7d581"><path d="M358.2,52.54c.05-.52.46-.93.97-.99l1.66-.09c.56-.07.98-.54.98-1.1v-4.65c0-.59-.46-1.07-1.04-1.11l-1.7-.05c-.47-.03-.87-.35-1-.79l-1.39-4.76c-.14-.47-.57-.8-1.06-.8-5.42-.79-11.74-1.37-18.79-1.37-7.1,0-13.45.58-18.9,1.37-.09,0-.38.01-.66.22-.19.14-.33.34-.4.58l-1.39,4.76c-.13.45-.53.77-1,.79l-1.7.05c-.59.03-1.04.52-1.04,1.11v4.65c0,.56.42,1.03.98,1.1l1.66.09c.52.06.92.47.97.99l5.96,56.61c.06.57.53,1,1.1,1,3.98.75,8.89,1.36,14.53,1.35,4.5,0,11.82-1.02,14.32-1.39.51-.08.91-.49.96-1l5.98-56.57Z" fill="#f5f0ea"></path></g><path id="fusion-uuid-574c7118-5598-4c31-985e-968e485aa8d0" d="M321.47,54.23l5.25,49.94c.04.42.37.76.79.81,3.07.4,6.18.61,9.27.61h.15c2.52,0,6.15-.38,9.1-.74.42-.05.75-.39.79-.81l5.27-49.81c.05-.5-.31-.96-.82-1-4.45-.41-9.32-.66-14.54-.66-5.18,0-10.02.26-14.44.66-.5.05-.87.5-.82,1Z" fill="url(#fusion-uuid-ef31e7b8-6975-4b8f-ad83-eacbbd955bb8)"></path><path id="fusion-uuid-343d88cc-177b-472e-be12-268ca0d25a5b" d="M321.94,58.77h29.66l.48-4.54c.05-.5-.31-.96-.82-1-4.45-.41-9.32-.66-14.54-.66-5.18,0-10.02.26-14.44.66-.5.05-.87.5-.82,1l.48,4.54Z" fill="#0e1522" opacity=".3"></path><path id="fusion-uuid-807107bb-d2db-4642-9b0c-a388558e2dc3" d="M360.77,44.61l-1.7-.05c-.47-.03-.87-.35-1-.79l-1.39-4.76c-.14-.47-.57-.8-1.06-.8-5.42-.79-11.74-1.37-18.79-1.37-.02,0-.04,0-.06,0v74.66c.05,0,.11,0,.16,0,4.5,0,11.82-1.02,14.32-1.39.51-.08.91-.49.96-1l5.98-56.57c.05-.52.46-.93.97-.99l1.66-.09c.56-.07.98-.54.98-1.1v-4.65c0-.59-.46-1.07-1.04-1.11Z" fill="url(#fusion-uuid-e68ffeca-bc53-4ec0-9114-dcc15de03550)"></path></g>
    </svg>
  );
}
