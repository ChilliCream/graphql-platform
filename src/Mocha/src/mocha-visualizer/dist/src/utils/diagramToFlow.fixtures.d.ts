import { DiagramData } from "../types/diagram";
/**
 * Minimal DiagramData fixture that exercises all code paths in diagramToFlow.
 *
 * Contains:
 * - 1 service with 1 consumer, 1 saga, inbound/outbound routes
 * - 1 transport with receive/dispatch endpoints and topology entities/links
 */
export declare const minimalDiagram: DiagramData;
/**
 * Empty diagram — no services, no transports.
 */
export declare const emptyDiagram: DiagramData;
/**
 * Diagram with a service that has no consumers, routes, or sagas.
 */
export declare const emptyServiceDiagram: DiagramData;
