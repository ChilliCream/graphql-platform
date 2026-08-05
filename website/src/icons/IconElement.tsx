import type { IconName } from "@fortawesome/fontawesome-svg-core";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import type { ComponentProps, ReactNode } from "react";

import { library } from "@fortawesome/fontawesome-svg-core";

import {
  faArrowRightArrowLeft,
  faBlog,
  faBuilding,
  faBuildingLock,
  faChartLineUp,
  faChevronDown,
  faCirclePlay,
  faClipboardCheck,
  faCloud,
  faCodeBranch,
  faCookieBite,
  faCubes,
  faDiagramProject,
  faEllipsis,
  faEnvelope,
  faFileCertificate,
  faFileContract,
  faFileLines,
  faGear,
  faHandshakeAngle,
  faHashtag,
  faLayerGroup,
  faLink,
  faMessage,
  faMessageQuestion,
  faNewspaper,
  faRightLeft,
  faRocket,
  faRoute,
  faServer,
  faShirt,
  faStar,
} from "@fortawesome/pro-solid-svg-icons";

library.add(
  faArrowRightArrowLeft,
  faBlog,
  faBuilding,
  faBuildingLock,
  faChartLineUp,
  faChevronDown,
  faCirclePlay,
  faClipboardCheck,
  faCloud,
  faCodeBranch,
  faCookieBite,
  faCubes,
  faDiagramProject,
  faEllipsis,
  faEnvelope,
  faFileCertificate,
  faFileContract,
  faFileLines,
  faGear,
  faHandshakeAngle,
  faHashtag,
  faLayerGroup,
  faLink,
  faMessage,
  faMessageQuestion,
  faNewspaper,
  faRightLeft,
  faRocket,
  faRocket,
  faRoute,
  faServer,
  faShirt,
  faStar,
);

interface IconElementProps extends Omit<
  ComponentProps<typeof FontAwesomeIcon>,
  "icon"
> {
  icon: IconName;
}

export function IconElement({ icon, ...rest }: IconElementProps): ReactNode {
  return <FontAwesomeIcon icon={["fas", icon]} {...rest} />;
}
