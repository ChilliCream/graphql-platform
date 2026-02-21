import React, { FC } from "react";
import styled from "styled-components";

import { ContentSectionElement, Link } from "@/components/misc";
import { Company } from "@/components/sprites";
import { MAX_CONTENT_WIDTH, THEME_COLORS } from "@/style";

// Logos
import AdditivLogoSvg from "@/images/companies/additiv.svg";
import AeiLogoSvg from "@/images/companies/aei.svg";
import AtminaLogoSvg from "@/images/companies/atmina.svg";
import AutoguruLogoSvg from "@/images/companies/autoguru.svg";
import BdnaLogoSvg from "@/images/companies/bdna.svg";
import BeyableLogoSvg from "@/images/companies/beyable.svg";
import BiqhLogoSvg from "@/images/companies/biqh.svg";
import CarmmunityLogoSvg from "@/images/companies/carmmunity.svg";
import CompassLogoSvg from "@/images/companies/compass.svg";
import E2mLogoSvg from "@/images/companies/e2m.svg";
import ExlrtLogoSvg from "@/images/companies/exlrt.svg";
import EzeepLogoSvg from "@/images/companies/ezeep.svg";
import FulcrumLogoSvg from "@/images/companies/fulcrum.svg";
import GalaxusLogoSvg from "@/images/companies/galaxus.svg";
import GiaLogoSvg from "@/images/companies/gia.svg";
import HiloLogoSvg from "@/images/companies/hilo.svg";
import IncloudLogoSvg from "@/images/companies/incloud.svg";
import InfoslipsLogoSvg from "@/images/companies/infoslips.svg";
import MicrosoftLogoSvg from "@/images/companies/microsoft.svg";
import MotiviewLogoSvg from "@/images/companies/motiview.svg";
import OrderinLogoSvg from "@/images/companies/orderin.svg";
import PoweredSoftLogoSvg from "@/images/companies/powered-soft.svg";
import PushpayLogoSvg from "@/images/companies/pushpay.svg";
import RailCargoAustriaLogoSvg from "@/images/companies/rail-cargo-austria.svg";
import Seven2OneLogoSvg from "@/images/companies/seven-2-one.svg";
import SolyticLogoSvg from "@/images/companies/solytic.svg";
import SpectrumMedicalLogoSvg from "@/images/companies/spectrum-medical.svg";
import SpeedwayMotorsLogoSvg from "@/images/companies/speedway-motors.svg";
import SplashbackLogoSvg from "@/images/companies/splashback.svg";
import SweetGeeksLogoSvg from "@/images/companies/sweetgeeks.svg";
import SwissLifeLogoSvg from "@/images/companies/swiss-life.svg";
import SytadelleLogoSvg from "@/images/companies/sytadelle.svg";
import TrackmanLogoSvg from "@/images/companies/trackman.svg";
import TravelSoftLogoSvg from "@/images/companies/travel-soft.svg";
import VptechLogoSvg from "@/images/companies/vptech.svg";
import XMLogoSvg from "@/images/companies/xm.svg";
import ZioskLogoSvg from "@/images/companies/ziosk.svg";

