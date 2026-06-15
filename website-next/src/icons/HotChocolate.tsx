import type { CSSProperties } from "react";

interface HotChocolateProps {
  readonly className?: string;
  readonly style?: CSSProperties;
}

/**
 * Hot Chocolate product drink, inlined as SVG so it ships in the HTML and can be
 * sized and positioned with CSS. Decorative by default.
 */
export function HotChocolate({ className, style }: HotChocolateProps) {
  return (
    <svg
      viewBox="507 32 59 84"
      aria-hidden="true"
      className={className}
      style={style}
    >
      <defs><linearGradient id="hot-chocolate-uuid-38fc5fc7-ceab-4d60-9949-23f500a13b8d" x1="536.93" y1="74.17" x2="561.97" y2="74.17" gradientUnits="userSpaceOnUse"><stop offset="0" stopColor="#0e1522" stopOpacity="0"></stop><stop offset=".4" stopColor="#0e1522" stopOpacity=".1"></stop></linearGradient></defs><g id="hot-chocolate-uuid-be6db7e2-4dca-4a2a-8d97-dcfec0ce7ab3"><path id="hot-chocolate-uuid-a7dc1acb-7f5d-4a79-b7ff-e4c7f83f7600" d="M536.93,35.33c-6.33,0-12.71.46-18.97,1.38-.51.03-1.02.21-1.43.52-.45.34-.78.82-.94,1.36l-1.31,4.49-1.38.04c-1.42.08-2.5,1.23-2.5,2.61v4.65c0,1.32.99,2.43,2.3,2.59h.05s.05,0,.05,0l1.26.07,5.92,56.28c.13,1.27,1.16,2.25,2.43,2.33,4.78.9,9.67,1.36,14.54,1.36h.16c4.53,0,11.67-.98,14.54-1.4,1.19-.17,2.11-1.13,2.24-2.33l5.95-56.24,1.26-.07h.05s.05,0,.05,0c1.31-.15,2.3-1.27,2.3-2.59v-4.65c0-1.38-1.08-2.52-2.45-2.6l-1.43-.04-1.31-4.49c-.31-1.06-1.27-1.82-2.37-1.87-6.24-.91-12.61-1.37-18.92-1.38h-.06Z" fill="#efefef"></path><g id="hot-chocolate-uuid-d5ba2457-f5c5-4a24-ac68-59e44af82f91"><path d="M558.36,52.54c.05-.52.46-.93.97-.99l1.66-.09c.56-.07.98-.54.98-1.1v-4.65c0-.59-.46-1.07-1.04-1.11l-1.7-.05c-.47-.03-.87-.35-1-.79l-1.39-4.76c-.14-.47-.57-.8-1.06-.8-5.42-.79-11.74-1.37-18.79-1.37-7.1,0-13.45.58-18.9,1.37-.09,0-.38.01-.66.22-.19.14-.33.34-.4.58l-1.39,4.76c-.13.45-.53.77-1,.79l-1.7.05c-.59.03-1.04.52-1.04,1.11v4.65c0,.56.42,1.03.98,1.1l1.66.09c.52.06.92.47.97.99l5.96,56.61c.06.57.53,1,1.1,1,3.98.75,8.89,1.36,14.53,1.35,4.5,0,11.82-1.02,14.32-1.39.51-.08.91-.49.96-1l5.98-56.57Z" fill="#f5f0ea"></path></g><path id="hot-chocolate-uuid-a3501823-3df7-4000-a10a-bf6e2896da82" d="M521.62,54.23l5.25,49.94c.04.42.37.76.79.81,3.07.4,6.18.61,9.27.61h.15c2.52,0,6.15-.38,9.1-.74.42-.05.75-.39.79-.81l5.27-49.81c.05-.5-.31-.96-.82-1-4.45-.41-9.32-.66-14.54-.66-5.18,0-10.02.26-14.44.66-.5.05-.87.5-.82,1Z" fill="#c6602e"></path><path id="hot-chocolate-uuid-3a41f22b-084a-4c8b-932f-1193ea5abf35" d="M522.1,58.77h29.66l.48-4.54c.05-.5-.31-.96-.82-1-4.45-.41-9.32-.66-14.54-.66-5.18,0-10.02.26-14.44.66-.5.05-.87.5-.82,1l.48,4.54Z" fill="#0e1522" opacity=".3"></path><path id="hot-chocolate-uuid-d701f288-2763-464d-a14f-534d715c8a9d" d="M560.93,44.61l-1.7-.05c-.47-.03-.87-.35-1-.79l-1.39-4.76c-.14-.47-.57-.8-1.06-.8-5.42-.79-11.74-1.37-18.79-1.37-.02,0-.04,0-.06,0v74.66c.05,0,.11,0,.16,0,4.5,0,11.82-1.02,14.32-1.39.51-.08.91-.49.96-1l5.98-56.57c.05-.52.46-.93.97-.99l1.66-.09c.56-.07.98-.54.98-1.1v-4.65c0-.59-.46-1.07-1.04-1.11Z" fill="url(#hot-chocolate-uuid-38fc5fc7-ceab-4d60-9949-23f500a13b8d)"></path></g>
    </svg>
  );
}
