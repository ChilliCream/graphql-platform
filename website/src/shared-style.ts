export const MobileBreakpoint = "450px";
export function IsMobile(innerStyle: String) {
  return `
    @media only screen and (max-width: ${MobileBreakpoint}) {
      ${innerStyle}
    }
  `;
}

export const PhabletBreakpoint = "860px";
export function IsPhablet(innerStyle: String) {
  return `
    @media only screen and (max-width: ${PhabletBreakpoint}) {
      ${innerStyle}
    }
  `;
}

export const TabletBreakpoint = "1110px";
export function IsTablet(innerStyle: String) {
  return `
    @media only screen and (max-width: ${TabletBreakpoint}) {
      ${innerStyle}
    }
  `;
}

export const SmallDesktopBreakpointNumber = 1280;
export const SmallDesktopBreakpoint = SmallDesktopBreakpointNumber + "px";
export function IsSmallDesktop(innerStyle: String) {
  return `
    @media only screen and (max-width: ${SmallDesktopBreakpoint}) {
      ${innerStyle}
    }
  `;
}

export const DesktopBreakpointNumber = SmallDesktopBreakpointNumber + 1;
export const DesktopBreakpoint = DesktopBreakpointNumber + "px";
export function IsDesktop(innerStyle: String) {
  return `
    @media only screen and (min-width: ${DesktopBreakpoint}) {
      ${innerStyle}
    }
  `;
}

export const DocPageDesktopGridColumns = `grid-template-columns: 1fr 250px 820px 250px 1fr;`;
export const BoxShadow = `box-shadow: rgba(0, 0, 0, 0.25) 0px 3px 6px 0px;`;