export const CompaniesSection: FC = () => (
  <ContentSectionElement>
    <VisibleArea>
      <FadeOut />
      <FadeIn />
      <Ticker>
        {[
          {
            logo: AdditivLogoSvg,
            width: 140,
            url: "https://additiv.com",
            name: "additiv",
          },
          {
            logo: AeiLogoSvg,
            width: 160,
            url: "https://aeieng.com",
            name: "AEI",
          },
          {
            logo: AtminaLogoSvg,
            width: 100,
            url: "https://atmina.de",
            name: "ATMINA",
          },
          {
            logo: AutoguruLogoSvg,
            width: 180,
            url: "https://www.autoguru.com.au",
            name: "AutoGuru",
          },
          {
            logo: BdnaLogoSvg,
            width: 150,
            url: "https://bdna.com.au",
            name: "BDNA",
          },
          {
            logo: BeyableLogoSvg,
            width: 150,
            url: "https://www.beyable.com",
            name: "Beyable",
          },
          {
            logo: BiqhLogoSvg,
            width: 100,
            url: "https://www.biqh.com",
            name: "BIQH",
          },
          {
            logo: CarmmunityLogoSvg,
            width: 180,
            url: "https://carmmunity.io",
            name: "Carmmunity",
          },
          {
            logo: CompassLogoSvg,
            width: 180,
            url: "https://www.compass.education",
            name: "Compass Education",
          },
          {
            logo: E2mLogoSvg,
            width: 90,
            url: "https://www.e2m.energy",
            name: "e2m",
          },
          {
            logo: ExlrtLogoSvg,
            width: 130,
            url: "https://www.exlrt.com",
            name: "Exlrt",
          },
          {
            logo: EzeepLogoSvg,
            width: 100,
            url: "https://www.ezeep.com",
            name: "ezeep",
          },
          {
            logo: FulcrumLogoSvg,
            width: 150,
            url: "https://fulcrumpro.com/",
            name: "Fulcrum",
          },
          {
            logo: GalaxusLogoSvg,
            width: 200,
            url: "https://www.galaxus.ch",
            name: "Galaxus",
          },
          { logo: GiaLogoSvg, width: 120, url: "https://gia.ch", name: "GIA" },
          {
            logo: HiloLogoSvg,
            width: 70,
            url: "https://www.hiloenergie.com",
            name: "Hilo Energie",
          },
          {
            logo: IncloudLogoSvg,
            width: 200,
            url: "https://www.incloud.de",
            name: "Incloud",
          },
          {
            logo: InfoslipsLogoSvg,
            width: 130,
            url: "https://www.infoslips.com",
            name: "Infoslips",
          },
          {
            logo: MicrosoftLogoSvg,
            width: 180,
            url: "https://www.microsoft.com",
            name: "Microsoft",
          },
          {
            logo: MotiviewLogoSvg,
            width: 160,
            url: "https://motitech.co.uk",
            name: "Motitech",
          },
          {
            logo: OrderinLogoSvg,
            width: 160,
            url: "https://orderin.co.za",
            name: "Orderin",
          },
          {
            logo: PoweredSoftLogoSvg,
            width: 110,
            url: "https://poweredsoft.com",
            name: "PoweredSoft",
          },
          {
            logo: PushpayLogoSvg,
            width: 180,
            url: "https://pushpay.com",
            name: "Pushpay",
          },
          {
            logo: RailCargoAustriaLogoSvg,
            width: 260,
            url: "http://www.railcargo.at",
            name: "Rail Cargo Austria",
          },
          {
            logo: Seven2OneLogoSvg,
            width: 120,
            url: "https://www.seven2one.de",
            name: "Seven2One",
          },
          {
            logo: SolyticLogoSvg,
            width: 150,
            url: "https://www.solytic.com",
            name: "Solytic",
          },
          {
            logo: SpectrumMedicalLogoSvg,
            width: 200,
            url: "https://www.spectrummedical.com/",
            name: "Spectrum Medical",
          },
          {
            logo: SpeedwayMotorsLogoSvg,
            width: 120,
            url: "https://www.speedwaymotors.com",
            name: "Speedway Motors",
          },
          {
            logo: SplashbackLogoSvg,
            width: 180,
            url: "https://splashback.io",
            name: "Splashback",
          },
          {
            logo: SweetGeeksLogoSvg,
            width: 120,
            url: "https://sweetgeeks.dk",
            name: "Sweet Geeks",
          },
          {
            logo: SwissLifeLogoSvg,
            width: 100,
            url: "https://www.swisslife.ch",
            name: "Swiss Life",
          },
          {
            logo: SytadelleLogoSvg,
            width: 160,
            url: "https://www.sytadelle.fr",
            name: "Sytadelle",
          },
          {
            logo: TrackmanLogoSvg,
            width: 180,
            url: "http://trackman.com/",
            name: "TrackMan",
          },
          {
            logo: TravelSoftLogoSvg,
            width: 180,
            url: "https://travel-soft.com",
            name: "TravelSoft",
          },
          {
            logo: VptechLogoSvg,
            width: 140,
            url: "https://careers.veepee.com/vptech/",
            name: "VPTech",
          },
          { logo: XMLogoSvg, width: 120, url: "https://xm.com", name: "XM" },
          {
            logo: ZioskLogoSvg,
            width: 120,
            url: "https://www.ziosk.com",
            name: "Ziosk",
          },
        ]}
      </Ticker>
    </VisibleArea>
  </ContentSectionElement>
);

const VisibleArea = styled.div`
  position: relative;
  display: flex;
  flex: 1 1 auto;
  flex-direction: column;
  justify-content: center;
  width: 100%;
  height: 100%;
  max-width: ${MAX_CONTENT_WIDTH}px;
`;

interface TickerLogo {
  readonly width: number;
  readonly logo: Sprite;
  readonly url: string;
  readonly name: string;
}

interface TickerProps {
  readonly children: readonly TickerLogo[];
}

const Ticker: FC<TickerProps> = ({ children }) => {
  const width = children.reduce<number>((p, c) => p + c.width + 60, 0);
  const logos = children.map(({ width, url, logo, name }, index) => (
    <GenericLogo key={"logo-" + index} width={width}>
      <Link to={url} aria-label={name}>
        <Company {...logo} />
      </Link>
    </GenericLogo>
  ));

  return (
    <TickerContainer $width={width} $logoCount={children.length}>
      {/* we have to add the `logos` twice to get a real infinite loop */}
      {logos}
      {logos}
    </TickerContainer>
  );
};

interface TickerContainerProps {
  readonly $width: number;
  readonly $logoCount: number;
}

const TickerContainer = styled.div<TickerContainerProps>`
  display: flex;
  flex-direction: row;
  flex-wrap: nowrap;
  align-items: center;
  justify-content: flex-start;
  width: max-content;
  animation: ticker ${({ $logoCount }) => $logoCount * 2}s linear infinite;

  &:hover {
    animation-play-state: paused;
  }

  @keyframes ticker {
    0% {
      transform: translate3d(0, 0, 0);
    }

    100% {
      transform: translate3d(-${({ $width }) => $width}px, 0, 0);
    }
  }
`;

const GenericLogo = styled.div<{ width?: number }>`
  flex: 0 0 auto;
  margin-right: 30px;
  margin-left: 30px;
  width: ${({ width = 160 }) => width}px;

  & svg {
    fill: ${THEME_COLORS.text};
    transition: fill 0.2s ease-in-out;

    &:hover {
      fill: ${THEME_COLORS.heading};
    }
  }
`;

const FadeOut = styled.div`
  position: absolute;
  top: 0;
  bottom: 0;
  left: 0;
  z-index: 1;
  width: 120px;
  background: linear-gradient(
    270deg,
    #ffffff00 0%,
    ${THEME_COLORS.background} 100%
  );
  pointer-events: none;
`;

const FadeIn = styled.div`
  position: absolute;
  top: 0;
  bottom: 0;
  right: 0;
  z-index: 1;
  width: 120px;
  background: linear-gradient(
    90deg,
    #ffffff00 0%,
    ${THEME_COLORS.background} 100%
  );
  pointer-events: none;
`;
