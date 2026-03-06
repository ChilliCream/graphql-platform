import h, {
  createGlobalStyle as on,
  css as F,
  keyframes as Se,
} from "styled-components";
import { jsxs as g, jsx as i, Fragment as pe } from "react/jsx-runtime";
import {
  useState as Y,
  useMemo as U,
  useCallback as G,
  useEffect as ie,
  useRef as ve,
} from "react";
import {
  Handle as W,
  Position as N,
  BaseEdge as ft,
  getSmoothStepPath as nn,
  EdgeLabelRenderer as io,
  ReactFlow as ht,
  Background as gt,
  BackgroundVariant as mt,
  useNodesState as sn,
  useEdgesState as rn,
  applyNodeChanges as an,
  Controls as cn,
  MiniMap as ln,
  useReactFlow as xt,
  useInternalNode as Ct,
  useStore as dn,
} from "@xyflow/react";
import { FontAwesomeIcon as z } from "@fortawesome/react-fontawesome";
import {
  faChevronUp as ao,
  faChevronDown as co,
  faStepBackward as pn,
  faPause as un,
  faPlay as fn,
  faStepForward as hn,
  faExclamationTriangle as gn,
  faLayerGroup as yt,
  faHashtag as lo,
  faRightLeft as po,
  faCubes as we,
  faArrowRightArrowLeft as bt,
  faEnvelope as $t,
  faDiagramProject as ue,
  faGear as Xe,
  faServer as uo,
  faNetworkWired as fo,
  faArrowLeft as mn,
  faTimes as ho,
  faMagnifyingGlass as go,
  faXmark as xn,
  faRoute as mo,
  faClock as xo,
  faExpand as yo,
  faLink as yn,
  faCircleInfo as bn,
  faCode as $n,
  faCrosshairs as vn,
  faMagnifyingGlassPlus as wn,
  faMagnifyingGlassMinus as Tn,
} from "@fortawesome/free-solid-svg-icons";
import bo from "elkjs/lib/elk.bundled.js";
const s = {
    colors: {
      canvas: {
        default: "#0d1117",
        subtle: "#161b22",
        inset: "#010409",
      },
      border: {
        default: "#30363d",
        muted: "#21262d",
      },
      fg: {
        default: "#c9d1d9",
        muted: "#8b949e",
        subtle: "#6e7681",
      },
      accent: {
        fg: "#58a6ff",
        emphasis: "#1f6feb",
      },
      success: {
        fg: "#3fb950",
      },
      attention: {
        fg: "#d29922",
      },
      danger: {
        fg: "#f85149",
      },
      done: {
        fg: "#a371f7",
      },
      sponsors: {
        fg: "#db61a2",
      },
      scale: {
        purple4: "#a371f7",
        purple5: "#8957e5",
      },
    },
    fonts: {
      sans: "-apple-system, BlinkMacSystemFont, 'Segoe UI', 'Noto Sans', Helvetica, Arial, sans-serif",
      mono: "ui-monospace, SFMono-Regular, SF Mono, Menlo, Consolas, monospace",
    },
  },
  dc = on`
  * {
    box-sizing: border-box;
  }

  html, body, #root {
    margin: 0;
    padding: 0;
    width: 100%;
    height: 100%;
    font-family: ${s.fonts.sans};
    background-color: ${s.colors.canvas.default};
    color: ${s.colors.fg.default};
  }

  /* React Flow Overrides */
  .react-flow__node {
    font-family: inherit;
    overflow: visible;
  }

  .react-flow__edge-path {
    stroke-width: 1.5;
  }

  /* Flow Controls */
  .react-flow__controls {
    background: ${s.colors.canvas.subtle} !important;
    border: 1px solid ${s.colors.border.default} !important;
    border-radius: 6px !important;
    box-shadow: 0 1px 3px rgba(1, 4, 9, 0.5) !important;
  }

  .react-flow__controls button {
    background: ${s.colors.canvas.subtle} !important;
    border-color: ${s.colors.border.default} !important;
    color: ${s.colors.fg.default} !important;
  }

  .react-flow__controls button:hover {
    background: ${s.colors.canvas.inset} !important;
  }

  .react-flow__controls button svg {
    fill: ${s.colors.fg.muted} !important;
  }

  /* Flow Minimap */
  .react-flow__minimap {
    background: ${s.colors.canvas.subtle} !important;
    border: 1px solid ${s.colors.border.default} !important;
    border-radius: 6px !important;
  }

  /* Handle Styles — invisible since the graph is static (no connecting) */
  .react-flow__handle {
    width: 1px;
    height: 1px;
    min-width: 0;
    min-height: 0;
    background: transparent;
    border: none;
    opacity: 0;
    pointer-events: none;
  }

  /* Hover Highlighting */
  .react-flow__node.highlighted .compact-node {
    border-color: ${s.colors.accent.fg};
    box-shadow: 0 0 0 2px rgba(88, 166, 255, 0.3);
    background: ${s.colors.canvas.inset};
  }

  .react-flow__node.highlighted .route-node {
    border-color: ${s.colors.accent.fg};
    box-shadow: 0 0 0 2px rgba(88, 166, 255, 0.3);
    background: rgba(88, 166, 255, 0.15);
  }

  .react-flow__node.dimmed {
    opacity: 0.3;
    transition: opacity 0.15s ease;
  }

  .react-flow__node.highlighted {
    transition: opacity 0.15s ease;
  }

  /* Trace Overlay — active path nodes */
  .react-flow__node.trace-active .compact-node {
    border-color: ${s.colors.accent.fg};
    box-shadow: 0 0 0 2px rgba(88, 166, 255, 0.3);
  }

  .react-flow__node.trace-active .route-node {
    border-color: ${s.colors.accent.fg};
    box-shadow: 0 0 0 2px rgba(88, 166, 255, 0.3);
  }

  /* Trace Overlay — inferred transport-internal nodes */
  .react-flow__node.trace-inferred .compact-node {
    border-style: dashed;
    opacity: 0.65;
  }

  .react-flow__node.trace-inferred .route-node {
    border-style: dashed;
    opacity: 0.65;
  }

  /* Trace Overlay — error nodes */
  .react-flow__node.trace-error .compact-node {
    border-color: ${s.colors.danger.fg};
    box-shadow: 0 0 0 2px rgba(248, 81, 73, 0.3);
  }

  .react-flow__node.trace-error .route-node {
    border-color: ${s.colors.danger.fg};
    box-shadow: 0 0 0 2px rgba(248, 81, 73, 0.3);
  }

  /* Trace Overlay — focused node (currently selected step) */
  .react-flow__node.trace-focused .compact-node {
    border-color: #79c0ff;
    box-shadow: 0 0 0 3px rgba(88, 166, 255, 0.6), 0 0 12px rgba(88, 166, 255, 0.4);
    background: rgba(88, 166, 255, 0.12);
    animation: trace-pulse 1.5s ease-in-out infinite;
  }

  .react-flow__node.trace-focused .route-node {
    border-color: #79c0ff;
    box-shadow: 0 0 0 3px rgba(88, 166, 255, 0.6), 0 0 12px rgba(88, 166, 255, 0.4);
    background: rgba(88, 166, 255, 0.12);
    animation: trace-pulse 1.5s ease-in-out infinite;
  }

  @keyframes trace-pulse {
    0%, 100% {
      box-shadow: 0 0 0 3px rgba(88, 166, 255, 0.6), 0 0 12px rgba(88, 166, 255, 0.4);
    }
    50% {
      box-shadow: 0 0 0 5px rgba(88, 166, 255, 0.4), 0 0 20px rgba(88, 166, 255, 0.3);
    }
  }

  /* Trace Overlay — hidden nodes (traceDisplayMode='hide') */
  .react-flow__node.hidden {
    display: none;
  }

  /* Saga Focus Mode — highlighted nodes */
  .react-flow__node.saga-focus-highlighted .focus-state-node {
    border-color: ${s.colors.accent.fg};
    box-shadow: 0 0 0 2px rgba(88, 166, 255, 0.3), 0 0 8px rgba(88, 166, 255, 0.15);
  }

  /* Saga Focus Mode — dimmed nodes */
  .react-flow__node.saga-focus-dimmed .focus-state-node {
    opacity: 0.3;
  }

  .react-flow__node.saga-focus-dimmed {
    transition: opacity 0.15s ease;
  }

  .react-flow__node.saga-focus-highlighted {
    transition: opacity 0.15s ease;
  }

  /* Saga Focus Mode — highlighted edges */
  .react-flow__edge.saga-focus-highlighted path {
    stroke-width: 3 !important;
    filter: brightness(1.3);
  }

  /* Saga Focus Mode — dimmed edges */
  .react-flow__edge.saga-focus-dimmed path {
    opacity: 0.2;
  }

  /* Animated edge */
  .react-flow__edge.animated path {
    stroke-dasharray: 5;
    animation: edge-flow 0.5s linear infinite;
  }

  @keyframes edge-flow {
    to {
      stroke-dashoffset: -10;
    }
  }

  @keyframes spin {
    to {
      transform: rotate(360deg);
    }
  }

  /* Search Highlight — pulsing border on focused search result */
  .react-flow__node.search-highlight .compact-node,
  .react-flow__node.search-highlight .route-node {
    border-color: #79c0ff;
    box-shadow: 0 0 0 3px rgba(88, 166, 255, 0.6), 0 0 12px rgba(88, 166, 255, 0.4);
    animation: search-pulse 1.5s ease-out forwards;
  }

  @keyframes search-pulse {
    0% {
      box-shadow: 0 0 0 4px rgba(88, 166, 255, 0.7), 0 0 20px rgba(88, 166, 255, 0.5);
    }
    100% {
      box-shadow: 0 0 0 2px rgba(88, 166, 255, 0.3), 0 0 0 transparent;
    }
  }

`,
  he = {
    GROUP_PADDING: 25,
    TITLE_HEIGHT: 40,
  },
  Ae = {
    edge: { stroke: "#6e7681", strokeWidth: 1 },
    serviceGroup: {
      backgroundColor: "rgba(22, 27, 34, 0.3)",
      borderRadius: "8px",
      border: "1px solid #30363d",
    },
    inboundGroup: {
      backgroundColor: "rgba(88, 166, 255, 0.05)",
      borderRadius: "6px",
      border: "1px solid rgba(88, 166, 255, 0.3)",
    },
    outboundGroup: {
      backgroundColor: "rgba(219, 97, 162, 0.05)",
      borderRadius: "6px",
      border: "1px solid rgba(219, 97, 162, 0.3)",
    },
    transportGroup: {
      backgroundColor: "rgba(163, 113, 247, 0.05)",
      borderRadius: "8px",
      border: "1px solid #a371f7",
    },
  },
  $e = {
    SERVICE_GROUP: "service-group",
    SERVICE_TITLE: "service-title",
    INBOUND_SUBFLOW: "inbound-subflow",
    OUTBOUND_SUBFLOW: "outbound-subflow",
  };
function kn(e) {
  const t = { nodes: [], edges: [] };
  return (
    e.services.forEach((o, n) => {
      Sn(t, o, n, e.transports);
    }),
    _n(t, e),
    Hn(t, e),
    { nodes: t.nodes, edges: t.edges }
  );
}
function Sn(e, t, o, n) {
  const r = new Set(t.sagas.map((u) => u.consumerName)),
    a = o > 0 ? `-${o}` : "",
    d = `${$e.SERVICE_GROUP}${a}`,
    p = `${$e.SERVICE_TITLE}${a}`,
    l = `${$e.INBOUND_SUBFLOW}${a}`,
    c = `${$e.OUTBOUND_SUBFLOW}${a}`;
  Qe(e, {
    id: d,
    position: { x: 20, y: 20 },
    size: { width: 600, height: 700 },
    style: Ae.serviceGroup,
    label: t.host.serviceName,
  }),
    Je(e, {
      id: p,
      parentId: d,
      position: { x: he.GROUP_PADDING, y: 0 },
      label: t.host.serviceName,
      type: "service",
    }),
    In(e, t, r, d, l, n, a),
    Cn(e, t, d, c, n, a);
}
function In(e, t, o, n, r, a, d) {
  Qe(e, {
    id: r,
    parentId: n,
    position: { x: he.GROUP_PADDING, y: he.TITLE_HEIGHT + 15 },
    size: { width: 300, height: 60 },
    style: Ae.inboundGroup,
    label: "Inbound",
  }),
    Je(e, {
      id: `${r}-title`,
      parentId: r,
      position: { x: he.GROUP_PADDING, y: 0 },
      label: "Inbound",
      type: "service",
    }),
    t.consumers.filter((u) => !o.has(u.name)).forEach((u) => On(e, u, r, d)),
    t.sagas.forEach((u) => Fn(e, u, r, d)),
    t.routes.inbound.forEach((u, f) => {
      $o(e, {
        id: `route-inbound${d}-${f}`,
        parentId: r,
        kind: u.kind,
        direction: "inbound",
        messageTypeIdentity: u.messageTypeIdentity,
        consumerName: u.consumerName,
        endpointName: u.endpoint.name,
      });
    });
  const l = new Set(t.routes.inbound.map((u) => u.endpoint.name)),
    c = /* @__PURE__ */ new Set();
  a.forEach((u) => {
    u.receiveEndpoints.forEach((f) => {
      l.has(f.name) &&
        !c.has(f.name) &&
        (c.add(f.name),
        vo(e, {
          id: `receive-${f.name}`,
          parentId: r,
          name: f.name,
          address: f.address,
          transportName: u.name,
          subType: "receive",
          layer: "LAST",
        }));
    });
  }),
    En(e, t.routes.inbound, t.sagas, o, d);
}
function En(e, t, o, n, r) {
  t.forEach((a, d) => {
    const p = `route-inbound${r}-${d}`,
      l = Nn(a.consumerName, o, n, r);
    te(e, {
      id: `edge-consumer-route${r}-${d}`,
      target: l,
      source: p,
      targetHandle: "right",
      sourceHandle: "left-source",
      markerEnd: !0,
    }),
      te(e, {
        id: `edge-route-recv${r}-${d}`,
        source: `receive-${a.endpoint.name}`,
        target: p,
        targetHandle: "right",
        sourceHandle: "left-source",
        markerEnd: !0,
      });
  });
}
function Nn(e, t, o, n) {
  if (o.has(e)) {
    const r = t.find((a) => a.consumerName === e);
    if (r) return `saga${n}-${r.name}`;
  }
  return `consumer${n}-${e}`;
}
function Cn(e, t, o, n, r, a) {
  Qe(e, {
    id: n,
    parentId: o,
    position: { x: he.GROUP_PADDING, y: 350 },
    size: { width: 300, height: 60 },
    style: Ae.outboundGroup,
    label: "Outbound",
  }),
    Je(e, {
      id: `${n}-title`,
      parentId: n,
      position: { x: he.GROUP_PADDING, y: 0 },
      label: "Outbound",
      type: "service",
    });
  const d = /* @__PURE__ */ new Map();
  t.messageTypes.forEach((u, f) => {
    d.set(u.identity, f);
  });
  const p = new Set(t.routes.outbound.map((u) => u.messageTypeIdentity));
  t.messageTypes.forEach((u, f) => {
    p.has(u.identity) &&
      An(e, {
        id: `msgtype${a}-${f}`,
        parentId: n,
        identity: u.identity,
        messageType: u,
      });
  }),
    t.routes.outbound.forEach((u, f) => {
      $o(e, {
        id: `route-outbound${a}-${f}`,
        parentId: n,
        kind: u.kind,
        direction: "outbound",
        messageTypeIdentity: u.messageTypeIdentity,
        endpointName: u.endpoint.name,
      });
    });
  const l = new Set(t.routes.outbound.map((u) => u.endpoint.name)),
    c = /* @__PURE__ */ new Set();
  r.forEach((u) => {
    u.dispatchEndpoints.forEach((f) => {
      l.has(f.name) &&
        !c.has(f.name) &&
        (c.add(f.name),
        vo(e, {
          id: `dispatch-${f.name}`,
          parentId: n,
          name: f.name,
          address: f.address,
          transportName: u.name,
          subType: "dispatch",
          layer: "LAST",
        }));
    });
  }),
    Mn(e, t.routes.outbound, d, a);
}
function Mn(e, t, o, n) {
  t.forEach((r, a) => {
    const d = `route-outbound${n}-${a}`,
      p = o.get(r.messageTypeIdentity);
    p !== void 0 &&
      te(e, {
        id: `edge-msg-route${n}-${a}`,
        source: `msgtype${n}-${p}`,
        target: d,
        sourceHandle: "right-source",
        targetHandle: "left",
        markerEnd: !0,
      }),
      te(e, {
        id: `edge-route-dispatch${n}-${a}`,
        source: d,
        target: `dispatch-${r.endpoint.name}`,
        sourceHandle: "right-source",
        targetHandle: "left",
        markerEnd: !0,
      });
  });
}
function _n(e, t) {
  const o = [],
    n = [];
  t.services.forEach((r) => {
    o.push(...r.routes.outbound), n.push(...r.routes.inbound);
  }),
    t.transports.forEach((r) => {
      Ln(e, r, o, n);
    });
}
function Ln(e, t, o, n) {
  const r = `transport-${t.name}`;
  Qe(e, {
    id: r,
    position: { x: 750, y: 50 },
    size: { width: 500, height: 400 },
    style: Ae.transportGroup,
    label: t.name,
  }),
    Je(e, {
      id: `${r}-title`,
      parentId: r,
      position: { x: he.GROUP_PADDING, y: 0 },
      label: t.name,
      type: "transport",
    }),
    Pn(e, t.topology.entities, t.topology.links, r),
    Rn(e, t.topology.links, t.name, r),
    Dn(e, o, t),
    zn(e, n, t);
}
function Pn(e, t, o, n) {
  const r = new Set(
    o.flatMap((a) =>
      a.direction === "bidirectional"
        ? [a.source, a.target]
        : a.direction === "forward"
        ? [a.target]
        : [a.source]
    )
  );
  t.forEach((a) => {
    const d = Bn(a),
      p = a.flow === "outbound" ? "LAST" : r.has(a.address) ? void 0 : "FIRST";
    e.nodes.push({
      id: d,
      type: "compact",
      position: { x: 0, y: 0 },
      data: {
        label: Yn(a.name, 30),
        nodeType: "entity",
        entityKind: a.kind,
        elkLayer: p,
        fullData: {
          name: a.name,
          kind: a.kind,
          address: a.address,
          flow: a.flow,
          properties: a.properties,
        },
      },
      parentId: n,
      extent: "parent",
    });
  });
}
function Rn(e, t, o, n) {
  t.forEach((r, a) => {
    const d = Te(r.source),
      p = Te(r.target);
    if (!d || !p || !ke(e, d) || !ke(e, p)) return;
    const l = `binding-${o}-${a}`;
    e.nodes.push({
      id: l,
      type: "route",
      position: { x: 0, y: 0 },
      data: {
        label: r.kind,
        kind: r.kind,
        nodeType: "binding",
        fullData: {
          kind: r.kind,
          address: r.address,
          direction: r.direction,
          source: r.source,
          target: r.target,
          properties: r.properties,
        },
      },
      parentId: n,
      extent: "parent",
    }),
      Gn(e, l, d, p, r.direction);
  });
}
function Dn(e, t, o) {
  o.dispatchEndpoints.forEach((n) => {
    const r = Te(n.destination.address);
    !r ||
      !ke(e, r) ||
      (ke(e, `dispatch-${n.name}`) &&
        te(e, {
          id: `edge-dispatch-entity-${n.name}`,
          source: `dispatch-${n.name}`,
          target: r,
          sourceHandle: "right-source",
          targetHandle: "left",
          markerEnd: !0,
        }));
  });
}
function zn(e, t, o) {
  o.receiveEndpoints.forEach((n) => {
    const r = Te(n.source.address);
    !r ||
      !ke(e, r) ||
      (ke(e, `receive-${n.name}`) &&
        te(e, {
          id: `edge-entity-recv-${n.name}`,
          source: r,
          target: `receive-${n.name}`,
          sourceHandle: "right-source",
          targetHandle: "right",
          markerEnd: !0,
        }));
  });
}
function Qe(e, t) {
  e.nodes.push({
    id: t.id,
    type: "group",
    position: t.position,
    style: {
      ...t.style,
      width: t.size.width,
      height: t.size.height,
    },
    data: { label: t.label },
    ...(t.parentId && { parentId: t.parentId, extent: "parent" }),
  });
}
function Je(e, t) {
  e.nodes.push({
    id: t.id,
    type: "groupLabel",
    position: t.position,
    data: {
      label: t.label,
      type: t.type,
    },
    parentId: t.parentId,
    extent: "parent",
    draggable: !1,
  });
}
function On(e, t, o, n) {
  e.nodes.push({
    id: `consumer${n}-${t.name}`,
    type: "compact",
    position: { x: 0, y: 0 },
    data: {
      label: vt(t.name),
      nodeType: "consumer",
      isBatch: t.isBatch || !1,
      elkLayer: "FIRST",
      fullData: {
        name: t.name,
        identityType: t.identityType,
        identityTypeFullName: t.identityTypeFullName,
        isBatch: t.isBatch || !1,
      },
    },
    parentId: o,
    extent: "parent",
  });
}
function Fn(e, t, o, n) {
  e.nodes.push({
    id: `saga${n}-${t.name}`,
    type: "compact",
    position: { x: 0, y: 0 },
    data: {
      label: vt(t.name),
      nodeType: "saga",
      elkLayer: "FIRST",
      fullData: {
        name: t.name,
        stateType: t.stateType,
        stateTypeFullName: t.stateTypeFullName,
        consumerName: t.consumerName,
        states: t.states,
      },
    },
    parentId: o,
    extent: "parent",
  });
}
function $o(e, t) {
  e.nodes.push({
    id: t.id,
    type: "route",
    position: { x: 0, y: 0 },
    data: {
      label: t.kind,
      kind: t.kind,
      direction: t.direction,
      nodeType: "route",
      fullData: {
        kind: t.kind,
        direction: t.direction,
        messageTypeIdentity: t.messageTypeIdentity,
        consumerName: t.consumerName,
        endpointName: t.endpointName,
      },
    },
    parentId: t.parentId,
    extent: "parent",
  });
}
function vo(e, t) {
  e.nodes.push({
    id: t.id,
    type: "compact",
    position: { x: 0, y: 0 },
    data: {
      label: vt(t.name),
      nodeType: "endpoint",
      subType: t.subType,
      elkLayer: t.layer,
      fullData: {
        name: t.name,
        address: t.address,
        transportName: t.transportName,
        kind: t.subType,
      },
    },
    parentId: t.parentId,
    extent: "parent",
  });
}
function An(e, t) {
  e.nodes.push({
    id: t.id,
    type: "compact",
    position: { x: 0, y: 0 },
    data: {
      label: t.messageType?.runtimeType || "Unknown",
      nodeType: "message",
      elkLayer: "FIRST",
      fullData: {
        name: t.messageType?.runtimeType,
        identity: t.identity,
        runtimeType: t.messageType?.runtimeType,
        runtimeTypeFullName: t.messageType?.runtimeTypeFullName,
        isInterface: t.messageType?.isInterface,
        isInternal: t.messageType?.isInternal,
      },
    },
    parentId: t.parentId,
    extent: "parent",
  });
}
function te(e, t) {
  e.edges.push({
    id: t.id,
    source: t.source,
    target: t.target,
    sourceHandle: t.sourceHandle,
    targetHandle: t.targetHandle,
    type: "smoothstep",
    style: Ae.edge,
    animated: t.animated,
    markerEnd: t.markerEnd ? { type: "arrowclosed", color: "#6e7681" } : void 0,
  });
}
function Gn(e, t, o, n, r) {
  r === "reverse"
    ? (te(e, {
        id: `edge-${t}-from`,
        source: n,
        target: t,
        sourceHandle: "left-source",
        targetHandle: "right",
        markerEnd: !0,
      }),
      te(e, {
        id: `edge-${t}-to`,
        source: t,
        target: o,
        sourceHandle: "left-source",
        targetHandle: "right",
        markerEnd: !0,
      }))
    : r === "bidirectional"
    ? (te(e, {
        id: `edge-${t}-from`,
        source: o,
        target: t,
        sourceHandle: "right-source",
        targetHandle: "left",
      }),
      te(e, {
        id: `edge-${t}-to`,
        source: t,
        target: n,
        sourceHandle: "right-source",
        targetHandle: "left",
      }))
    : (te(e, {
        id: `edge-${t}-from`,
        source: o,
        target: t,
        sourceHandle: "right-source",
        targetHandle: "left",
        markerEnd: !0,
      }),
      te(e, {
        id: `edge-${t}-to`,
        source: t,
        target: n,
        sourceHandle: "right-source",
        targetHandle: "left",
        markerEnd: !0,
      }));
}
const Le = "summary-";
function Hn(e, t) {
  const o = /* @__PURE__ */ new Map();
  t.services.forEach((n, r) => {
    const a = r > 0 ? `-${r}` : "",
      d = `${$e.SERVICE_GROUP}${a}`,
      p = new Set(n.sagas.map((f) => f.consumerName)),
      l = n.consumers.filter((f) => !p.has(f.name)),
      c = /* @__PURE__ */ new Map();
    [...n.routes.inbound, ...n.routes.outbound].forEach((f) => {
      const m = f.endpoint.transportName;
      c.set(m, (c.get(m) ?? 0) + 1);
    }),
      o.set(d, c);
    const u = `${Le}${d}`;
    e.nodes.push({
      id: u,
      type: "summaryService",
      position: { x: 0, y: 0 },
      // Will be repositioned by elkLayout at group centroid
      data: {
        label: n.host.serviceName,
        consumerCount: l.length,
        messageCount: n.messageTypes.length,
        sagaCount: n.sagas.length,
        transportNames: [...c.keys()],
        serviceGroupId: d,
      },
      hidden: !0,
      // Hidden by default (shown at zoom < 0.35)
    });
  }),
    t.transports.forEach((n) => {
      const r = `transport-${n.name}`,
        a = `${Le}${r}`,
        d = {};
      n.topology.entities.forEach((p) => {
        d[p.kind] = (d[p.kind] ?? 0) + 1;
      }),
        e.nodes.push({
          id: a,
          type: "summaryTransport",
          position: { x: 0, y: 0 },
          // Will be repositioned by elkLayout at group centroid
          data: {
            label: n.name,
            entityCounts: d,
            totalEntityCount: n.topology.entities.length,
            transportGroupId: r,
          },
          hidden: !0,
          // Hidden by default (shown at zoom < 0.35)
        });
    }),
    t.services.forEach((n, r) => {
      const a = r > 0 ? `-${r}` : "",
        d = `${$e.SERVICE_GROUP}${a}`,
        p = o.get(d);
      p &&
        p.forEach((l, c) => {
          const u = `${Le}${d}`,
            f = `${Le}transport-${c}`;
          e.edges.push({
            id: `${Le}edge-${d}-${c}`,
            source: u,
            target: f,
            sourceHandle: "right-source",
            targetHandle: "left",
            type: "smoothstep",
            style: { stroke: "#6e7681", strokeWidth: 2, opacity: 0.8 },
            label: `${l}`,
            labelStyle: {
              fill: "#8b949e",
              fontSize: 11,
              fontWeight: 600,
            },
            labelBgStyle: {
              fill: "#161b22",
              fillOpacity: 0.9,
            },
            labelBgPadding: [6, 3],
            labelBgBorderRadius: 4,
            hidden: !0,
            // Hidden by default (shown at low zoom)
            data: { isSummaryEdge: !0, routeCount: l },
          });
        });
    });
}
function Te(e) {
  const t = e.match(/\/([a-z])\/(.+)$/);
  if (t) {
    const [, n, r] = t;
    return `entity-${n}-${r}`;
  }
  const o = e.split("/").filter(Boolean);
  return o.length > 0 ? `entity-x-${o[o.length - 1]}` : null;
}
function Bn(e) {
  return `entity-${e.kind.charAt(0)}-${e.name}`;
}
function vt(e) {
  return e.split(".").pop() || e;
}
function Yn(e, t) {
  return e.length <= t ? e : `${e.substring(0, t - 3)}...`;
}
function ke(e, t) {
  return e.nodes.some((o) => o.id === t);
}
const wo = {
    gridCellSize: 25,
    // Increased from 10 for better performance
    nodePadding: 10,
    borderRadius: 8,
    graphPadding: 20,
  },
  ye = 200,
  Un = 5e3;
class Wn {
  heap = [];
  positionMap = /* @__PURE__ */ new Map();
  // key -> heap index
  key(t, o) {
    return `${t},${o}`;
  }
  get size() {
    return this.heap.length;
  }
  push(t) {
    const o = this.key(t.x, t.y),
      n = this.positionMap.get(o);
    if (n !== void 0) {
      t.f < this.heap[n].f &&
        ((this.heap[n] = t), this.bubbleUp(n), this.bubbleDown(n));
      return;
    }
    this.heap.push(t),
      this.positionMap.set(o, this.heap.length - 1),
      this.bubbleUp(this.heap.length - 1);
  }
  pop() {
    if (this.heap.length === 0) return;
    const t = this.heap[0],
      o = this.heap.pop();
    return (
      this.positionMap.delete(this.key(t.x, t.y)),
      this.heap.length > 0 &&
        ((this.heap[0] = o),
        this.positionMap.set(this.key(o.x, o.y), 0),
        this.bubbleDown(0)),
      t
    );
  }
  has(t, o) {
    return this.positionMap.has(this.key(t, o));
  }
  bubbleUp(t) {
    for (; t > 0; ) {
      const o = (t - 1) >> 1;
      if (this.heap[t].f >= this.heap[o].f) break;
      this.swap(t, o), (t = o);
    }
  }
  bubbleDown(t) {
    const o = this.heap.length;
    for (;;) {
      const n = (t << 1) + 1,
        r = n + 1;
      let a = t;
      if (
        (n < o && this.heap[n].f < this.heap[a].f && (a = n),
        r < o && this.heap[r].f < this.heap[a].f && (a = r),
        a === t)
      )
        break;
      this.swap(t, a), (t = a);
    }
  }
  swap(t, o) {
    const n = this.heap[t],
      r = this.heap[o];
    (this.heap[t] = r),
      (this.heap[o] = n),
      this.positionMap.set(this.key(n.x, n.y), o),
      this.positionMap.set(this.key(r.x, r.y), t);
  }
}
function Kn(e, t, o, n = wo) {
  const r = o.filter((x) => x.type !== "group" && x.width && x.height),
    a = jn(e, t, r, n);
  if (a) return a;
  const d = Zn(e, t, r, n),
    p = Qn(d, r, n);
  if (!p) return null;
  const l = _t(e, d, n.gridCellSize),
    c = _t(t, d, n.gridCellSize);
  if (!je(l, p) || !je(c, p)) return null;
  const u = Lt(l, p),
    f = Lt(c, p);
  if (!u || !f) return null;
  const m = Xn(u, f, p);
  if (!m) return null;
  const y = m.map((x) => Jn(x, d, n.gridCellSize));
  return y.unshift(e), y.push(t), es(y);
}
const To = [
  { dx: 0, dy: -1 },
  { dx: 0, dy: 1 },
  { dx: -1, dy: 0 },
  { dx: 1, dy: 0 },
];
function Xn(e, t, o) {
  const n = new Wn(),
    r = /* @__PURE__ */ new Set(),
    a = /* @__PURE__ */ new Map(),
    d = /* @__PURE__ */ new Map(),
    p = (x, b) => `${x},${b}`,
    l = p(e.x, e.y),
    c = p(t.x, t.y),
    u = (x, b) => Math.abs(x - t.x) + Math.abs(b - t.y);
  d.set(l, 0),
    n.push({
      x: e.x,
      y: e.y,
      g: 0,
      f: u(e.x, e.y),
      parentKey: null,
    });
  let f = 0;
  const m = o.length,
    y = o[0].length;
  for (; n.size > 0 && f < Un; ) {
    f++;
    const x = n.pop(),
      b = p(x.x, x.y);
    if (b === c) {
      const $ = [];
      let v = b;
      for (; v; ) {
        const [T, C] = v.split(",").map(Number);
        $.unshift({ x: T, y: C }), (v = a.get(v));
      }
      return $;
    }
    r.add(b);
    for (const { dx: $, dy: v } of To) {
      const T = x.x + $,
        C = x.y + v;
      if (C < 0 || C >= m || T < 0 || T >= y || !o[C][T]) continue;
      const w = p(T, C);
      if (r.has(w)) continue;
      const I = x.g + 1,
        O = d.get(w);
      (O === void 0 || I < O) &&
        (d.set(w, I),
        a.set(w, b),
        n.push({
          x: T,
          y: C,
          g: I,
          f: I + u(T, C),
          parentKey: b,
        }));
    }
  }
  return null;
}
function jn(e, t, o, n) {
  const r = { x: t.x, y: e.y };
  if (!Mt([e, r, t], o, n.nodePadding)) return [e, r, t];
  const a = { x: e.x, y: t.y };
  return Mt([e, a, t], o, n.nodePadding) ? null : [e, a, t];
}
function Mt(e, t, o) {
  for (let n = 0; n < e.length - 1; n++) {
    const r = e[n],
      a = e[n + 1];
    for (const d of t) if (Vn(r, a, d, o)) return !0;
  }
  return !1;
}
function Vn(e, t, o, n) {
  const r = o.position.x - n,
    a = o.position.x + (o.width ?? 0) + n,
    d = o.position.y - n,
    p = o.position.y + (o.height ?? 0) + n;
  if (e.y === t.y) {
    const l = e.y,
      c = Math.min(e.x, t.x),
      u = Math.max(e.x, t.x);
    return l >= d && l <= p && u >= r && c <= a;
  }
  if (e.x === t.x) {
    const l = e.x,
      c = Math.min(e.y, t.y),
      u = Math.max(e.y, t.y);
    return l >= r && l <= a && u >= d && c <= p;
  }
  return qn(e, t, { left: r, right: a, top: d, bottom: p });
}
function qn(e, t, o) {
  const n = t.x - e.x,
    r = t.y - e.y,
    a = [-n, n, -r, r],
    d = [e.x - o.left, o.right - e.x, e.y - o.top, o.bottom - e.y];
  let p = 0,
    l = 1;
  for (let c = 0; c < 4; c++)
    if (a[c] === 0) {
      if (d[c] < 0) return !1;
    } else {
      const u = d[c] / a[c];
      a[c] < 0 ? (p = Math.max(p, u)) : (l = Math.min(l, u));
    }
  return p <= l;
}
function Zn(e, t, o, n) {
  let r = Math.min(e.x, t.x),
    a = Math.min(e.y, t.y),
    d = Math.max(e.x, t.x),
    p = Math.max(e.y, t.y);
  for (const c of o)
    (r = Math.min(r, c.position.x)),
      (a = Math.min(a, c.position.y)),
      (d = Math.max(d, c.position.x + (c.width ?? 0))),
      (p = Math.max(p, c.position.y + (c.height ?? 0)));
  const l = n.graphPadding + n.nodePadding;
  return {
    minX: r - l,
    minY: a - l,
    maxX: d + l,
    maxY: p + l,
  };
}
function Qn(e, t, o) {
  let n = o.gridCellSize,
    r = Math.ceil((e.maxX - e.minX) / n),
    a = Math.ceil((e.maxY - e.minY) / n);
  if (r > ye || a > ye) {
    const p = Math.max(r / ye, a / ye);
    (n = n * p),
      (r = Math.ceil((e.maxX - e.minX) / n)),
      (a = Math.ceil((e.maxY - e.minY) / n));
  }
  if (r > ye || a > ye) return null;
  const d = Array.from({ length: a }, () => Array(r).fill(!0));
  for (const p of t) {
    const l = p.position.x - o.nodePadding,
      c = p.position.y - o.nodePadding,
      u = p.position.x + (p.width ?? 0) + o.nodePadding,
      f = p.position.y + (p.height ?? 0) + o.nodePadding,
      m = Math.max(0, Math.floor((l - e.minX) / n)),
      y = Math.max(0, Math.floor((c - e.minY) / n)),
      x = Math.min(r, Math.ceil((u - e.minX) / n)),
      b = Math.min(a, Math.ceil((f - e.minY) / n));
    for (let $ = y; $ < b; $++) for (let v = m; v < x; v++) d[$][v] = !1;
  }
  return d;
}
function _t(e, t, o) {
  return {
    x: Math.floor((e.x - t.minX) / o),
    y: Math.floor((e.y - t.minY) / o),
  };
}
function Jn(e, t, o) {
  return {
    x: t.minX + (e.x + 0.5) * o,
    y: t.minY + (e.y + 0.5) * o,
  };
}
function je(e, t) {
  return e.y >= 0 && e.y < t.length && e.x >= 0 && e.x < t[0].length;
}
function Lt(e, t) {
  if (je(e, t) && t[e.y][e.x]) return e;
  const o = /* @__PURE__ */ new Set(),
    n = [e];
  for (; n.length > 0 && o.size < 500; ) {
    const r = n.shift(),
      a = `${r.x},${r.y}`;
    if (!o.has(a)) {
      if ((o.add(a), je(r, t) && t[r.y][r.x])) return r;
      for (const { dx: d, dy: p } of To) n.push({ x: r.x + d, y: r.y + p });
    }
  }
  return null;
}
function es(e) {
  if (e.length <= 2) return e;
  const t = [e[0]];
  for (let o = 1; o < e.length - 1; o++) {
    const n = t[t.length - 1],
      r = e[o],
      a = e[o + 1],
      d = n.x === r.x && r.x === a.x,
      p = n.y === r.y && r.y === a.y;
    !d && !p && t.push(r);
  }
  return t.push(e[e.length - 1]), t;
}
function Oe(e, t = 8) {
  if (e.length < 2) return "";
  let o = `M ${e[0].x},${e[0].y}`;
  for (let n = 1; n < e.length - 1; n++) {
    const r = e[n - 1],
      a = e[n],
      d = e[n + 1],
      p = { x: r.x - a.x, y: r.y - a.y },
      l = { x: d.x - a.x, y: d.y - a.y },
      c = Math.sqrt(p.x ** 2 + p.y ** 2),
      u = Math.sqrt(l.x ** 2 + l.y ** 2),
      f = Math.min(t, c / 2, u / 2);
    if (f > 0 && c > 0 && u > 0) {
      const m = a.x + (p.x / c) * f,
        y = a.y + (p.y / c) * f,
        x = a.x + (l.x / u) * f,
        b = a.y + (l.y / u) * f;
      (o += ` L ${m},${y}`), (o += ` Q ${a.x},${a.y} ${x},${b}`);
    } else o += ` L ${a.x},${a.y}`;
  }
  return (o += ` L ${e[e.length - 1].x},${e[e.length - 1].y}`), o;
}
function ts(e, t, o) {
  if (o === 0) return e;
  switch (t) {
    case "left":
    case "right":
      return { x: e.x, y: e.y + o };
    case "top":
    case "bottom":
      return { x: e.x + o, y: e.y };
    default:
      return e;
  }
}
const os = new bo(),
  Ie = 180,
  Ve = 40,
  ko = 70,
  ns = 30,
  So = 50,
  Io = 26,
  P = 20,
  J = 35,
  Pt = 15,
  qe = 12,
  Rt = 50,
  rt = 40;
function ss(e) {
  const t = /* @__PURE__ */ new Map();
  function o(n, r) {
    const a = t.get(n);
    a ? a.add(r) : t.set(n, /* @__PURE__ */ new Set([r]));
  }
  for (const n of e) o(n.source, n.target), o(n.target, n.source);
  return {
    get: (n) => t.get(n),
  };
}
async function rs(e, t) {
  const o = [...e],
    n = [...t],
    r = ss(t),
    a = o.filter((p) => p.type === "group" && p.id.startsWith("service-group"));
  for (const p of a) {
    const l = p.id.replace("service-group", ""),
      c = `inbound-subflow${l}`,
      u = `outbound-subflow${l}`;
    Dt(o, r, c), Dt(o, r, u), it(o, c), it(o, u);
  }
  const d = o.filter(
    (p) => p.type === "group" && p.id.startsWith("transport-")
  );
  for (const p of d) await as(o, n, p.id), it(o, p.id);
  return ps(o, a, d), us(o, n), ms(o), { nodes: o, edges: n };
}
function Dt(e, t, o) {
  const n = e.filter(
    (f) => f.parentId === o && (f.type === "compact" || f.type === "route")
  );
  if (n.length === 0) return;
  const r = [],
    a = [],
    d = [];
  for (const f of n) {
    const m = f.data?.elkLayer;
    m === "FIRST" ? r.push(f) : m === "LAST" ? d.push(f) : a.push(f);
  }
  const p = P,
    l = r.length > 0 ? p + Ie + Rt : p,
    c = a.length > 0 ? l + ko + Rt : l;
  let u = is(e, d, c);
  (u = zt(e, a, l, u, t)), zt(e, r, p, u, t);
}
function is(e, t, o) {
  const n = [];
  let r = P + J;
  for (const a of t) {
    const d = e.findIndex((p) => p.id === a.id);
    d !== -1 &&
      ((e[d] = { ...e[d], position: { x: o, y: r } }),
      (r += Fe(a) + qe),
      n.push(a));
  }
  return n;
}
function zt(e, t, o, n, r) {
  const a = [];
  t = t.sort((l, c) => {
    const u = r.get(l.id),
      f = r.get(c.id),
      m = u !== void 0 ? n.findIndex((x) => u.has(x.id)) : -1,
      y = f !== void 0 ? n.findIndex((x) => f.has(x.id)) : -1;
    return m === y ? 0 : m < 0 ? 1 : y < 0 || m < y ? -1 : 1;
  });
  const d = /* @__PURE__ */ new Map();
  for (const l of n) {
    const c = e.find((u) => u.id === l.id);
    c &&
      d.set(l.id, {
        y: c.position.y,
        h: Fe(l),
      });
  }
  let p = P + J;
  for (const l of t) {
    const c = Fe(l),
      u = r.get(l.id);
    let f = p;
    if (u) {
      for (const x of n)
        if (u.has(x.id)) {
          const b = d.get(x.id);
          b && (f = b.y + b.h / 2 - c / 2);
          break;
        }
    }
    const m = Math.max(f, p),
      y = e.findIndex((x) => x.id === l.id);
    y !== -1 && (e[y] = { ...e[y], position: { x: o, y: m } }),
      (p = m + c + qe),
      a.push(l);
  }
  return a;
}
async function as(e, t, o) {
  const n = e.filter(
    (c) => c.parentId === o && (c.type === "compact" || c.type === "route")
  );
  if (n.length === 0) return;
  const r = new Set(n.map((c) => c.id)),
    a = t.filter((c) => r.has(c.source) && r.has(c.target)),
    d = n.map((c) => {
      const u = c.data?.elkLayer,
        f = c.data?.nodeType;
      let m = Ie,
        y = Ve;
      return (
        (c.type === "route" || f === "binding") && ((m = So), (y = Io)),
        {
          id: c.id,
          width: m,
          height: y,
          layoutOptions: u
            ? { "elk.layered.layering.layerConstraint": u }
            : void 0,
        }
      );
    }),
    p = a.map((c) => ({
      id: c.id,
      sources: [c.source],
      targets: [c.target],
    })),
    l = {
      id: o,
      layoutOptions: {
        "elk.algorithm": "layered",
        "elk.direction": "RIGHT",
        "elk.spacing.nodeNode": "35",
        "elk.layered.spacing.nodeNodeBetweenLayers": "80",
        "elk.layered.spacing.edgeNodeBetweenLayers": "35",
        "elk.layered.spacing.edgeEdgeBetweenLayers": "20",
        "elk.layered.nodePlacement.strategy": "NETWORK_SIMPLEX",
        "elk.layered.nodePlacement.bk.fixedAlignment": "BALANCED",
        "elk.layered.crossingMinimization.strategy": "LAYER_SWEEP",
        "elk.layered.considerModelOrder.strategy": "NODES_AND_EDGES",
        "elk.edgeRouting": "ORTHOGONAL",
      },
      children: d,
      edges: p,
    };
  try {
    const c = await os.layout(l);
    if (!c.children) return;
    const u = /* @__PURE__ */ new Map();
    for (const f of c.children) {
      const m = (f.x ?? 0) + P,
        y = (f.y ?? 0) + P + J;
      u.set(f.id, { x: m, y, h: f.height ?? Ve });
      const x = e.findIndex((b) => b.id === f.id);
      x !== -1 && (e[x] = { ...e[x], position: { x: m, y } });
    }
    if (c.edges)
      for (const f of c.edges) {
        if (!f.sections || f.sections.length === 0) continue;
        const m = f.sections[0],
          y = [];
        if (
          (y.push({
            x: m.startPoint.x + P,
            y: m.startPoint.y + P + J,
          }),
          m.bendPoints)
        )
          for (const b of m.bendPoints)
            y.push({
              x: b.x + P,
              y: b.y + P + J,
            });
        y.push({
          x: m.endPoint.x + P,
          y: m.endPoint.y + P + J,
        });
        const x = t.findIndex((b) => b.id === f.id);
        x !== -1 &&
          (t[x] = {
            ...t[x],
            type: "elkRouted",
            data: {
              ...t[x].data,
              _elkLocalPoints: y,
              _elkGroupId: o,
            },
          });
      }
    cs(e, n, a, u);
  } catch (c) {
    console.error(`ELK layout failed for ${o}:`, c), ls(e, n);
  }
}
function cs(e, t, o, n) {
  const r = /* @__PURE__ */ new Set();
  for (const p of o) r.add(p.source), r.add(p.target);
  let a = null,
    d = P + J;
  for (const p of t)
    if (p.data?.elkLayer === "LAST" && r.has(p.id)) {
      const c = n.get(p.id);
      c && ((a = c.x), (d = Math.max(d, c.y + c.h + qe)));
    }
  if (a !== null) {
    for (const p of t)
      if (p.data?.elkLayer === "LAST" && !r.has(p.id)) {
        const c = e.findIndex((u) => u.id === p.id);
        c !== -1 &&
          ((e[c] = { ...e[c], position: { x: a, y: d } }), (d += Fe(p) + qe));
      }
  }
}
function ls(e, t, o) {
  const n = Math.ceil(Math.sqrt(t.length));
  t.forEach((r, a) => {
    const d = a % n,
      p = Math.floor(a / n),
      l = e.findIndex((c) => c.id === r.id);
    l !== -1 &&
      (e[l] = {
        ...e[l],
        position: {
          x: P + d * (Ie + 40),
          y: P + J + p * (Ve + 30),
        },
      });
  });
}
function ds(e) {
  const t = e.data?.nodeType;
  return e.type === "route" ? (t === "binding" ? So : ko) : Ie;
}
function Fe(e) {
  const t = e.data?.nodeType;
  return e.type === "route" ? (t === "binding" ? Io : ns) : Ve;
}
function it(e, t) {
  const o = e.filter(
    (p) => p.parentId === t && (p.type === "compact" || p.type === "route")
  );
  let n, r;
  if (o.length === 0) (n = Ie + P * 2), (r = J + P * 2);
  else {
    let p = 0,
      l = 0;
    for (const c of o) {
      const u = ds(c),
        f = Fe(c);
      (p = Math.max(p, c.position.x + u)), (l = Math.max(l, c.position.y + f));
    }
    (n = p + P), (r = l + P);
  }
  const a = e.findIndex((p) => p.id === t);
  a !== -1 &&
    (e[a] = {
      ...e[a],
      style: { ...e[a].style, width: n, height: r },
    });
  const d = e.findIndex((p) => p.id === `${t}-title`);
  d !== -1 &&
    (e[d] = {
      ...e[d],
      position: { x: n / 2, y: 0 },
    });
}
function ps(e, t, o) {
  const n = [...t].sort((u, f) => u.id.localeCompare(f.id)),
    r = [];
  for (const u of n) {
    const f = u.id.replace("service-group", ""),
      m = `inbound-subflow${f}`,
      y = `outbound-subflow${f}`,
      x = e.find((_) => _.id === m),
      b = e.find((_) => _.id === y),
      $ = Ie + P * 2,
      v = J + P * 2,
      T = x?.style?.width ?? $,
      C = x?.style?.height ?? v,
      w = b?.style?.width ?? $,
      I = b?.style?.height ?? v,
      O = e.findIndex((_) => _.id === m);
    O !== -1 &&
      (e[O] = {
        ...e[O],
        position: { x: P, y: J + 15 },
      });
    const H = e.findIndex((_) => _.id === y);
    H !== -1 &&
      (e[H] = {
        ...e[H],
        position: {
          x: P,
          y: J + 15 + C + Pt,
        },
      });
    const B = Math.max(T, w);
    if (O !== -1) {
      e[O] = {
        ...e[O],
        style: { ...e[O].style, width: B },
      };
      const _ = e.findIndex((X) => X.id === `${m}-title`);
      _ !== -1 &&
        (e[_] = {
          ...e[_],
          position: { x: B / 2, y: 0 },
        });
    }
    if (H !== -1) {
      e[H] = {
        ...e[H],
        style: { ...e[H].style, width: B },
      };
      const _ = e.findIndex((X) => X.id === `${y}-title`);
      _ !== -1 &&
        (e[_] = {
          ...e[_],
          position: { x: B / 2, y: 0 },
        });
    }
    const Q = B + P * 2,
      M = J + 15 + C + Pt + I + P,
      K = e.findIndex((_) => _.id === u.id);
    K !== -1 &&
      (e[K] = {
        ...e[K],
        style: { ...e[K].style, width: Q, height: M },
      });
    const ae = `service-title${f}`,
      Z = e.findIndex((_) => _.id === ae);
    Z !== -1 &&
      (e[Z] = {
        ...e[Z],
        position: { x: Q / 2, y: 0 },
      }),
      r.push({ w: Q, h: M });
  }
  let a = P,
    d = 0;
  for (let u = 0; u < n.length; u++) {
    const f = e.findIndex((m) => m.id === n[u].id);
    f !== -1 &&
      (e[f] = {
        ...e[f],
        position: { x: P, y: a },
      }),
      (d = Math.max(d, r[u].w)),
      (a += r[u].h + rt);
  }
  const p = P + d + rt;
  let l = P;
  const c = [...o].sort((u, f) => u.id.localeCompare(f.id));
  for (const u of c) {
    const f = e.findIndex((m) => m.id === u.id);
    if (f !== -1) {
      e[f] = {
        ...e[f],
        position: { x: p, y: l },
      };
      const m = e[f].style?.height ?? 400;
      l += m + rt;
    }
  }
}
function us(e, t) {
  const o = /* @__PURE__ */ new Map();
  for (const n of e) n.type === "group" && o.set(n.id, n.position);
  for (let n = 0; n < t.length; n++) {
    const r = t[n],
      a = r.data;
    if (!a?._elkLocalPoints || !a?._elkGroupId) continue;
    const d = a._elkLocalPoints,
      p = a._elkGroupId,
      l = o.get(p);
    if (!l) continue;
    const c = d.map((x) => ({
        x: x.x + l.x,
        y: x.y + l.y,
      })),
      u = Oe(c, 8),
      { _elkLocalPoints: f, _elkGroupId: m, ...y } = a;
    t[n] = {
      ...r,
      data: { ...y, elkPath: u, elkPoints: c },
    };
  }
}
const fs = 95,
  hs = 90,
  Ot = 40,
  gs = 160;
function ms(e) {
  const t = [],
    o = [];
  for (let l = 0; l < e.length; l++) {
    const c = e[l];
    c.type === "summaryService"
      ? t.push(l)
      : c.type === "summaryTransport" && o.push(l);
  }
  if (t.length === 0 && o.length === 0) return;
  let n = 1 / 0,
    r = 1 / 0;
  for (const l of e)
    l.type === "group" &&
      ((n = Math.min(n, l.position.x)), (r = Math.min(r, l.position.y)));
  isFinite(n) || (n = 0), isFinite(r) || (r = 0);
  let a = r + P;
  for (const l of t)
    (e[l] = {
      ...e[l],
      position: { x: n + P, y: a },
    }),
      (a += fs + Ot);
  const d = n + P + 520 + gs;
  let p = r + P;
  for (const l of o)
    (e[l] = {
      ...e[l],
      position: { x: d, y: p },
    }),
      (p += hs + Ot);
}
function xs(e, t, o, n) {
  const r = /* @__PURE__ */ new Set(),
    a = /* @__PURE__ */ new Set(),
    d = /* @__PURE__ */ new Set(),
    p = /* @__PURE__ */ new Map(),
    l = /* @__PURE__ */ new Map(),
    c = [],
    u = /* @__PURE__ */ new Set(),
    f = bs(o),
    m = $s(n),
    y = [...e.activities].sort(
      (v, T) =>
        new Date(v.startTime).getTime() - new Date(T.startTime).getTime()
    );
  for (const v of y) {
    const T = ys(v, f, o);
    if (!T || !o.some((w) => w.id === T)) continue;
    p.set(v.id, T),
      r.has(T) || c.push(T),
      r.add(T),
      v.status === "error" && u.add(T);
    const C = l.get(T);
    C ? C.push(v) : l.set(T, [v]);
  }
  vs(e, t, p, a, d, m, n);
  const x = /* @__PURE__ */ new Set([...r, ...a]);
  for (const v of n) x.has(v.source) && x.has(v.target) && d.add(v.id);
  const b = /* @__PURE__ */ new Set();
  for (const v of o) {
    if (x.has(v.id)) continue;
    let T = !1,
      C = !1;
    const w = [];
    for (const I of n)
      I.target === v.id && x.has(I.source) && ((T = !0), w.push(I.id)),
        I.source === v.id && x.has(I.target) && ((C = !0), w.push(I.id));
    if (T && C) {
      b.add(v.id), x.add(v.id);
      for (const I of w) d.add(I);
    }
  }
  const $ = /* @__PURE__ */ new Set([...r, ...a, ...b]);
  return {
    observedNodeIds: r,
    inferredNodeIds: a,
    nodeIds: $,
    edgeIds: d,
    activityToNodeId: p,
    nodeIdToActivities: l,
    sequenceNodeIds: c,
    errorNodeIds: u,
  };
}
function ys(e, t, o) {
  switch (e.operation) {
    case "publish":
    case "send":
    case "request":
    case "reply":
    case "subscribe":
      return t.get(e.messageTypeIdentity) ?? null;
    case "dispatch":
      return `dispatch-${e.endpointName}`;
    case "receive":
      return `receive-${e.endpointName}`;
    case "consume":
      return (
        o.find((r) => {
          const a = r.data,
            d = a.fullData;
          return d
            ? a.nodeType === "saga"
              ? d.consumerName === e.consumerName
              : a.nodeType === "consumer"
              ? d.name === e.consumerName
              : !1
            : !1;
        })?.id ?? null
      );
    case "saga-transition":
      return `saga-${e.sagaName}`;
  }
}
function bs(e) {
  const t = /* @__PURE__ */ new Map();
  for (const o of e) {
    const n = o.data;
    if (n.nodeType === "message" && n.fullData) {
      const r = n.fullData;
      typeof r.identity == "string" && t.set(r.identity, o.id);
    }
  }
  return t;
}
function $s(e) {
  const t = /* @__PURE__ */ new Map();
  for (const o of e) {
    const n = t.get(o.source),
      r = { nodeId: o.target, edgeId: o.id };
    n ? n.push(r) : t.set(o.source, [r]);
  }
  return t;
}
function vs(e, t, o, n, r, a, d) {
  const p = e.activities.filter((c) => c.operation === "dispatch"),
    l = e.activities.filter((c) => c.operation === "receive");
  for (const c of p) {
    if (c.operation !== "dispatch") continue;
    const u = o.get(c.id);
    if (!u) continue;
    const f = ws(t, c.endpointName);
    if (!f) continue;
    const m = Te(f.destinationAddress);
    if (!m) continue;
    const y = d.find((x) => x.source === u && x.target === m);
    y && r.add(y.id);
    for (const x of l) {
      if (
        x.operation !== "receive" ||
        x.messageTypeIdentity !== c.messageTypeIdentity
      )
        continue;
      const b = o.get(x.id);
      if (!b) continue;
      const $ = Ts(t, x.endpointName);
      if (!$) continue;
      const v = Te($.sourceAddress);
      if (!v) continue;
      const T = d.find((w) => w.source === v && w.target === b);
      T && r.add(T.id);
      const C = ks(m, v, a);
      if (C) {
        for (const w of C) n.add(w.nodeId), r.add(w.edgeId);
        n.add(m);
      }
    }
  }
}
function ws(e, t) {
  for (const o of e.transports)
    for (const n of o.dispatchEndpoints)
      if (n.name === t) return { destinationAddress: n.destination.address };
  return null;
}
function Ts(e, t) {
  for (const o of e.transports)
    for (const n of o.receiveEndpoints)
      if (n.name === t) return { sourceAddress: n.source.address };
  return null;
}
function ks(e, t, o) {
  if (e === t) return [];
  const n = /* @__PURE__ */ new Set([e]),
    r = [],
    a = o.get(e);
  if (a)
    for (const d of a)
      n.has(d.nodeId) ||
        (n.add(d.nodeId),
        r.push({
          nodeId: d.nodeId,
          path: [d],
        }));
  for (; r.length > 0; ) {
    const d = r.shift();
    if (d.nodeId === t) return d.path;
    const p = o.get(d.nodeId);
    if (p)
      for (const l of p)
        n.has(l.nodeId) ||
          (n.add(l.nodeId),
          r.push({
            nodeId: l.nodeId,
            path: [...d.path, l],
          }));
  }
  return null;
}
function Ss(e) {
  const t = [];
  for (const o of e) {
    const n = o.data;
    if (o.type === "compact" && "nodeType" in n) {
      const r = n,
        a = r.fullData;
      let d;
      if (a) {
        const l = a.identity,
          c = a.runtimeType,
          u = a.name;
        d = c || l || u;
      }
      const p = r.nodeType;
      t.push({
        nodeId: o.id,
        label: r.label,
        description: d !== r.label ? d : void 0,
        category: p,
        nodeType: r.nodeType,
        subType: r.subType,
      });
    }
    if (o.type === "route" && "kind" in n) {
      const r = n;
      t.push({
        nodeId: o.id,
        label: r.label,
        description: r.kind,
        category: "route",
        nodeType: "route",
        subType: r.direction,
      });
    }
  }
  return t;
}
const Is = {
  publish: s.colors.accent.fg,
  send: s.colors.accent.fg,
  dispatch: s.colors.accent.fg,
  receive: s.colors.success.fg,
  consume: "#39d2c0",
  // teal
  "saga-transition": "#39d2c0",
  reply: s.colors.done.fg,
  request: s.colors.accent.fg,
  subscribe: s.colors.accent.fg,
};
function Ft(e) {
  return Is[e] ?? s.colors.fg.muted;
}
function At(e) {
  switch (e.operation) {
    case "publish":
      return `publish ${e.messageType}`;
    case "send":
      return `send ${e.messageType}`;
    case "dispatch":
      return `dispatch ${e.endpointName}`;
    case "receive":
      return `receive ${e.endpointName}`;
    case "consume":
      return `consume ${e.consumerName}`;
    case "saga-transition":
      return `${e.sagaName}: ${e.fromState} → ${e.toState}`;
    case "reply":
      return `reply ${e.messageType}`;
    case "request":
      return `request ${e.messageType}`;
    case "subscribe":
      return `subscribe ${e.messageType}`;
  }
}
const Es = /* @__PURE__ */ new Set();
function Ns({
  trace: e,
  hoveredActivityIds: t = Es,
  onActivityHover: o,
  onActivityClick: n,
}) {
  const [r, a] = Y(!1),
    [d, p] = Y(null),
    [l, c] = Y(!1),
    u = U(() => Cs(e), [e]),
    f = U(
      () =>
        [...e.activities].sort(
          ($, v) =>
            new Date($.startTime).getTime() - new Date(v.startTime).getTime()
        ),
      [e]
    ),
    m = G(() => {
      if (l) {
        c(!1);
        return;
      }
      c(!0), (d === null || d >= f.length - 1) && (p(0), f[0] && o(f[0]));
    }, [l, d, f, o]),
    y = G(() => {
      p(($) => {
        const v = ($ ?? -1) + 1;
        return v < f.length ? v : $;
      }),
        c(!1);
    }, [f.length]),
    x = G(() => {
      p(($) => {
        const v = ($ ?? 1) - 1;
        return v >= 0 ? v : 0;
      }),
        c(!1);
    }, []);
  ie(() => {
    if (!l || d === null) return;
    const $ = setTimeout(() => {
      if (d < f.length - 1) {
        const v = d + 1;
        p(v), o(f[v]);
      } else c(!1);
    }, 800);
    return () => clearTimeout($);
  }, [l, d, f, o]);
  const b = U(() => {
    if (e.activities.length === 0) return 0;
    const $ = e.activities.map(
        (T) => new Date(T.startTime).getTime() + T.durationMs
      ),
      v = e.activities.map((T) => new Date(T.startTime).getTime());
    return Math.max(...$) - Math.min(...v);
  }, [e]);
  return /* @__PURE__ */ g(Ms, {
    $collapsed: r,
    children: [
      /* @__PURE__ */ g(_s, {
        onClick: () => a(!r),
        children: [
          /* @__PURE__ */ g(Ls, {
            children: [
              /* @__PURE__ */ i(z, {
                icon: r ? ao : co,
                size: "xs",
              }),
              /* @__PURE__ */ i(Ps, { children: "Trace Timeline" }),
              /* @__PURE__ */ i(Rs, { children: e.traceId }),
              /* @__PURE__ */ g(Ds, {
                children: [
                  e.activities.length,
                  " activities ·",
                  " ",
                  b.toFixed(0),
                  "ms",
                ],
              }),
            ],
          }),
          /* @__PURE__ */ g(zs, {
            onClick: ($) => $.stopPropagation(),
            children: [
              d !== null &&
                /* @__PURE__ */ g(Os, { children: [d + 1, "/", f.length] }),
              /* @__PURE__ */ i(at, {
                onClick: x,
                title: "Step backward",
                "aria-label": "Step backward",
                children: /* @__PURE__ */ i(z, { icon: pn, size: "xs" }),
              }),
              /* @__PURE__ */ i(at, {
                onClick: m,
                title: l ? "Pause" : "Play",
                "aria-label": l ? "Pause" : "Play",
                children: /* @__PURE__ */ i(z, {
                  icon: l ? un : fn,
                  size: "xs",
                }),
              }),
              /* @__PURE__ */ i(at, {
                onClick: y,
                title: "Step forward",
                "aria-label": "Step forward",
                children: /* @__PURE__ */ i(z, { icon: hn, size: "xs" }),
              }),
            ],
          }),
        ],
      }),
      !r &&
        /* @__PURE__ */ g(Fs, {
          children: [
            u.length === 0 &&
              /* @__PURE__ */ i(As, {
                children: "No activities recorded for this trace.",
              }),
            u.map(($) =>
              /* @__PURE__ */ g(
                Hs,
                {
                  $depth: $.depth,
                  $hovered: t.has($.activity.id),
                  $playing: d !== null && f[d]?.id === $.activity.id,
                  onMouseEnter: () => o($.activity),
                  onMouseLeave: () => o(null),
                  onClick: () => n($.activity),
                  children: [
                    /* @__PURE__ */ g(Bs, {
                      $depth: $.depth,
                      children: [
                        $.activity.status === "error" &&
                          /* @__PURE__ */ i(Ys, {
                            children: /* @__PURE__ */ i(z, {
                              icon: gn,
                              size: "xs",
                            }),
                          }),
                        /* @__PURE__ */ i(Us, {
                          $color: Ft($.activity.operation),
                          title: $.activity.operation,
                        }),
                        /* @__PURE__ */ i(Ws, {
                          title: At($.activity),
                          children: At($.activity),
                        }),
                      ],
                    }),
                    /* @__PURE__ */ g(Ks, {
                      children: [
                        /* @__PURE__ */ i(Xs, {
                          $left: $.offsetPercent,
                          $width: Math.max($.widthPercent, 0.5),
                          $color: Ft($.activity.operation),
                          $error: $.activity.status === "error",
                        }),
                        $.activity.durationMs > 0 &&
                          /* @__PURE__ */ g(js, {
                            style: {
                              left: `${
                                $.offsetPercent + $.widthPercent + 0.5
                              }%`,
                            },
                            children: [$.activity.durationMs, "ms"],
                          }),
                      ],
                    }),
                  ],
                },
                $.activity.id
              )
            ),
          ],
        }),
    ],
  });
}
function Cs(e) {
  if (e.activities.length === 0) return [];
  const t = e.activities,
    o = t.map((f) => new Date(f.startTime).getTime()),
    n = t.map((f) => new Date(f.startTime).getTime() + f.durationMs),
    r = Math.min(...o),
    d = Math.max(...n) - r || 1,
    p = new Set(t.map((f) => f.id)),
    l = /* @__PURE__ */ new Map();
  for (const f of t) {
    const m = f.parentId !== null && p.has(f.parentId) ? f.parentId : null,
      y = l.get(m);
    y ? y.push(f) : l.set(m, [f]);
  }
  const c = [];
  function u(f, m) {
    const y = [...(l.get(f) ?? [])];
    y.sort(
      (x, b) =>
        new Date(x.startTime).getTime() - new Date(b.startTime).getTime()
    );
    for (const x of y) {
      const $ = ((new Date(x.startTime).getTime() - r) / d) * 100,
        v = (x.durationMs / d) * 100;
      c.push({
        activity: x,
        depth: m,
        offsetPercent: $,
        widthPercent: v,
      }),
        u(x.id, m + 1);
    }
  }
  return u(null, 0), c;
}
const Ms = h.div`
  position: absolute;
  bottom: 0;
  left: 0;
  right: 0;
  background: ${s.colors.canvas.subtle};
  border-top: 1px solid ${s.colors.border.default};
  z-index: 10;
  display: flex;
  flex-direction: column;
  max-height: ${(e) => (e.$collapsed ? "36px" : "280px")};
  transition: max-height 0.2s ease;
`,
  _s = h.div`
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 8px 12px;
  cursor: pointer;
  user-select: none;
  border-bottom: 1px solid ${s.colors.border.muted};
  flex-shrink: 0;

  &:hover {
    background: ${s.colors.canvas.inset};
  }
`,
  Ls = h.div`
  display: flex;
  align-items: center;
  gap: 8px;
`,
  Ps = h.span`
  font-size: 12px;
  font-weight: 600;
  color: ${s.colors.fg.default};
`,
  Rs = h.span`
  font-size: 10px;
  font-family: ${s.fonts.mono};
  color: ${s.colors.fg.subtle};
  max-width: 120px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
`,
  Ds = h.span`
  font-size: 10px;
  color: ${s.colors.fg.muted};
`,
  zs = h.div`
  display: flex;
  align-items: center;
  gap: 4px;
`,
  Os = h.span`
  font-size: 10px;
  font-family: ${s.fonts.mono};
  color: ${s.colors.fg.subtle};
  margin-right: 4px;
`,
  at = h.button`
  background: none;
  border: 1px solid ${s.colors.border.default};
  color: ${s.colors.fg.muted};
  cursor: pointer;
  padding: 3px 6px;
  border-radius: 4px;
  font-size: 10px;
  display: flex;
  align-items: center;

  &:hover {
    background: ${s.colors.canvas.inset};
    color: ${s.colors.fg.default};
  }
`,
  Fs = h.div`
  flex: 1;
  overflow-y: auto;
  padding: 4px 0;
`,
  As = h.div`
  padding: 16px 12px;
  font-size: 11px;
  color: ${s.colors.fg.muted};
  text-align: center;
`,
  Gs = Se`
  0%, 100% { opacity: 1; }
  50% { opacity: 0.6; }
`,
  Hs = h.div`
  display: flex;
  align-items: center;
  height: 24px;
  padding: 0 12px;
  cursor: pointer;
  font-size: 11px;

  ${(e) =>
    e.$hovered &&
    F`
      background: rgba(88, 166, 255, 0.1);
    `}

  ${(e) =>
    e.$playing &&
    F`
      background: rgba(88, 166, 255, 0.15);
      animation: ${Gs} 1s ease-in-out infinite;
    `}

  &:hover {
    background: rgba(88, 166, 255, 0.08);
  }
`,
  Bs = h.div`
  display: flex;
  align-items: center;
  gap: 4px;
  min-width: 240px;
  max-width: 240px;
  padding-left: ${(e) => e.$depth * 16}px;
  overflow: hidden;
`,
  Ys = h.span`
  color: ${s.colors.danger.fg};
  flex-shrink: 0;
`,
  Us = h.span`
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: ${(e) => e.$color};
  flex-shrink: 0;
`,
  Ws = h.span`
  color: ${s.colors.fg.default};
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
`,
  Ks = h.div`
  flex: 1;
  position: relative;
  height: 14px;
`,
  Xs = h.div`
  position: absolute;
  top: 2px;
  height: 10px;
  left: ${(e) => e.$left}%;
  width: ${(e) => e.$width}%;
  min-width: 3px;
  background: ${(e) => e.$color}33;
  border: 1px solid ${(e) => (e.$error ? s.colors.danger.fg : e.$color)};
  border-radius: 2px;

  ${(e) =>
    e.$error &&
    F`
      background: ${s.colors.danger.fg}22;
    `}
`,
  js = h.span`
  position: absolute;
  top: 3px;
  font-size: 9px;
  font-family: ${s.fonts.mono};
  color: ${s.colors.fg.subtle};
  white-space: nowrap;
`,
  Vs = {
    consumer: Xe,
    saga: ue,
    message: $t,
    endpoint: bt,
    entity: we,
  },
  qs = {
    exchange: po,
    queue: yt,
    topic: lo,
  },
  Be = {
    consumer: s.colors.success.fg,
    saga: s.colors.done.fg,
    message: s.colors.attention.fg,
    endpoint: s.colors.sponsors.fg,
    "endpoint-receive": s.colors.accent.fg,
    entity: s.colors.scale.purple4,
    "entity-exchange": s.colors.attention.fg,
    "entity-queue": s.colors.success.fg,
    "entity-topic": s.colors.accent.fg,
  },
  Zs = h.div`
  position: relative;
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 12px;
  background: ${s.colors.canvas.subtle};
  border: 1px solid ${s.colors.border.default};
  border-radius: 6px;
  font-size: 12px;
  cursor: pointer;
  transition: all 0.15s ease;
  min-width: 120px;
  max-width: 200px;

  &:hover {
    border-color: ${s.colors.border.default};
    background: ${s.colors.canvas.inset};
  }

  ${(e) =>
    e.$selected &&
    F`
    border-color: ${s.colors.accent.fg};
    box-shadow: 0 0 0 1px ${s.colors.accent.fg};
  `}
`,
  Qs = h.span`
  font-size: 14px;
  width: 18px;
  text-align: center;
  flex-shrink: 0;
  color: ${(e) => e.$color};
`,
  Js = h.span`
  font-weight: 500;
  color: ${s.colors.fg.default};
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
`,
  er = h.span`
  position: absolute;
  top: -8px;
  left: -8px;
  width: 18px;
  height: 18px;
  border-radius: 50%;
  background: ${s.colors.accent.fg};
  color: #fff;
  font-size: 10px;
  font-weight: 700;
  display: flex;
  align-items: center;
  justify-content: center;
  line-height: 1;
  z-index: 1;
  pointer-events: none;
`;
function tr({ data: e }) {
  const t =
    e.nodeType === "consumer" && e.isBatch
      ? yt
      : e.nodeType === "entity" && e.entityKind
      ? qs[e.entityKind] || we
      : Vs[e.nodeType] || we;
  let o;
  return (
    e.nodeType === "consumer" && e.isBatch
      ? (o = s.colors.attention.fg)
      : e.nodeType === "entity" && e.entityKind
      ? (o = Be[`entity-${e.entityKind}`] || Be.entity)
      : e.nodeType === "endpoint" && e.subType === "receive"
      ? (o = Be["endpoint-receive"])
      : (o = Be[e.nodeType] || s.colors.fg.muted),
    /* @__PURE__ */ g(Zs, {
      className: "compact-node",
      children: [
        typeof e.traceSequenceNumber == "number" &&
          /* @__PURE__ */ i(er, { children: e.traceSequenceNumber }),
        /* @__PURE__ */ i(W, { type: "target", position: N.Left, id: "left" }),
        /* @__PURE__ */ i(W, {
          type: "source",
          position: N.Left,
          id: "left-source",
        }),
        /* @__PURE__ */ i(W, {
          type: "target",
          position: N.Right,
          id: "right",
        }),
        /* @__PURE__ */ i(W, {
          type: "source",
          position: N.Right,
          id: "right-source",
        }),
        /* @__PURE__ */ i(Qs, {
          $color: o,
          children: /* @__PURE__ */ i(z, { icon: t }),
        }),
        /* @__PURE__ */ i(Js, { title: e.label, children: e.label }),
      ],
    })
  );
}
const or = h.div`
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 4px 10px;
  background: ${s.colors.canvas.inset};
  border: 1px solid ${s.colors.border.default};
  border-radius: 12px;
  font-size: 10px;
  min-width: 60px;
  cursor: pointer;
  transition: border-color 0.15s ease;

  &:hover {
    border-color: ${s.colors.accent.fg};
  }

  ${(e) =>
    e.$isBinding &&
    F`
    background: ${s.colors.canvas.subtle};
    border-color: ${s.colors.scale.purple5};

    &:hover {
      border-color: ${s.colors.scale.purple4};
    }
  `}
`,
  nr = h.span`
  color: ${s.colors.fg.muted};
  font-weight: 500;
  text-transform: lowercase;
`,
  sr = h.span`
  position: absolute;
  top: -8px;
  left: -8px;
  width: 18px;
  height: 18px;
  border-radius: 50%;
  background: ${s.colors.accent.fg};
  color: #fff;
  font-size: 10px;
  font-weight: 700;
  display: flex;
  align-items: center;
  justify-content: center;
  line-height: 1;
  z-index: 1;
  pointer-events: none;
`;
function rr({ data: e }) {
  const o = (e.nodeType || "route") === "binding";
  return /* @__PURE__ */ g(or, {
    className: "route-node",
    $isBinding: o,
    children: [
      typeof e.traceSequenceNumber == "number" &&
        /* @__PURE__ */ i(sr, { children: e.traceSequenceNumber }),
      /* @__PURE__ */ i(W, { type: "target", position: N.Left, id: "left" }),
      /* @__PURE__ */ i(W, {
        type: "source",
        position: N.Left,
        id: "left-source",
      }),
      /* @__PURE__ */ i(W, { type: "target", position: N.Right, id: "right" }),
      /* @__PURE__ */ i(W, {
        type: "source",
        position: N.Right,
        id: "right-source",
      }),
      /* @__PURE__ */ i(nr, { children: e.kind }),
    ],
  });
}
const ir = h.div`
  /* Position label to sit centered on the border line */
  /* translateX(-50%) centers horizontally, translateY(-50%) centers on border */
  transform: translate(-50%, -50%);
  pointer-events: none;
`,
  ar = h.div`
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 6px 14px;
  background: ${s.colors.canvas.default};
  border: 1px solid ${s.colors.border.default};
  border-radius: 6px;
  font-weight: 600;
  font-size: 13px;
  white-space: nowrap;

  ${(e) =>
    e.$type === "service" &&
    F`
    background: ${s.colors.canvas.default};
    border-color: ${s.colors.border.default};
  `}

  ${(e) =>
    e.$type === "transport" &&
    F`
    background: ${s.colors.canvas.default};
    border-color: ${s.colors.done.fg};
  `}
`,
  cr = h.span`
  font-size: 14px;
  color: ${(e) =>
    e.$type === "transport" ? s.colors.done.fg : s.colors.fg.muted};
`,
  lr = h.span`
  color: ${s.colors.fg.default};
`;
function dr({ data: e }) {
  const t = e.type === "service" ? uo : fo;
  return /* @__PURE__ */ i(ir, {
    children: /* @__PURE__ */ g(ar, {
      $type: e.type,
      children: [
        /* @__PURE__ */ i(cr, {
          $type: e.type,
          children: /* @__PURE__ */ i(z, { icon: t }),
        }),
        /* @__PURE__ */ i(lr, { children: e.label }),
      ],
    }),
  });
}
const pr = h.div`
  font-size: 10px;
  font-weight: 600;
  color: ${s.colors.fg.subtle};
  text-transform: uppercase;
  letter-spacing: 0.5px;
  pointer-events: none;
`;
function ur({ data: e }) {
  return /* @__PURE__ */ i(pr, { children: e.label });
}
const fr = h.div`
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 18px 24px;
  background: ${s.colors.canvas.subtle};
  border: 1px solid ${s.colors.border.default};
  border-radius: 10px;
  min-width: 450px;
  max-width: 520px;
  cursor: pointer;
  transition: border-color 0.15s ease, box-shadow 0.15s ease;

  &:hover {
    border-color: ${s.colors.accent.fg};
    box-shadow: 0 0 0 1px ${s.colors.accent.fg};
  }
`,
  hr = h.div`
  display: flex;
  align-items: center;
  gap: 8px;
`,
  gr = h.span`
  font-size: 22px;
  color: ${s.colors.fg.muted};
  width: 26px;
  text-align: center;
  flex-shrink: 0;
`,
  mr = h.span`
  font-weight: 600;
  font-size: 20px;
  color: ${s.colors.fg.default};
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
`,
  xr = h.div`
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 15px;
  color: ${s.colors.fg.muted};
`,
  yr = h.span`
  white-space: nowrap;
`,
  br = h.span`
  color: ${s.colors.border.default};
`;
function $r({ data: e }) {
  const t = [];
  return (
    e.consumerCount > 0 && t.push(`${e.consumerCount} consumers`),
    e.messageCount > 0 && t.push(`${e.messageCount} messages`),
    e.sagaCount > 0 && t.push(`${e.sagaCount} sagas`),
    e.transportNames.length > 0 && t.push(e.transportNames.join(", ")),
    /* @__PURE__ */ g(fr, {
      className: "summary-service-node",
      children: [
        /* @__PURE__ */ i(W, { type: "target", position: N.Left, id: "left" }),
        /* @__PURE__ */ i(W, {
          type: "source",
          position: N.Left,
          id: "left-source",
        }),
        /* @__PURE__ */ i(W, {
          type: "target",
          position: N.Right,
          id: "right",
        }),
        /* @__PURE__ */ i(W, {
          type: "source",
          position: N.Right,
          id: "right-source",
        }),
        /* @__PURE__ */ g(hr, {
          children: [
            /* @__PURE__ */ i(gr, {
              children: /* @__PURE__ */ i(z, { icon: uo }),
            }),
            /* @__PURE__ */ i(mr, { title: e.label, children: e.label }),
          ],
        }),
        t.length > 0 &&
          /* @__PURE__ */ i(xr, {
            children: t.map((o, n) =>
              /* @__PURE__ */ g(
                "span",
                {
                  children: [
                    n > 0 && /* @__PURE__ */ i(br, { children: " | " }),
                    /* @__PURE__ */ i(yr, { children: o }),
                  ],
                },
                n
              )
            ),
          }),
      ],
    })
  );
}
const vr = h.div`
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 18px 24px;
  background: rgba(163, 113, 247, 0.1);
  border: 1px solid ${s.colors.done.fg};
  border-radius: 10px;
  min-width: 380px;
  max-width: 450px;
  cursor: pointer;
  transition: border-color 0.15s ease, box-shadow 0.15s ease;

  &:hover {
    border-color: ${s.colors.scale.purple4};
    box-shadow: 0 0 0 1px ${s.colors.scale.purple4};
  }
`,
  wr = h.div`
  display: flex;
  align-items: center;
  gap: 8px;
`,
  Tr = h.span`
  font-size: 22px;
  color: ${s.colors.done.fg};
  width: 26px;
  text-align: center;
  flex-shrink: 0;
`,
  kr = h.span`
  font-weight: 600;
  font-size: 20px;
  color: ${s.colors.fg.default};
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
`,
  Sr = h.div`
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 15px;
  color: ${s.colors.fg.muted};
`,
  Ir = h.span`
  white-space: nowrap;
`,
  Er = h.span`
  color: ${s.colors.border.default};
`;
function Nr({ data: e }) {
  const t = Object.entries(e.entityCounts),
    o =
      t.length > 0
        ? t.map(([n, r]) => `${r} ${n}s`)
        : [`${e.totalEntityCount} entities`];
  return /* @__PURE__ */ g(vr, {
    className: "summary-transport-node",
    children: [
      /* @__PURE__ */ i(W, { type: "target", position: N.Left, id: "left" }),
      /* @__PURE__ */ i(W, {
        type: "source",
        position: N.Left,
        id: "left-source",
      }),
      /* @__PURE__ */ i(W, { type: "target", position: N.Right, id: "right" }),
      /* @__PURE__ */ i(W, {
        type: "source",
        position: N.Right,
        id: "right-source",
      }),
      /* @__PURE__ */ g(wr, {
        children: [
          /* @__PURE__ */ i(Tr, {
            children: /* @__PURE__ */ i(z, { icon: fo }),
          }),
          /* @__PURE__ */ i(kr, { title: e.label, children: e.label }),
        ],
      }),
      o.length > 0 &&
        /* @__PURE__ */ i(Sr, {
          children: o.map((n, r) =>
            /* @__PURE__ */ g(
              "span",
              {
                children: [
                  r > 0 && /* @__PURE__ */ i(Er, { children: " | " }),
                  /* @__PURE__ */ i(Ir, { children: n }),
                ],
              },
              r
            )
          ),
        }),
    ],
  });
}
function Cr({
  id: e,
  style: t,
  markerStart: o,
  markerEnd: n,
  data: r,
  sourceX: a,
  sourceY: d,
  targetX: p,
  targetY: l,
}) {
  const c = U(() => {
    const u = r?.elkPoints;
    if (!u || u.length < 2) return r?.elkPath ?? `M ${a} ${d} L ${p} ${l}`;
    const f = u[0],
      m = u[u.length - 1],
      y = Math.abs(f.x - a),
      x = Math.abs(f.y - d),
      b = Math.abs(m.x - p),
      $ = Math.abs(m.y - l);
    if (!(y > 2 || x > 2 || b > 2 || $ > 2)) return r?.elkPath ?? Oe(u, 8);
    const T = u.slice();
    return (
      (T[0] = { x: a, y: d }), (T[T.length - 1] = { x: p, y: l }), Oe(T, 8)
    );
  }, [r, a, d, p, l]);
  return /* @__PURE__ */ i(ft, {
    id: e,
    path: c,
    style: t,
    markerStart: o,
    markerEnd: n,
  });
}
function Mr({ data: e }) {
  return /* @__PURE__ */ g(_r, {
    className: "focus-state-node",
    $isInitial: e.isInitial,
    $isFinal: e.isFinal,
    children: [
      /* @__PURE__ */ i(W, { type: "target", position: N.Left }),
      /* @__PURE__ */ g(Lr, {
        children: [
          /* @__PURE__ */ i(Pr, { children: e.label }),
          e.isInitial &&
            /* @__PURE__ */ i(Gt, { $type: "initial", children: "Start" }),
          e.isFinal &&
            /* @__PURE__ */ i(Gt, { $type: "final", children: "End" }),
          e.response && /* @__PURE__ */ g(Rr, { children: ["→ ", e.response] }),
          e.sendActions.length > 0 &&
            /* @__PURE__ */ i(Dr, {
              children: e.sendActions.map((t, o) =>
                /* @__PURE__ */ g(zr, { children: ["↑ ", t] }, o)
              ),
            }),
        ],
      }),
      /* @__PURE__ */ i(W, { type: "source", position: N.Right }),
    ],
  });
}
const _r = h.div`
  min-width: 140px;
  padding: 10px 14px;
  background: ${s.colors.canvas.subtle};
  border: 1px solid ${s.colors.border.default};
  border-radius: 6px;
  border-left: 3px solid ${s.colors.border.default};

  ${(e) =>
    e.$isInitial &&
    F`
      border-left-color: ${s.colors.accent.fg};
      background: rgba(88, 166, 255, 0.06);
    `}

  ${(e) =>
    e.$isFinal &&
    F`
      border-left-color: ${s.colors.success.fg};
      background: rgba(63, 185, 80, 0.06);
    `}

  .react-flow__handle {
    width: 6px;
    height: 6px;
  }
`,
  Lr = h.div`
  display: flex;
  flex-direction: column;
  gap: 3px;
`,
  Pr = h.div`
  font-weight: 600;
  font-size: 13px;
  color: ${s.colors.fg.default};
`,
  Gt = h.span`
  display: inline-block;
  width: fit-content;
  font-size: 8px;
  padding: 1px 5px;
  border-radius: 3px;
  text-transform: uppercase;
  font-weight: 600;

  ${(e) =>
    e.$type === "initial" &&
    F`
      background: ${s.colors.accent.fg};
      color: white;
    `}

  ${(e) =>
    e.$type === "final" &&
    F`
      background: ${s.colors.success.fg};
      color: white;
    `}
`,
  Rr = h.div`
  font-size: 10px;
  color: ${s.colors.fg.muted};
  margin-top: 2px;
`,
  Dr = h.div`
  display: flex;
  flex-direction: column;
  gap: 1px;
  margin-top: 3px;
`,
  zr = h.div`
  font-size: 9px;
  color: ${s.colors.attention.fg};
  font-family: ${s.fonts.mono};
`,
  Eo = {
    request: s.colors.accent.fg,
    reply: s.colors.success.fg,
    event: s.colors.attention.fg,
  };
function Or({
  id: e,
  sourceX: t,
  sourceY: o,
  targetX: n,
  targetY: r,
  sourcePosition: a,
  targetPosition: d,
  data: p,
  markerEnd: l,
}) {
  const c = p,
    u = c?.transitionKind ?? "event",
    f = Eo[u] ?? s.colors.fg.subtle,
    [m, y, x] = nn({
      sourceX: t,
      sourceY: o,
      targetX: n,
      targetY: r,
      sourcePosition: a,
      targetPosition: d,
      borderRadius: 8,
    });
  return /* @__PURE__ */ g(pe, {
    children: [
      /* @__PURE__ */ i(ft, {
        id: e,
        path: m,
        markerEnd: l,
        style: { stroke: f, strokeWidth: 2 },
      }),
      c &&
        /* @__PURE__ */ i(io, {
          children: /* @__PURE__ */ g(Fr, {
            style: {
              transform: `translate(-50%, -50%) translate(${y}px, ${x}px)`,
            },
            className: "nodrag nopan",
            children: [
              /* @__PURE__ */ i(Ar, { children: c.eventType }),
              /* @__PURE__ */ i(Gr, { $kind: u, children: u }),
              c.sendActions.length > 0 &&
                c.sendActions.map((b, $) =>
                  /* @__PURE__ */ g(Hr, { children: ["↑ ", b] }, $)
                ),
            ],
          }),
        }),
    ],
  });
}
const Fr = h.div`
  position: absolute;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 2px;
  background: ${s.colors.canvas.subtle};
  border: 1px solid ${s.colors.border.muted};
  border-radius: 4px;
  padding: 4px 8px;
  pointer-events: all;
  cursor: pointer;
`,
  Ar = h.div`
  font-size: 12px;
  color: ${s.colors.fg.default};
  font-weight: 500;
  white-space: nowrap;
  max-width: 200px;
  overflow: hidden;
  text-overflow: ellipsis;
`,
  Gr = h.span`
  font-size: 8px;
  text-transform: uppercase;
  font-weight: 600;
  letter-spacing: 0.5px;
  color: ${(e) => Eo[e.$kind] ?? s.colors.fg.subtle};
`,
  Hr = h.div`
  font-size: 9px;
  color: ${s.colors.fg.muted};
  font-family: ${s.fonts.mono};
`,
  Br = new bo(),
  Yr = 180,
  Ur = 50,
  Wr = 16,
  Kr = 14,
  Xr = 60,
  jr = 120;
function Vr(e, t) {
  let o = Ur;
  return e.response && (o += Wr), t.length > 0 && (o += t.length * Kr), o;
}
function qr(e, t) {
  const o = [];
  for (const n of t)
    for (const r of n.transitions)
      if (r.transitionTo === e && r.send)
        for (const a of r.send) o.push(a.messageType);
  return o;
}
async function Zr(e) {
  if (e.length === 0) return { nodes: [], edges: [] };
  const t = /* @__PURE__ */ new Map();
  for (const c of e) t.set(c.name, qr(c.name, e));
  const o = e.map((c) => {
      const u = t.get(c.name) ?? [],
        f = Vr(c, u),
        m = {};
      return (
        c.isInitial && (m["elk.layered.layering.layerConstraint"] = "FIRST"),
        c.isFinal && (m["elk.layered.layering.layerConstraint"] = "LAST"),
        {
          id: c.name,
          width: Yr,
          height: f,
          layoutOptions: Object.keys(m).length > 0 ? m : void 0,
        }
      );
    }),
    n = [];
  for (const c of e)
    for (let u = 0; u < c.transitions.length; u++) {
      const f = c.transitions[u];
      n.push({
        id: `${c.name}-${f.transitionTo}-${u}`,
        sources: [c.name],
        targets: [f.transitionTo],
      });
    }
  const r = {
      id: "saga-focus-root",
      layoutOptions: {
        "elk.algorithm": "layered",
        "elk.direction": "RIGHT",
        "elk.spacing.nodeNode": String(Xr),
        "elk.layered.spacing.nodeNodeBetweenLayers": String(jr),
        "elk.layered.nodePlacement.strategy": "NETWORK_SIMPLEX",
        "elk.layered.nodePlacement.bk.fixedAlignment": "BALANCED",
        "elk.layered.crossingMinimization.strategy": "LAYER_SWEEP",
      },
      children: o,
      edges: n,
    },
    a = await Br.layout(r),
    d = /* @__PURE__ */ new Map();
  if (a.children)
    for (const c of a.children) d.set(c.id, { x: c.x ?? 0, y: c.y ?? 0 });
  const p = e.map((c) => {
      const u = d.get(c.name) ?? { x: 0, y: 0 },
        f = t.get(c.name) ?? [],
        m = {
          label: c.name === "__Initial" ? "Initial" : c.name,
          isInitial: c.isInitial,
          isFinal: c.isFinal,
          response: c.response?.eventType,
          sendActions: f,
        };
      return {
        id: c.name,
        type: "focusState",
        position: u,
        data: m,
      };
    }),
    l = [];
  for (const c of e)
    for (let u = 0; u < c.transitions.length; u++) {
      const f = c.transitions[u],
        m = (f.send ?? []).map((x) => x.messageType),
        y = {
          eventType: f.eventType,
          transitionKind: f.transitionKind,
          sendActions: m,
          sourceStateName: c.name,
          transitionIndex: u,
        };
      l.push({
        id: `${c.name}-${f.transitionTo}-${u}`,
        source: c.name,
        target: f.transitionTo,
        type: "focusTransition",
        data: y,
      });
    }
  return { nodes: p, edges: l };
}
const Qr = h.div`
  height: 220px;
  background: ${s.colors.canvas.inset};
  border: 1px solid ${s.colors.border.muted};
  border-radius: 4px;
  overflow: hidden;

  .react-flow__background {
    background: ${s.colors.canvas.inset};
  }
`,
  Jr = h.div`
  width: 100%;
  height: 100%;

  .react-flow__background {
    background: ${s.colors.canvas.default};
  }
`,
  ei = h.div`
  display: flex;
  align-items: center;
  padding: 6px 10px;
  background: ${s.colors.canvas.subtle};
  border: 1px solid ${s.colors.border.default};
  border-radius: 6px;
  font-size: 11px;
  min-width: 80px;

  ${(e) =>
    e.$isInitial &&
    F`
    border-color: ${s.colors.accent.fg};
    background: rgba(88, 166, 255, 0.1);
  `}

  ${(e) =>
    e.$isFinal &&
    F`
    border-color: ${s.colors.success.fg};
    background: rgba(63, 185, 80, 0.1);
  `}

  .react-flow__handle {
    width: 6px;
    height: 6px;
  }
`,
  ti = h.div`
  display: flex;
  flex-direction: column;
  gap: 2px;
`,
  oi = h.span`
  font-weight: 500;
  color: ${s.colors.fg.default};
`,
  Ht = h.span`
  font-size: 8px;
  padding: 1px 4px;
  border-radius: 3px;
  text-transform: uppercase;
  font-weight: 600;

  ${(e) =>
    e.$type === "initial" &&
    F`
    background: ${s.colors.accent.fg};
    color: white;
  `}

  ${(e) =>
    e.$type === "final" &&
    F`
    background: ${s.colors.success.fg};
    color: white;
  `}
`,
  ni = h.div`
  font-size: 9px;
  color: ${s.colors.fg.muted};
  margin-top: 2px;
`;
function si({ data: e }) {
  return /* @__PURE__ */ g(ei, {
    $isInitial: e.isInitial,
    $isFinal: e.isFinal,
    children: [
      /* @__PURE__ */ i(W, { type: "target", position: N.Left }),
      /* @__PURE__ */ g(ti, {
        children: [
          /* @__PURE__ */ i(oi, { children: e.label }),
          e.isInitial &&
            /* @__PURE__ */ i(Ht, { $type: "initial", children: "Start" }),
          e.isFinal &&
            /* @__PURE__ */ i(Ht, { $type: "final", children: "End" }),
          e.response && /* @__PURE__ */ g(ni, { children: ["→ ", e.response] }),
        ],
      }),
      /* @__PURE__ */ i(W, { type: "source", position: N.Right }),
    ],
  });
}
const ri = { state: si },
  ii = {
    focusState: Mr,
  },
  ai = {
    focusTransition: Or,
  };
function No({
  states: e,
  mode: t = "compact",
  onStateClick: o,
  onTransitionClick: n,
}) {
  return t === "focus"
    ? /* @__PURE__ */ i(li, {
        states: e,
        onStateClick: o,
        onTransitionClick: n,
      })
    : /* @__PURE__ */ i(ci, { states: e });
}
function ci({ states: e }) {
  const { nodes: t, edges: o } = U(() => {
    const n = [],
      r = [],
      a = /* @__PURE__ */ new Map(),
      d = e.find((c) => c.isInitial),
      p = e.filter((c) => c.isFinal),
      l = e.filter((c) => !c.isInitial && !c.isFinal);
    return (
      d && a.set(d.name, { x: 0, y: 60 }),
      l.forEach((c, u) => {
        a.set(c.name, { x: 180, y: u * 80 + 20 });
      }),
      p.forEach((c, u) => {
        a.set(c.name, { x: 360, y: u * 80 + 20 });
      }),
      e.forEach((c) => {
        const u = a.get(c.name) || { x: 0, y: 0 };
        n.push({
          id: c.name,
          type: "state",
          position: u,
          data: {
            label: c.name === "__Initial" ? "Initial" : c.name,
            isInitial: c.isInitial,
            isFinal: c.isFinal,
            response: c.response?.eventType,
          },
        });
      }),
      e.forEach((c) => {
        c.transitions.forEach((u, f) => {
          r.push({
            id: `${c.name}-${u.transitionTo}-${f}`,
            source: c.name,
            target: u.transitionTo,
            label: u.eventType,
            type: "smoothstep",
            style: { stroke: "#6e7681", strokeWidth: 1 },
            labelStyle: { fontSize: 9, fill: "#8b949e" },
            labelBgStyle: { fill: "#161b22", fillOpacity: 0.9 },
            labelBgPadding: [4, 2],
          });
        });
      }),
      { nodes: n, edges: r }
    );
  }, [e]);
  return /* @__PURE__ */ i(Qr, {
    children: /* @__PURE__ */ i(ht, {
      nodes: t,
      edges: o,
      nodeTypes: ri,
      fitView: !0,
      fitViewOptions: { padding: 0.3 },
      nodesDraggable: !1,
      nodesConnectable: !1,
      elementsSelectable: !1,
      panOnDrag: !1,
      zoomOnScroll: !1,
      zoomOnPinch: !1,
      zoomOnDoubleClick: !1,
      preventScrolling: !1,
      proOptions: { hideAttribution: !0 },
      children: /* @__PURE__ */ i(gt, {
        variant: mt.Dots,
        gap: 12,
        size: 1,
        color: "#21262d",
      }),
    }),
  });
}
function li({ states: e, onStateClick: t, onTransitionClick: o }) {
  const [n, r] = Y([]),
    [a, d] = Y([]),
    p = ve(null),
    l = ve([]);
  (l.current = a),
    ie(() => {
      const x = e.map((b) => ({
        name: b.name,
        isInitial: b.isInitial ?? !1,
        isFinal: b.isFinal ?? !1,
        onEntry: {},
        response: b.response
          ? {
              eventType: b.response.eventType,
              eventTypeFullName: b.response.eventType,
            }
          : void 0,
        transitions: b.transitions.map(($) => ({
          eventType: $.eventType,
          eventTypeFullName: $.eventType,
          transitionTo: $.transitionTo,
          transitionKind: $.transitionKind ?? "event",
          autoProvision: !1,
          send: $.send?.map((v) => ({
            messageType: v.messageType,
            messageTypeFullName: v.messageType,
          })),
        })),
      }));
      Zr(x).then(({ nodes: b, edges: $ }) => {
        r(b), d($);
      });
    }, [e]);
  const c = G((x) => {
      const b = p.current;
      if (!b) return;
      const $ = b.querySelectorAll(".react-flow__node"),
        v = b.querySelectorAll(".react-flow__edge");
      if (!x) {
        $.forEach((w) => {
          w.classList.remove("saga-focus-highlighted", "saga-focus-dimmed");
        }),
          v.forEach((w) => {
            w.classList.remove("saga-focus-highlighted", "saga-focus-dimmed");
          });
        return;
      }
      const T = /* @__PURE__ */ new Set([x]),
        C = /* @__PURE__ */ new Set();
      for (const w of l.current)
        (w.source === x || w.target === x) &&
          (C.add(w.id), T.add(w.source === x ? w.target : w.source));
      $.forEach((w) => {
        const I = w.getAttribute("data-id");
        I && T.has(I)
          ? (w.classList.add("saga-focus-highlighted"),
            w.classList.remove("saga-focus-dimmed"))
          : (w.classList.add("saga-focus-dimmed"),
            w.classList.remove("saga-focus-highlighted"));
      }),
        v.forEach((w) => {
          const I = w.getAttribute("data-testid")?.replace("rf__edge-", "");
          I && C.has(I)
            ? (w.classList.add("saga-focus-highlighted"),
              w.classList.remove("saga-focus-dimmed"))
            : (w.classList.add("saga-focus-dimmed"),
              w.classList.remove("saga-focus-highlighted"));
        });
    }, []),
    u = G(
      (x, b) => {
        t?.(b.id);
      },
      [t]
    ),
    f = G(
      (x, b) => {
        const $ = b.data;
        $?.sourceStateName != null &&
          $?.transitionIndex != null &&
          o?.($.sourceStateName, $.transitionIndex);
      },
      [o]
    ),
    m = G(
      (x, b) => {
        c(b.id);
      },
      [c]
    ),
    y = G(() => {
      c(null);
    }, [c]);
  return /* @__PURE__ */ i(Jr, {
    ref: p,
    children: /* @__PURE__ */ i(ht, {
      nodes: n,
      edges: a,
      nodeTypes: ii,
      edgeTypes: ai,
      onNodeClick: u,
      onEdgeClick: f,
      onNodeMouseEnter: m,
      onNodeMouseLeave: y,
      fitView: !0,
      fitViewOptions: { padding: 0.3 },
      nodesDraggable: !1,
      nodesConnectable: !1,
      elementsSelectable: !1,
      proOptions: { hideAttribution: !0 },
      children: /* @__PURE__ */ i(gt, {
        variant: mt.Dots,
        gap: 20,
        size: 1,
        color: "#21262d",
      }),
    }),
  });
}
function di({ saga: e, onClose: t }) {
  const [o, n] = Y(null);
  ie(() => {
    function p(l) {
      l.key === "Escape" && (o ? n(null) : t());
    }
    return (
      window.addEventListener("keydown", p),
      () => window.removeEventListener("keydown", p)
    );
  }, [o, t]);
  const r = G(
      (p) => {
        const l = e.states.find((c) => c.name === p);
        l && n({ type: "state", stateName: l.name, state: l });
      },
      [e.states]
    ),
    a = G(
      (p, l) => {
        const c = e.states.find((u) => u.name === p);
        c &&
          c.transitions[l] &&
          n({
            type: "transition",
            transition: c.transitions[l],
            sourceStateName: p,
          });
      },
      [e.states]
    ),
    d = U(() => {
      let p = 0;
      const l = {};
      for (const c of e.states)
        for (const u of c.transitions)
          p++, (l[u.transitionKind] = (l[u.transitionKind] ?? 0) + 1);
      return {
        stateCount: e.states.length,
        totalTransitions: p,
        kindCounts: l,
      };
    }, [e.states]);
  return /* @__PURE__ */ g(fi, {
    children: [
      /* @__PURE__ */ g(hi, {
        children: [
          /* @__PURE__ */ g(gi, {
            onClick: t,
            children: [
              /* @__PURE__ */ i(z, { icon: mn }),
              /* @__PURE__ */ i("span", { children: "Back to Topology" }),
            ],
          }),
          /* @__PURE__ */ g(mi, {
            children: [
              /* @__PURE__ */ i(z, { icon: ue }),
              /* @__PURE__ */ i("span", { children: e.name }),
            ],
          }),
          /* @__PURE__ */ i(xi, {}),
        ],
      }),
      /* @__PURE__ */ i(yi, {
        children: /* @__PURE__ */ i(No, {
          states: e.states,
          mode: "focus",
          onStateClick: r,
          onTransitionClick: a,
        }),
      }),
      /* @__PURE__ */ g(bi, {
        children: [
          /* @__PURE__ */ g(Bt, { children: [d.stateCount, " states"] }),
          /* @__PURE__ */ i(Yt, {}),
          /* @__PURE__ */ g(Bt, {
            children: [d.totalTransitions, " transitions"],
          }),
          Object.entries(d.kindCounts).map(([p, l]) =>
            /* @__PURE__ */ g(
              "span",
              {
                children: [
                  /* @__PURE__ */ i(Yt, {}),
                  /* @__PURE__ */ g($i, { $kind: p, children: [l, " ", p] }),
                ],
              },
              p
            )
          ),
        ],
      }),
      o &&
        /* @__PURE__ */ g(wi, {
          $open: !0,
          children: [
            /* @__PURE__ */ g(Ti, {
              children: [
                /* @__PURE__ */ i(ki, {
                  children:
                    o.type === "state" ? "State Details" : "Transition Details",
                }),
                /* @__PURE__ */ i(Si, {
                  onClick: () => n(null),
                  children: /* @__PURE__ */ i(z, { icon: ho }),
                }),
              ],
            }),
            /* @__PURE__ */ g(Ii, {
              children: [
                o.type === "state" &&
                  o.state &&
                  /* @__PURE__ */ i(pi, {
                    state: o.state,
                    allStates: e.states,
                  }),
                o.type === "transition" &&
                  o.transition &&
                  /* @__PURE__ */ i(ui, {
                    transition: o.transition,
                    sourceStateName: o.sourceStateName ?? "",
                  }),
              ],
            }),
          ],
        }),
    ],
  });
}
function pi({ state: e, allStates: t }) {
  const o = [];
  for (const n of t)
    for (const r of n.transitions)
      r.transitionTo === e.name && o.push({ from: n.name, transition: r });
  return /* @__PURE__ */ g(pe, {
    children: [
      /* @__PURE__ */ g(se, {
        children: [
          /* @__PURE__ */ i(re, { children: "Name" }),
          /* @__PURE__ */ i(Ze, { children: e.name }),
        ],
      }),
      /* @__PURE__ */ g(se, {
        children: [
          /* @__PURE__ */ i(re, { children: "Type" }),
          /* @__PURE__ */ g(Ze, {
            children: [
              e.isInitial &&
                /* @__PURE__ */ i(ze, { $color: "blue", children: "Initial" }),
              e.isFinal &&
                /* @__PURE__ */ i(ze, { $color: "green", children: "Final" }),
              !e.isInitial &&
                !e.isFinal &&
                /* @__PURE__ */ i("span", { children: "Intermediate" }),
            ],
          }),
        ],
      }),
      e.response &&
        /* @__PURE__ */ g(se, {
          children: [
            /* @__PURE__ */ i(re, { children: "Response" }),
            /* @__PURE__ */ i(De, { children: e.response.eventType }),
          ],
        }),
      e.transitions.length > 0 &&
        /* @__PURE__ */ g(se, {
          children: [
            /* @__PURE__ */ g(re, {
              children: ["Outgoing Transitions (", e.transitions.length, ")"],
            }),
            e.transitions.map((n, r) =>
              /* @__PURE__ */ g(
                Ut,
                {
                  children: [
                    /* @__PURE__ */ i(Wt, { children: n.eventType }),
                    /* @__PURE__ */ g(Kt, {
                      children: [
                        /* @__PURE__ */ i(ze, {
                          $color: pt(n.transitionKind),
                          children: n.transitionKind,
                        }),
                        /* @__PURE__ */ g("span", {
                          children: ["→ ", n.transitionTo],
                        }),
                      ],
                    }),
                    n.send &&
                      n.send.length > 0 &&
                      /* @__PURE__ */ i(Ei, {
                        children: n.send.map((a, d) =>
                          /* @__PURE__ */ g(
                            Ni,
                            { children: ["↑ ", a.messageType] },
                            d
                          )
                        ),
                      }),
                  ],
                },
                r
              )
            ),
          ],
        }),
      o.length > 0 &&
        /* @__PURE__ */ g(se, {
          children: [
            /* @__PURE__ */ g(re, {
              children: ["Incoming Transitions (", o.length, ")"],
            }),
            o.map((n, r) =>
              /* @__PURE__ */ g(
                Ut,
                {
                  children: [
                    /* @__PURE__ */ i(Wt, { children: n.transition.eventType }),
                    /* @__PURE__ */ g(Kt, {
                      children: [
                        /* @__PURE__ */ i(ze, {
                          $color: pt(n.transition.transitionKind),
                          children: n.transition.transitionKind,
                        }),
                        /* @__PURE__ */ g("span", { children: ["← ", n.from] }),
                      ],
                    }),
                  ],
                },
                r
              )
            ),
          ],
        }),
    ],
  });
}
function ui({ transition: e, sourceStateName: t }) {
  return /* @__PURE__ */ g(pe, {
    children: [
      /* @__PURE__ */ g(se, {
        children: [
          /* @__PURE__ */ i(re, { children: "Event Type" }),
          /* @__PURE__ */ i(De, { children: e.eventType }),
        ],
      }),
      e.eventTypeFullName !== e.eventType &&
        /* @__PURE__ */ g(se, {
          children: [
            /* @__PURE__ */ i(re, { children: "Full Name" }),
            /* @__PURE__ */ i(De, {
              $small: !0,
              children: e.eventTypeFullName,
            }),
          ],
        }),
      /* @__PURE__ */ g(se, {
        children: [
          /* @__PURE__ */ i(re, { children: "Kind" }),
          /* @__PURE__ */ i(ze, {
            $color: pt(e.transitionKind),
            children: e.transitionKind,
          }),
        ],
      }),
      /* @__PURE__ */ g(se, {
        children: [
          /* @__PURE__ */ i(re, { children: "From" }),
          /* @__PURE__ */ i(Ze, { children: t }),
        ],
      }),
      /* @__PURE__ */ g(se, {
        children: [
          /* @__PURE__ */ i(re, { children: "To" }),
          /* @__PURE__ */ i(Ze, { children: e.transitionTo }),
        ],
      }),
      e.send &&
        e.send.length > 0 &&
        /* @__PURE__ */ g(se, {
          children: [
            /* @__PURE__ */ g(re, {
              children: ["Send Actions (", e.send.length, ")"],
            }),
            e.send.map((o, n) =>
              /* @__PURE__ */ g(
                Ci,
                {
                  children: [
                    /* @__PURE__ */ i(De, { children: o.messageType }),
                    o.messageTypeFullName !== o.messageType &&
                      /* @__PURE__ */ i(De, {
                        $small: !0,
                        children: o.messageTypeFullName,
                      }),
                  ],
                },
                n
              )
            ),
          ],
        }),
    ],
  });
}
function pt(e) {
  switch (e) {
    case "request":
      return "blue";
    case "reply":
      return "green";
    case "event":
      return "orange";
    default:
      return "blue";
  }
}
const fi = h.div`
  position: absolute;
  inset: 0;
  z-index: 900;
  background: ${s.colors.canvas.default};
  display: flex;
  flex-direction: column;
`,
  hi = h.div`
  height: 48px;
  display: flex;
  align-items: center;
  padding: 0 16px;
  background: ${s.colors.canvas.subtle};
  border-bottom: 1px solid ${s.colors.border.default};
  flex-shrink: 0;
`,
  gi = h.button`
  display: flex;
  align-items: center;
  gap: 8px;
  background: none;
  border: 1px solid ${s.colors.border.default};
  color: ${s.colors.fg.muted};
  font-size: 12px;
  cursor: pointer;
  padding: 6px 12px;
  border-radius: 6px;

  &:hover {
    background: ${s.colors.canvas.inset};
    color: ${s.colors.fg.default};
  }
`,
  mi = h.div`
  display: flex;
  align-items: center;
  gap: 8px;
  font-weight: 600;
  font-size: 14px;
  color: ${s.colors.fg.default};
  flex: 1;
  justify-content: center;

  svg {
    color: ${s.colors.accent.fg};
  }
`,
  xi = h.div`
  width: 140px;
`,
  yi = h.div`
  flex: 1;
  min-height: 0;
`,
  bi = h.div`
  height: 28px;
  display: flex;
  align-items: center;
  padding: 0 16px;
  gap: 8px;
  background: ${s.colors.canvas.subtle};
  border-top: 1px solid ${s.colors.border.muted};
  flex-shrink: 0;
`,
  Bt = h.span`
  font-size: 11px;
  color: ${s.colors.fg.muted};
`,
  Yt = h.span`
  width: 1px;
  height: 12px;
  background: ${s.colors.border.muted};
  display: inline-block;
  vertical-align: middle;
`,
  $i = h.span`
  font-size: 11px;
  color: ${(e) => {
    switch (e.$kind) {
      case "request":
        return s.colors.accent.fg;
      case "reply":
        return s.colors.success.fg;
      case "event":
        return s.colors.attention.fg;
      default:
        return s.colors.fg.muted;
    }
  }};
`,
  vi = Se`
  from { transform: translateX(100%); }
  to { transform: translateX(0); }
`,
  wi = h.div`
  position: absolute;
  top: 48px;
  right: 0;
  bottom: 28px;
  width: 320px;
  background: ${s.colors.canvas.subtle};
  border-left: 1px solid ${s.colors.border.default};
  display: flex;
  flex-direction: column;
  z-index: 910;
  animation: ${vi} 0.2s ease-out;
  box-shadow: -4px 0 12px rgba(1, 4, 9, 0.4);
`,
  Ti = h.div`
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 12px 16px;
  border-bottom: 1px solid ${s.colors.border.muted};
`,
  ki = h.div`
  font-weight: 600;
  font-size: 13px;
  color: ${s.colors.fg.default};
`,
  Si = h.button`
  background: none;
  border: none;
  color: ${s.colors.fg.muted};
  cursor: pointer;
  padding: 4px;
  border-radius: 4px;
  font-size: 14px;

  &:hover {
    background: ${s.colors.canvas.inset};
    color: ${s.colors.fg.default};
  }
`,
  Ii = h.div`
  flex: 1;
  overflow-y: auto;
  padding: 16px;
`,
  se = h.div`
  margin-bottom: 14px;

  &:last-child {
    margin-bottom: 0;
  }
`,
  re = h.div`
  font-size: 10px;
  font-weight: 600;
  color: ${s.colors.fg.subtle};
  text-transform: uppercase;
  letter-spacing: 0.5px;
  margin-bottom: 4px;
`,
  Ze = h.div`
  font-size: 13px;
  color: ${s.colors.fg.default};
`,
  De = h.div`
  font-family: ${s.fonts.mono};
  font-size: ${(e) => (e.$small ? "10px" : "12px")};
  color: ${s.colors.fg.default};
  word-break: break-all;
`,
  ze = h.span`
  display: inline-block;
  padding: 2px 6px;
  border-radius: 4px;
  font-size: 10px;
  font-weight: 600;
  text-transform: uppercase;

  ${(e) =>
    e.$color === "blue" &&
    F`
      background: rgba(88, 166, 255, 0.15);
      color: ${s.colors.accent.fg};
    `}

  ${(e) =>
    e.$color === "green" &&
    F`
      background: rgba(63, 185, 80, 0.15);
      color: ${s.colors.success.fg};
    `}

  ${(e) =>
    e.$color === "orange" &&
    F`
      background: rgba(210, 153, 34, 0.15);
      color: ${s.colors.attention.fg};
    `}
`,
  Ut = h.div`
  padding: 8px 10px;
  background: ${s.colors.canvas.inset};
  border: 1px solid ${s.colors.border.muted};
  border-radius: 4px;
  margin-top: 6px;
`,
  Wt = h.div`
  font-family: ${s.fonts.mono};
  font-size: 11px;
  color: ${s.colors.fg.default};
  margin-bottom: 4px;
`,
  Kt = h.div`
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 11px;
  color: ${s.colors.fg.muted};
`,
  Ei = h.div`
  margin-top: 6px;
  display: flex;
  flex-direction: column;
  gap: 2px;
`,
  Ni = h.div`
  font-size: 10px;
  color: ${s.colors.attention.fg};
  font-family: ${s.fonts.mono};
`,
  Ci = h.div`
  padding: 6px 8px;
  background: ${s.colors.canvas.inset};
  border: 1px solid ${s.colors.border.muted};
  border-radius: 4px;
  margin-top: 4px;
`;
function Xt(e, t) {
  if (e.length === 0) return { score: 0, matchedIndices: [] };
  const o = e.toLowerCase(),
    n = t.toLowerCase(),
    r = n.indexOf(o);
  if (r !== -1) {
    const l = [];
    for (let u = 0; u < e.length; u++) l.push(r + u);
    let c = 1e3 + e.length * 10;
    return (
      r === 0 && (c += 100),
      (c += Math.round((e.length / t.length) * 50)),
      { score: c, matchedIndices: l }
    );
  }
  const a = [];
  let d = 0;
  for (let l = 0; l < t.length && d < o.length; l++)
    n[l] === o[d] && (a.push(l), d++);
  if (d !== o.length) return null;
  let p = 0;
  for (let l = 0; l < a.length; l++) {
    const c = a[l];
    if ((l > 0 && a[l] === a[l - 1] + 1 && (p += 15), c === 0)) p += 20;
    else {
      const u = t[c - 1],
        f = t[c];
      u === "." || u === "-" || u === "_"
        ? (p += 20)
        : u === u.toLowerCase() &&
          f === f.toUpperCase() &&
          f !== f.toLowerCase() &&
          (p += 15);
    }
    p += Math.max(0, 10 - c);
  }
  return (
    (p += Math.round((e.length / t.length) * 30)),
    { score: p, matchedIndices: a }
  );
}
const Mi = ["consumer", "saga", "message", "endpoint", "entity", "route"],
  _i = {
    consumer: "Consumers",
    saga: "Sagas",
    message: "Messages",
    endpoint: "Endpoints",
    entity: "Entities",
    route: "Routes",
  },
  ct = {
    consumer: Xe,
    saga: ue,
    message: $t,
    endpoint: bt,
    entity: we,
    route: mo,
  },
  lt = {
    consumer: s.colors.success.fg,
    saga: s.colors.done.fg,
    message: s.colors.attention.fg,
    endpoint: s.colors.sponsors.fg,
    entity: s.colors.scale.purple4,
    route: s.colors.fg.muted,
  },
  Li = 50,
  Pi = 10;
function Ri({ searchIndex: e, onResultSelect: t }) {
  const [o, n] = Y(""),
    [r, a] = Y([]),
    d = ve(null);
  ie(() => {
    d.current?.focus();
  }, []);
  const p = U(() => {
      const m = o.trim();
      if (m.length === 0) return [];
      const y = [];
      for (const x of e) {
        const b = Xt(m, x.label),
          $ = x.description ? Xt(m, x.description) : null,
          v = b && $ ? (b.score >= $.score ? b : $) : b ?? $;
        v && y.push({ entry: x, score: v.score });
      }
      return y.sort((x, b) => b.score - x.score), y.slice(0, Li);
    }, [o, e]),
    l = U(() => {
      const m = /* @__PURE__ */ new Map();
      for (const y of p) {
        const x = m.get(y.entry.category) ?? [];
        x.push(y), m.set(y.entry.category, x);
      }
      return m;
    }, [p]),
    c = G(
      (m) => {
        t(m.nodeId),
          a((y) => {
            const x = y.filter((b) => b.nodeId !== m.nodeId);
            return [
              { label: m.label, nodeId: m.nodeId, category: m.category },
              ...x,
            ].slice(0, Pi);
          });
      },
      [t]
    ),
    u = () => {
      n(""), d.current?.focus();
    },
    f = o.trim().length > 0;
  return /* @__PURE__ */ g(Di, {
    children: [
      /* @__PURE__ */ g(zi, {
        children: [
          /* @__PURE__ */ i(Oi, {
            children: /* @__PURE__ */ i(z, { icon: go }),
          }),
          /* @__PURE__ */ i(Fi, {
            ref: d,
            type: "text",
            placeholder: "Search topology…",
            value: o,
            onChange: (m) => n(m.target.value),
            spellCheck: !1,
            autoComplete: "off",
          }),
          f &&
            /* @__PURE__ */ i(Ai, {
              onClick: u,
              title: "Clear search",
              children: /* @__PURE__ */ i(z, { icon: xn, size: "xs" }),
            }),
        ],
      }),
      /* @__PURE__ */ g(Gi, {
        children: [
          f &&
            p.length === 0 &&
            /* @__PURE__ */ i(jt, { children: "No results found" }),
          f &&
            Mi.map((m) => {
              const y = l.get(m);
              return !y || y.length === 0
                ? null
                : /* @__PURE__ */ g(
                    Vt,
                    {
                      children: [
                        /* @__PURE__ */ g(qt, {
                          children: [
                            /* @__PURE__ */ i(Zt, {
                              style: { color: lt[m] },
                              children: /* @__PURE__ */ i(z, { icon: ct[m] }),
                            }),
                            /* @__PURE__ */ i(Qt, { children: _i[m] }),
                            /* @__PURE__ */ i(Hi, { children: y.length }),
                          ],
                        }),
                        y.map(({ entry: x }) =>
                          /* @__PURE__ */ g(
                            Jt,
                            {
                              onClick: () => c(x),
                              children: [
                                /* @__PURE__ */ i(eo, {
                                  style: { color: lt[x.category] },
                                  children: /* @__PURE__ */ i(z, {
                                    icon: ct[x.category],
                                    size: "sm",
                                  }),
                                }),
                                /* @__PURE__ */ g(to, {
                                  children: [
                                    /* @__PURE__ */ i(oo, {
                                      children: x.label,
                                    }),
                                    x.description &&
                                      /* @__PURE__ */ i(Bi, {
                                        children: x.description,
                                      }),
                                  ],
                                }),
                                /* @__PURE__ */ i(ut, { children: "Focus →" }),
                              ],
                            },
                            x.nodeId
                          )
                        ),
                      ],
                    },
                    m
                  );
            }),
          !f &&
            r.length > 0 &&
            /* @__PURE__ */ g(Vt, {
              children: [
                /* @__PURE__ */ g(qt, {
                  children: [
                    /* @__PURE__ */ i(Zt, {
                      style: { color: s.colors.fg.subtle },
                      children: /* @__PURE__ */ i(z, { icon: xo }),
                    }),
                    /* @__PURE__ */ i(Qt, { children: "Recent" }),
                  ],
                }),
                r.map((m) =>
                  /* @__PURE__ */ g(
                    Jt,
                    {
                      onClick: () => {
                        const y = e.find((x) => x.nodeId === m.nodeId);
                        y ? c(y) : t(m.nodeId);
                      },
                      children: [
                        /* @__PURE__ */ i(eo, {
                          style: { color: lt[m.category] },
                          children: /* @__PURE__ */ i(z, {
                            icon: ct[m.category],
                            size: "sm",
                          }),
                        }),
                        /* @__PURE__ */ i(to, {
                          children: /* @__PURE__ */ i(oo, {
                            children: m.label,
                          }),
                        }),
                        /* @__PURE__ */ i(ut, { children: "Focus →" }),
                      ],
                    },
                    m.nodeId
                  )
                ),
              ],
            }),
          !f &&
            r.length === 0 &&
            /* @__PURE__ */ i(jt, {
              children: "Type to search consumers, messages, endpoints…",
            }),
        ],
      }),
    ],
  });
}
const Di = h.div`
  display: flex;
  flex-direction: column;
  height: 100%;
`,
  zi = h.div`
  position: relative;
  padding: 8px 12px;
  border-bottom: 1px solid ${s.colors.border.muted};
  flex-shrink: 0;
`,
  Oi = h.span`
  position: absolute;
  left: 20px;
  top: 50%;
  transform: translateY(-50%);
  font-size: 11px;
  color: ${s.colors.fg.subtle};
  pointer-events: none;
`,
  Fi = h.input`
  width: 100%;
  padding: 6px 28px 6px 26px;
  background: ${s.colors.canvas.inset};
  border: 1px solid ${s.colors.border.default};
  border-radius: 4px;
  color: ${s.colors.fg.default};
  font-size: 12px;
  font-family: ${s.fonts.sans};
  outline: none;

  &::placeholder {
    color: ${s.colors.fg.subtle};
  }

  &:focus-visible {
    border-color: ${s.colors.accent.fg};
    box-shadow: 0 0 0 1px ${s.colors.accent.fg}40;
  }
`,
  Ai = h.button`
  position: absolute;
  right: 18px;
  top: 50%;
  transform: translateY(-50%);
  display: flex;
  align-items: center;
  justify-content: center;
  width: 18px;
  height: 18px;
  border: none;
  background: none;
  color: ${s.colors.fg.muted};
  cursor: pointer;
  border-radius: 2px;

  &:hover {
    color: ${s.colors.fg.default};
  }

  &:focus-visible {
    outline: 2px solid ${s.colors.accent.fg};
    outline-offset: -2px;
  }
`,
  Gi = h.div`
  flex: 1;
  overflow-y: auto;
  min-height: 0;
`,
  jt = h.div`
  padding: 16px 12px;
  font-size: 11px;
  color: ${s.colors.fg.subtle};
  text-align: center;
`,
  Vt = h.div`
  &:not(:first-child) {
    border-top: 1px solid ${s.colors.border.muted};
  }
`,
  qt = h.div`
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 6px 12px 2px;
`,
  Zt = h.span`
  font-size: 10px;
  width: 14px;
  text-align: center;
`,
  Qt = h.span`
  font-size: 10px;
  font-weight: 600;
  color: ${s.colors.fg.muted};
  text-transform: uppercase;
  letter-spacing: 0.5px;
`,
  Hi = h.span`
  font-size: 9px;
  padding: 0 4px;
  background: ${s.colors.canvas.inset};
  border: 1px solid ${s.colors.border.muted};
  border-radius: 8px;
  color: ${s.colors.fg.subtle};
  line-height: 14px;
`,
  ut = h.span`
  font-size: 10px;
  color: ${s.colors.accent.fg};
  border: 1px solid ${s.colors.border.default};
  padding: 2px 8px;
  border-radius: 4px;
  opacity: 0;
  transition: opacity 0.1s ease;
  flex-shrink: 0;
`,
  Jt = h.div`
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 6px 12px;
  cursor: pointer;

  &:hover {
    background: rgba(88, 166, 255, 0.08);

    ${ut} {
      opacity: 1;
    }
  }
`,
  eo = h.span`
  font-size: 12px;
  width: 18px;
  text-align: center;
  flex-shrink: 0;
`,
  to = h.div`
  flex: 1;
  min-width: 0;
`,
  oo = h.div`
  font-size: 12px;
  font-weight: 500;
  color: ${s.colors.fg.default};
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
`,
  Bi = h.div`
  font-size: 10px;
  color: ${s.colors.fg.subtle};
  font-family: ${s.fonts.mono};
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  margin-top: 1px;
`,
  Yi = {
    exchange: po,
    queue: yt,
    topic: lo,
  };
function Ui({ node: e, onClose: t, onFocusSaga: o, open: n, embedded: r }) {
  if (!e || !(n ?? !!e)) return null;
  const d =
      typeof e.data.nodeType == "string"
        ? e.data.nodeType
        : e.type || "unknown",
    p = typeof e.data.entityKind == "string" ? e.data.entityKind : void 0,
    l =
      typeof e.data.fullData == "object" && e.data.fullData !== null
        ? e.data.fullData
        : e.data,
    c = () => {
      if (d === "entity" && p) return Yi[p] || we;
      switch (d) {
        case "consumer":
          return Xe;
        case "saga":
          return ue;
        case "message":
          return $t;
        case "endpoint":
          return bt;
        case "entity":
          return we;
        case "route":
          return mo;
        case "binding":
          return yn;
        default:
          return Xe;
      }
    },
    u = () => e.data.label || e.id,
    f = d === "saga" && l.states;
  return /* @__PURE__ */ g(Ki, {
    $hasSagaStateMachine: f,
    $embedded: !!r,
    children: [
      /* @__PURE__ */ g(Xi, {
        children: [
          /* @__PURE__ */ g(ji, {
            children: [
              /* @__PURE__ */ i(Vi, {
                children: /* @__PURE__ */ i(z, { icon: c() }),
              }),
              /* @__PURE__ */ i("span", { children: u() }),
            ],
          }),
          /* @__PURE__ */ i(qi, {
            onClick: t,
            "aria-label": "Close",
            title: "Close details",
            children: /* @__PURE__ */ i(z, { icon: ho }),
          }),
        ],
      }),
      /* @__PURE__ */ g(Zi, {
        children: [
          /* @__PURE__ */ g(be, {
            children: [
              /* @__PURE__ */ i(le, { children: "Information" }),
              l.name != null
                ? /* @__PURE__ */ g(j, {
                    $stacked: !0,
                    children: [
                      /* @__PURE__ */ i(V, { children: "Name" }),
                      /* @__PURE__ */ i(ce, { children: String(l.name) }),
                    ],
                  })
                : null,
              l.identity != null
                ? /* @__PURE__ */ g(j, {
                    $stacked: !0,
                    children: [
                      /* @__PURE__ */ i(V, { children: "Identity" }),
                      /* @__PURE__ */ i(ce, { children: String(l.identity) }),
                    ],
                  })
                : null,
              l.isBatch &&
                /* @__PURE__ */ g(j, {
                  children: [
                    /* @__PURE__ */ i(V, { children: "Type" }),
                    /* @__PURE__ */ i(Ye, {
                      $variant: "warning",
                      children: "batch handler",
                    }),
                  ],
                }),
              l.address != null
                ? /* @__PURE__ */ g(j, {
                    $stacked: !0,
                    children: [
                      /* @__PURE__ */ i(V, { children: "Address" }),
                      /* @__PURE__ */ i(ce, { children: String(l.address) }),
                    ],
                  })
                : null,
              l.transportName != null
                ? /* @__PURE__ */ g(j, {
                    children: [
                      /* @__PURE__ */ i(V, { children: "Transport" }),
                      /* @__PURE__ */ i(Pe, {
                        children: String(l.transportName),
                      }),
                    ],
                  })
                : null,
              l.runtimeType != null
                ? /* @__PURE__ */ g(j, {
                    children: [
                      /* @__PURE__ */ i(V, { children: "Type" }),
                      /* @__PURE__ */ i(Pe, {
                        children: String(l.runtimeType),
                      }),
                    ],
                  })
                : null,
              l.kind != null
                ? /* @__PURE__ */ g(j, {
                    children: [
                      /* @__PURE__ */ i(V, { children: "Kind" }),
                      /* @__PURE__ */ i(Ye, { children: String(l.kind ?? "") }),
                    ],
                  })
                : null,
            ],
          }),
          d === "endpoint" &&
            /* @__PURE__ */ g(be, {
              children: [
                /* @__PURE__ */ i(le, { children: "Metrics" }),
                /* @__PURE__ */ g(ea, {
                  children: [
                    /* @__PURE__ */ g(Ue, {
                      children: [
                        /* @__PURE__ */ i(We, { children: "30" }),
                        /* @__PURE__ */ i(Ke, { children: "Req/min" }),
                      ],
                    }),
                    /* @__PURE__ */ g(Ue, {
                      children: [
                        /* @__PURE__ */ i(We, { children: "40" }),
                        /* @__PURE__ */ i(Ke, { children: "Avg Latency" }),
                      ],
                    }),
                    /* @__PURE__ */ g(Ue, {
                      children: [
                        /* @__PURE__ */ i(We, { children: "40" }),
                        /* @__PURE__ */ i(Ke, { children: "Success Rate" }),
                      ],
                    }),
                    /* @__PURE__ */ g(Ue, {
                      children: [
                        /* @__PURE__ */ i(We, { children: "40" }),
                        /* @__PURE__ */ i(Ke, { children: "Errors" }),
                      ],
                    }),
                  ],
                }),
              ],
            }),
          d === "saga" &&
            l.states &&
            /* @__PURE__ */ g(be, {
              children: [
                /* @__PURE__ */ g(Qi, {
                  children: [
                    /* @__PURE__ */ i(le, { children: "State Machine" }),
                    o &&
                      /* @__PURE__ */ g(Ji, {
                        onClick: () => o(l),
                        title: "Open in focus mode",
                        children: [
                          /* @__PURE__ */ i(z, { icon: yo }),
                          /* @__PURE__ */ i("span", { children: "Focus" }),
                        ],
                      }),
                  ],
                }),
                /* @__PURE__ */ i(No, {
                  states: l.states,
                }),
              ],
            }),
          d === "route" &&
            /* @__PURE__ */ g(be, {
              children: [
                /* @__PURE__ */ i(le, { children: "Route Details" }),
                l.direction != null &&
                  /* @__PURE__ */ g(j, {
                    children: [
                      /* @__PURE__ */ i(V, { children: "Direction" }),
                      /* @__PURE__ */ i(Pe, { children: String(l.direction) }),
                    ],
                  }),
                l.messageTypeIdentity != null &&
                  /* @__PURE__ */ g(j, {
                    $stacked: !0,
                    children: [
                      /* @__PURE__ */ i(V, { children: "Message Type" }),
                      /* @__PURE__ */ i(ce, {
                        children: String(l.messageTypeIdentity),
                      }),
                    ],
                  }),
                l.consumerName != null &&
                  /* @__PURE__ */ g(j, {
                    $stacked: !0,
                    children: [
                      /* @__PURE__ */ i(V, { children: "Consumer" }),
                      /* @__PURE__ */ i(ce, {
                        children: String(l.consumerName),
                      }),
                    ],
                  }),
                l.endpointName != null &&
                  /* @__PURE__ */ g(j, {
                    $stacked: !0,
                    children: [
                      /* @__PURE__ */ i(V, { children: "Endpoint" }),
                      /* @__PURE__ */ i(ce, {
                        children: String(l.endpointName),
                      }),
                    ],
                  }),
              ],
            }),
          d === "binding" &&
            /* @__PURE__ */ g(be, {
              children: [
                /* @__PURE__ */ i(le, { children: "Binding Details" }),
                l.direction != null &&
                  /* @__PURE__ */ g(j, {
                    children: [
                      /* @__PURE__ */ i(V, { children: "Direction" }),
                      /* @__PURE__ */ i(Ye, { children: String(l.direction) }),
                    ],
                  }),
                l.source != null &&
                  /* @__PURE__ */ g(j, {
                    $stacked: !0,
                    children: [
                      /* @__PURE__ */ i(V, { children: "Source" }),
                      /* @__PURE__ */ i(ce, { children: String(l.source) }),
                    ],
                  }),
                l.target != null &&
                  /* @__PURE__ */ g(j, {
                    $stacked: !0,
                    children: [
                      /* @__PURE__ */ i(V, { children: "Target" }),
                      /* @__PURE__ */ i(ce, { children: String(l.target) }),
                    ],
                  }),
                l.address != null &&
                  /* @__PURE__ */ g(j, {
                    $stacked: !0,
                    children: [
                      /* @__PURE__ */ i(V, { children: "Address" }),
                      /* @__PURE__ */ i(ce, { children: String(l.address) }),
                    ],
                  }),
                l.properties != null &&
                  typeof l.properties == "object" &&
                  Object.keys(l.properties).length > 0 &&
                  /* @__PURE__ */ g(pe, {
                    children: [
                      /* @__PURE__ */ i(le, {
                        $marginTop: !0,
                        children: "Properties",
                      }),
                      Object.entries(l.properties).map(([m, y]) =>
                        /* @__PURE__ */ g(
                          j,
                          {
                            children: [
                              /* @__PURE__ */ i(V, { children: m }),
                              /* @__PURE__ */ i(Pe, {
                                children: y === null ? "null" : String(y),
                              }),
                            ],
                          },
                          m
                        )
                      ),
                    ],
                  }),
              ],
            }),
          d === "entity" &&
            /* @__PURE__ */ g(be, {
              children: [
                /* @__PURE__ */ i(le, { children: "Entity Details" }),
                l.flow != null &&
                  /* @__PURE__ */ g(j, {
                    children: [
                      /* @__PURE__ */ i(V, { children: "Flow" }),
                      /* @__PURE__ */ i(Ye, { children: String(l.flow) }),
                    ],
                  }),
                l.properties != null &&
                  typeof l.properties == "object" &&
                  Object.keys(l.properties).length > 0 &&
                  /* @__PURE__ */ g(pe, {
                    children: [
                      /* @__PURE__ */ i(le, {
                        $marginTop: !0,
                        children: "Properties",
                      }),
                      Object.entries(l.properties).map(([m, y]) =>
                        /* @__PURE__ */ g(
                          j,
                          {
                            children: [
                              /* @__PURE__ */ i(V, { children: m }),
                              /* @__PURE__ */ i(Pe, { children: String(y) }),
                            ],
                          },
                          m
                        )
                      ),
                    ],
                  }),
              ],
            }),
        ],
      }),
    ],
  });
}
const Wi = Se`
  from { transform: translateX(100%); }
  to { transform: translateX(0); }
`,
  Ki = h.div`
  display: flex;
  flex-direction: column;
  background: ${s.colors.canvas.subtle};

  ${(e) =>
    e.$embedded
      ? F`
          position: static;
          width: 100%;
          flex: 1;
          min-height: 0;
        `
      : F`
          position: absolute;
          top: 0;
          right: 0;
          bottom: 0;
          width: 340px;
          border-left: 1px solid ${s.colors.border.default};
          z-index: 800;
          box-shadow: -4px 0 16px rgba(1, 4, 9, 0.5);
          animation: ${Wi} 0.15s ease-out;

          ${
            e.$hasSagaStateMachine &&
            F`
            width: 520px;
          `
          }
        `}
`,
  Xi = h.div`
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 12px 16px;
  border-bottom: 1px solid ${s.colors.border.muted};
  background: ${s.colors.canvas.inset};
  flex-shrink: 0;
`,
  ji = h.div`
  display: flex;
  align-items: center;
  gap: 8px;
  font-weight: 600;
  font-size: 14px;
  min-width: 0;
  overflow: hidden;

  span {
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }
`,
  Vi = h.span`
  font-size: 16px;
  flex-shrink: 0;
`,
  qi = h.button`
  background: none;
  border: none;
  color: ${s.colors.fg.muted};
  cursor: pointer;
  padding: 4px;
  border-radius: 4px;
  font-size: 14px;
  flex-shrink: 0;

  &:hover {
    background: ${s.colors.canvas.subtle};
    color: ${s.colors.fg.default};
  }

  &:focus-visible {
    outline: 2px solid ${s.colors.accent.fg};
    outline-offset: -2px;
  }
`,
  Zi = h.div`
  padding: 16px;
  overflow-y: auto;
  flex: 1;
`,
  be = h.div`
  margin-bottom: 16px;

  &:last-child {
    margin-bottom: 0;
  }
`,
  le = h.div`
  font-size: 11px;
  font-weight: 600;
  color: ${s.colors.fg.subtle};
  text-transform: uppercase;
  letter-spacing: 0.5px;
  margin-bottom: 8px;

  ${(e) =>
    e.$marginTop &&
    F`
      margin-top: 12px;
    `}
`,
  Qi = h.div`
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 8px;

  ${le} {
    margin-bottom: 0;
  }
`,
  Ji = h.button`
  display: flex;
  align-items: center;
  gap: 4px;
  background: none;
  border: 1px solid ${s.colors.border.default};
  color: ${s.colors.accent.fg};
  font-size: 10px;
  cursor: pointer;
  padding: 2px 8px;
  border-radius: 4px;

  &:hover {
    background: rgba(88, 166, 255, 0.1);
    border-color: ${s.colors.accent.fg};
  }

  &:focus-visible {
    outline: 2px solid ${s.colors.accent.fg};
    outline-offset: -2px;
  }
`,
  j = h.div`
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 6px 0;
  border-bottom: 1px solid ${s.colors.border.muted};
  font-size: 12px;
  gap: 12px;

  &:last-child {
    border-bottom: none;
  }

  ${(e) =>
    e.$stacked &&
    F`
      flex-direction: column;
      align-items: flex-start;
      gap: 6px;
    `}
`,
  V = h.span`
  color: ${s.colors.fg.muted};
  flex-shrink: 0;
`,
  Pe = h.span`
  color: ${s.colors.fg.default};
  font-family: ${s.fonts.mono};
  font-size: 12px;
  text-align: right;
  overflow-wrap: break-word;
  word-break: break-word;
`,
  ce = h.span`
  color: ${s.colors.fg.default};
  font-family: ${s.fonts.mono};
  font-size: 12px;
  overflow-wrap: break-word;
  word-break: break-word;
  width: 100%;
`,
  Ye = h.span`
  display: inline-block;
  padding: 2px 6px;
  background: ${s.colors.canvas.inset};
  border: 1px solid ${s.colors.border.default};
  border-radius: 4px;
  font-size: 10px;
  font-weight: 500;

  ${(e) =>
    e.$variant === "success" &&
    F`
      color: ${s.colors.success.fg};
      border-color: ${s.colors.success.fg};
    `}

  ${(e) =>
    e.$variant === "warning" &&
    F`
      color: ${s.colors.attention.fg};
      border-color: ${s.colors.attention.fg};
    `}

  ${(e) =>
    e.$variant === "info" &&
    F`
      color: ${s.colors.accent.fg};
      border-color: ${s.colors.accent.fg};
    `}

  ${(e) =>
    e.$variant === "purple" &&
    F`
      color: ${s.colors.done.fg};
      border-color: ${s.colors.done.fg};
    `}
`,
  ea = h.div`
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 8px;
`,
  Ue = h.div`
  padding: 10px;
  background: ${s.colors.canvas.inset};
  border: 1px solid ${s.colors.border.muted};
  border-radius: 6px;
  text-align: center;
`,
  We = h.div`
  font-size: 18px;
  font-weight: 600;
  color: ${s.colors.accent.fg};
`,
  Ke = h.div`
  font-size: 9px;
  color: ${s.colors.fg.subtle};
  text-transform: uppercase;
  margin-top: 2px;
`;
function ta({
  sagas: e,
  onSagaClick: t,
  traceContent: o,
  onTraceFocusChange: n,
  searchIndex: r,
  onSearchResultSelect: a,
  selectedNode: d,
  onNodeDeselect: p,
  onFocusSaga: l,
  activeTab: c,
  onActiveTabChange: u,
  collapsed: f,
  onCollapsedChange: m,
  developerContent: y,
  visibleTabs: x,
}) {
  const b = x ? new Set(x) : null,
    $ = b ? b.has("sagas") : e.length > 0,
    v = b ? b.has("trace") : !!o,
    T = b ? b.has("search") : !!r && !!a,
    C = b ? b.has("details") : !!d,
    w = b ? b.has("developer") : !!y;
  return !$ && !v && !T && !C && !w
    ? null
    : /* @__PURE__ */ i(oa, {
        sagas: e,
        onSagaClick: t,
        traceContent: o,
        onTraceFocusChange: n,
        searchIndex: r,
        onSearchResultSelect: a,
        selectedNode: d,
        onNodeDeselect: p,
        onFocusSaga: l,
        hasSagas: $,
        hasTrace: v,
        hasSearch: T,
        hasDetails: C,
        hasDeveloper: w,
        defaultTab: v && !$ ? "trace" : "sagas",
        controlledActiveTab: c,
        onActiveTabChange: u,
        controlledCollapsed: f,
        onCollapsedChange: m,
        developerContent: y,
      });
}
function oa({
  sagas: e,
  onSagaClick: t,
  traceContent: o,
  onTraceFocusChange: n,
  searchIndex: r,
  onSearchResultSelect: a,
  selectedNode: d,
  onNodeDeselect: p,
  onFocusSaga: l,
  hasSagas: c,
  hasTrace: u,
  hasSearch: f,
  hasDetails: m,
  hasDeveloper: y,
  defaultTab: x,
  controlledActiveTab: b,
  onActiveTabChange: $,
  controlledCollapsed: v,
  onCollapsedChange: T,
  developerContent: C,
}) {
  const [w, I] = Y(x),
    [O, H] = Y(!0),
    [B, Q] = Y(!0),
    M = b ?? w,
    K = v ?? O,
    ae = G(
      (R) => {
        $?.(R), b === void 0 && I(R);
      },
      [b, $]
    ),
    Z = G(
      (R) => {
        T?.(R), v === void 0 && H(R);
      },
      [v, T]
    ),
    _ = G(
      (R, fe) => {
        n?.(R === "trace" && fe);
      },
      [n]
    ),
    X = G(
      (R) => {
        M === R && !K ? Z(!0) : (ae(R), Z(!1)), _(R, B);
      },
      [M, K, ae, Z, _, B]
    ),
    oe = G(
      (R) => {
        ae(R), _(R, B);
      },
      [ae, _, B]
    ),
    ee = () => {
      const R = !B;
      Q(R), _(M, R);
    },
    q = !K;
  return (
    ie(() => {
      _(x, !0);
    }, []),
    ie(() => {
      const R = () => (c ? "sagas" : u ? "trace" : f ? "search" : "sagas");
      (M === "details" && !m) || (M === "trace" && !u)
        ? oe(R())
        : M === "sagas" && !c
        ? oe(u ? "trace" : f ? "search" : "sagas")
        : M === "search" && !f
        ? oe(c ? "sagas" : u ? "trace" : "sagas")
        : M === "developer" && !y && oe(R());
    }, [c, u, f, m, y]),
    /* @__PURE__ */ g(ra, {
      $panelOpen: q,
      children: [
        /* @__PURE__ */ g(ia, {
          role: "tablist",
          children: [
            c &&
              /* @__PURE__ */ i(Re, {
                $active: M === "sagas" && q,
                onClick: () => X("sagas"),
                role: "tab",
                "aria-selected": M === "sagas" && q,
                title: "Sagas",
                icon: ue,
                badge: e.length,
              }),
            u &&
              /* @__PURE__ */ i(Re, {
                $active: M === "trace" && q,
                onClick: () => X("trace"),
                role: "tab",
                "aria-selected": M === "trace" && q,
                title: "Trace",
                icon: xo,
              }),
            f &&
              /* @__PURE__ */ i(Re, {
                $active: M === "search" && q,
                onClick: () => X("search"),
                role: "tab",
                "aria-selected": M === "search" && q,
                title: "Search",
                icon: go,
              }),
            m &&
              /* @__PURE__ */ i(Re, {
                $active: M === "details" && q,
                onClick: () => X("details"),
                role: "tab",
                "aria-selected": M === "details" && q,
                title: "Details",
                icon: bn,
              }),
            y &&
              /* @__PURE__ */ i(aa, {
                children: /* @__PURE__ */ i(Re, {
                  $active: M === "developer" && q,
                  onClick: () => X("developer"),
                  role: "tab",
                  "aria-selected": M === "developer" && q,
                  title: "Developer",
                  icon: $n,
                }),
              }),
          ],
        }),
        q &&
          /* @__PURE__ */ i(da, {
            children: /* @__PURE__ */ g(pa, {
              role: "tabpanel",
              children: [
                M === "sagas" &&
                  c &&
                  /* @__PURE__ */ i(ga, {
                    children: e.map((R) =>
                      /* @__PURE__ */ g(
                        ma,
                        {
                          onClick: () => t(R),
                          children: [
                            /* @__PURE__ */ i(xa, {
                              children: /* @__PURE__ */ i(z, { icon: ue }),
                            }),
                            /* @__PURE__ */ g(ya, {
                              children: [
                                /* @__PURE__ */ i(ba, { children: R.name }),
                                /* @__PURE__ */ g($a, {
                                  children: [
                                    R.states.length,
                                    " states",
                                    " · ",
                                    R.states.reduce(
                                      (fe, ge) => fe + ge.transitions.length,
                                      0
                                    ),
                                    " ",
                                    "transitions",
                                  ],
                                }),
                              ],
                            }),
                            /* @__PURE__ */ i(Co, { children: "Focus →" }),
                          ],
                        },
                        R.name
                      )
                    ),
                  }),
                M === "trace" &&
                  u &&
                  /* @__PURE__ */ g(pe, {
                    children: [
                      /* @__PURE__ */ i(ua, {
                        children: /* @__PURE__ */ g(fa, {
                          $active: B,
                          onClick: ee,
                          title: B
                            ? "Disable trace overlay"
                            : "Enable trace overlay",
                          children: [
                            /* @__PURE__ */ i(z, { icon: vn, size: "xs" }),
                            /* @__PURE__ */ i("span", { children: "Focus" }),
                          ],
                        }),
                      }),
                      /* @__PURE__ */ i(ha, { children: o }),
                    ],
                  }),
                M === "search" &&
                  f &&
                  /* @__PURE__ */ i(Ri, {
                    searchIndex: r,
                    onResultSelect: a,
                  }),
                M === "details" &&
                  m &&
                  d &&
                  /* @__PURE__ */ i(Ui, {
                    node: d,
                    onClose: p ?? (() => {}),
                    onFocusSaga: l,
                    embedded: !0,
                  }),
                M === "developer" &&
                  y &&
                  /* @__PURE__ */ i(va, { children: C }),
              ],
            }),
          }),
      ],
    })
  );
}
function Re({ icon: e, badge: t, ...o }) {
  return /* @__PURE__ */ g(ca, {
    ...o,
    children: [
      /* @__PURE__ */ i(z, { icon: e }),
      t != null && t > 0 && /* @__PURE__ */ i(la, { children: t }),
    ],
  });
}
const na = Se`
  from { transform: translateX(-100%); }
  to { transform: translateX(0); }
`,
  sa = Se`
  from { opacity: 0; }
  to { opacity: 1; }
`,
  ra = h.div`
  position: absolute;
  left: 0;
  top: 0;
  bottom: 0;
  width: ${(e) => (e.$panelOpen ? "320px" : "40px")};
  background: ${s.colors.canvas.subtle};
  border-right: 1px solid ${s.colors.border.default};
  z-index: 12;
  display: flex;
  flex-direction: row;
  animation: ${na} 0.2s ease;
  transition: width 0.2s ease;
  overflow: hidden;
  box-shadow: ${(e) => (e.$panelOpen ? "4px 0 12px rgba(1,4,9,0.35)" : "none")};
`,
  ia = h.div`
  width: 40px;
  flex-shrink: 0;
  display: flex;
  flex-direction: column;
  align-items: center;
  padding-top: 6px;
  gap: 4px;
  border-right: 1px solid ${s.colors.border.default};
`,
  aa = h.div`
  margin-top: auto;
  padding-bottom: 6px;
`,
  ca = h.button`
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  border: none;
  background: none;
  cursor: pointer;
  border-radius: 4px;
  border-left: 2px solid ${(e) =>
    e.$active ? s.colors.accent.fg : "transparent"};
  color: ${(e) => (e.$active ? s.colors.accent.fg : s.colors.fg.muted)};
  transition: color 0.15s ease, border-color 0.15s ease, background 0.15s ease;

  &:hover {
    background: ${s.colors.canvas.inset};
    color: ${s.colors.fg.default};
  }

  &:focus-visible {
    outline: 2px solid ${s.colors.accent.fg};
    outline-offset: -2px;
  }
`,
  la = h.span`
  position: absolute;
  top: 2px;
  right: 2px;
  min-width: 14px;
  height: 14px;
  padding: 0 3px;
  border-radius: 7px;
  background: ${s.colors.accent.fg};
  color: #fff;
  font-size: 9px;
  font-weight: 600;
  line-height: 14px;
  text-align: center;
  pointer-events: none;
`,
  da = h.div`
  flex: 1;
  display: flex;
  flex-direction: column;
  min-width: 0;
  overflow: hidden;
  animation: ${sa} 0.15s ease;
`,
  pa = h.div`
  flex: 1;
  overflow-y: auto;
  min-height: 0;
`,
  ua = h.div`
  display: flex;
  align-items: center;
  padding: 4px 12px;
  border-bottom: 1px solid ${s.colors.border.muted};
  flex-shrink: 0;
`,
  fa = h.button`
  display: flex;
  align-items: center;
  gap: 5px;
  padding: 3px 8px;
  border-radius: 4px;
  font-size: 11px;
  cursor: pointer;
  transition: all 0.15s ease;

  border: 1px solid ${(e) =>
    e.$active ? s.colors.accent.fg : s.colors.border.default};
  background: ${(e) => (e.$active ? `${s.colors.accent.fg}18` : "none")};
  color: ${(e) => (e.$active ? s.colors.accent.fg : s.colors.fg.muted)};

  &:hover {
    background: ${(e) =>
      e.$active ? `${s.colors.accent.fg}25` : s.colors.canvas.inset};
    color: ${(e) => (e.$active ? s.colors.accent.fg : s.colors.fg.default)};
  }

  &:focus-visible {
    outline: 2px solid ${s.colors.accent.fg};
    outline-offset: -2px;
  }
`,
  ha = h.div`
  display: flex;
  flex-direction: column;
  flex: 1;
  min-height: 0;
`,
  Co = h.span`
  font-size: 10px;
  color: ${s.colors.accent.fg};
  border: 1px solid ${s.colors.border.default};
  padding: 2px 8px;
  border-radius: 4px;
  opacity: 0;
  transition: opacity 0.1s ease;
  flex-shrink: 0;
`,
  ga = h.div`
  padding: 4px 0;
`,
  ma = h.div`
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 8px 12px;
  cursor: pointer;

  &:hover {
    background: rgba(88, 166, 255, 0.08);

    ${Co} {
      opacity: 1;
    }
  }
`,
  xa = h.div`
  color: ${s.colors.accent.fg};
  font-size: 14px;
  flex-shrink: 0;
  width: 20px;
  text-align: center;
`,
  ya = h.div`
  flex: 1;
  min-width: 0;
`,
  ba = h.div`
  font-size: 12px;
  font-weight: 500;
  color: ${s.colors.fg.default};
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
`,
  $a = h.div`
  font-size: 11px;
  color: ${s.colors.fg.subtle};
  margin-top: 1px;
`,
  va = h.div`
  display: flex;
  flex-direction: column;
  padding: 12px;
  gap: 12px;
`,
  wa = {
    compact: tr,
    route: rr,
    groupLabel: dr,
    sectionLabel: ur,
    summaryService: $r,
    summaryTransport: Nr,
  },
  Ta = /* @__PURE__ */ new Set(["summaryService", "summaryTransport"]),
  ka = /* @__PURE__ */ new Set([
    "compact",
    "route",
    "groupLabel",
    "sectionLabel",
    "group",
  ]),
  Sa = {
    elkRouted: Cr,
  },
  Ia = h.div`
  width: 100%;
  height: 100%;
  background-color: ${s.colors.canvas.default};
  position: relative;
`,
  Ea = Se`
  to {
    transform: rotate(360deg);
  }
`,
  Na = h.div`
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 100vh;
  gap: 12px;
  color: ${s.colors.fg.muted};
`,
  Ca = h.div`
  width: 32px;
  height: 32px;
  border: 2px solid ${s.colors.border.default};
  border-top-color: ${s.colors.accent.fg};
  border-radius: 50%;
  animation: ${Ea} 1s linear infinite;
`;
function Ma({ nodeId: e }) {
  const { getInternalNode: t, setCenter: o, getZoom: n } = xt();
  return (
    ie(() => {
      if (!e) return;
      const r = setTimeout(() => {
        const a = t(e);
        if (!a) return;
        const d = a.internals.positionAbsolute,
          p = a.measured?.width ?? 100,
          l = a.measured?.height ?? 40,
          c = d.x + p / 2,
          u = d.y + l / 2,
          f = Math.max(n(), 1.5);
        o(c, u, { duration: 400, zoom: f });
      }, 50);
      return () => clearTimeout(r);
    }, [e, t, o, n]),
    null
  );
}
function _a({ viewMode: e }) {
  const { fitView: t } = xt(),
    o = ve(e);
  return (
    ie(() => {
      o.current !== e &&
        ((o.current = e),
        setTimeout(() => {
          t({ padding: 0.2, duration: 300 });
        }, 50));
    }, [e, t]),
    null
  );
}
function La({ apiRef: e }) {
  const t = xt();
  return (
    ie(() => {
      e.current = {
        zoomIn: t.zoomIn,
        zoomOut: t.zoomOut,
        fitView: t.fitView,
      };
    }, [t, e]),
    null
  );
}
function Pa({ onZoomIn: e, onZoomOut: t, onFitView: o }) {
  return /* @__PURE__ */ g(Ra, {
    children: [
      /* @__PURE__ */ i(Da, { children: "Zoom" }),
      /* @__PURE__ */ g(za, {
        children: [
          /* @__PURE__ */ i(dt, {
            onClick: e,
            title: "Zoom in",
            children: /* @__PURE__ */ i(z, { icon: wn }),
          }),
          /* @__PURE__ */ i(dt, {
            onClick: t,
            title: "Zoom out",
            children: /* @__PURE__ */ i(z, { icon: Tn }),
          }),
          /* @__PURE__ */ i(dt, {
            onClick: o,
            title: "Fit view",
            children: /* @__PURE__ */ i(z, { icon: yo }),
          }),
        ],
      }),
    ],
  });
}
function pc({
  data: e,
  trace: t,
  traceContent: o,
  activeTraceActivityId: n,
  focusedTopologyNodeId: r,
  onTraceActivityHover: a,
  onTraceMappingChange: d,
  traceDisplayMode: p = "dim",
  edgeRouting: l = "smart",
  viewMode: c,
  onViewModeChange: u,
  sidebarTab: f,
  onSidebarTabChange: m,
  sidebarCollapsed: y,
  onSidebarCollapsedChange: x,
  showDeveloperPane: b = !1,
  developerPaneContent: $,
  hideSidebar: v = !1,
  hideMinimap: T = !1,
  hideControls: C = !1,
  sidebarTabs: w,
}) {
  const [I, O] = sn([]),
    [H, B] = rn([]),
    Q = G(
      (k) => {
        const S = k.filter((E) => E.type !== "position" && E.type !== "select");
        S.length > 0 && O((E) => an(S, E));
      },
      [O]
    ),
    [M, K] = Y(null),
    [ae, Z] = Y(null),
    [_, X] = Y(/* @__PURE__ */ new Set()),
    [oe, ee] = Y(!0),
    [q] = Y("detail"),
    [R, fe] = Y(null),
    [ge, Lo] = Y(v && !!t),
    [Ee, wt] = Y(null),
    [Po, Ro] = Y("sagas"),
    [Do, zo] = Y(!0),
    Ge = ve(null),
    Oo = f ?? Po,
    Fo = y ?? Do,
    me = G(
      (k) => {
        m?.(k), f === void 0 && Ro(k);
      },
      [f, m]
    ),
    Ne = G(
      (k) => {
        x?.(k), y === void 0 && zo(k);
      },
      [y, x]
    ),
    Ce = c ?? q,
    Ao = U(() => {
      const k = [];
      for (const S of e.services) for (const E of S.sagas) k.push(E);
      return k;
    }, [e]);
  ie(() => {
    async function k() {
      ee(!0);
      const { nodes: S, edges: E } = kn(e);
      try {
        const { nodes: D, edges: L } = await rs(S, E);
        O(D), B(L);
      } catch (D) {
        console.error("Layout failed:", D), O(S), B(E);
      }
      ee(!1);
    }
    k();
  }, [e, O, B]);
  const A = U(
    () => (!t || I.length === 0 ? null : xs(t, e, I, H)),
    [t, e, I, H]
  );
  ie(() => {
    d?.({ traceMapping: A, nodes: I, edges: H });
  }, [A, I, H, d]);
  const Tt = U(() => {
      if (_.size === 0 || !A) return null;
      const k = _.values().next().value;
      return k ? A.activityToNodeId.get(k) ?? null : null;
    }, [_, A]),
    de = ae ?? M?.id ?? null,
    kt = U(() => {
      const k = /* @__PURE__ */ new Map();
      for (const S of H) {
        let E = k.get(S.source);
        E || ((E = []), k.set(S.source, E)),
          E.push({ nodeId: S.target, edgeId: S.id }),
          (E = k.get(S.target)),
          E || ((E = []), k.set(S.target, E)),
          E.push({ nodeId: S.source, edgeId: S.id });
      }
      return k;
    }, [H]),
    { highlightedNodes: St, highlightedEdges: Me } = U(() => {
      if (!de)
        return {
          highlightedNodes: /* @__PURE__ */ new Set(),
          highlightedEdges: /* @__PURE__ */ new Set(),
        };
      const k = /* @__PURE__ */ new Set([de]),
        S = /* @__PURE__ */ new Set(),
        E = [de];
      for (; E.length > 0; ) {
        const D = E.shift(),
          L = kt.get(D);
        if (L)
          for (const { nodeId: ne, edgeId: tt } of L)
            S.add(tt), k.has(ne) || (k.add(ne), E.push(ne));
      }
      return {
        highlightedNodes: k,
        highlightedEdges: S,
      };
    }, [de, kt]),
    It = U(() => {
      const k = /* @__PURE__ */ new Map();
      return (
        !t ||
          !A ||
          [...t.activities]
            .sort(
              (E, D) =>
                new Date(E.startTime).getTime() -
                new Date(D.startTime).getTime()
            )
            .forEach((E, D) => {
              const L = A.activityToNodeId.get(E.id);
              L && !k.has(L) && k.set(L, D + 1);
            }),
        k
      );
    }, [t, A]),
    et = U(
      () => r || (n && A ? A.activityToNodeId.get(n) ?? null : Ee || null),
      [r, n, A, Ee]
    ),
    Go = U(() => Ss(I), [I]),
    Ho = U(() => {
      const k = !!de,
        S = !!A && ge,
        E = Tt,
        D = Ce === "overview";
      return I.map((L) => {
        const ne = Ta.has(L.type ?? ""),
          tt = ka.has(L.type ?? ""),
          ot = L.type === "compact" || L.type === "route",
          nt = Ee === L.id,
          st = (He) => (nt ? `${He} search-highlight`.trim() : He);
        if (D) {
          if (tt) return { ...L, hidden: !0 };
          if (ne) return { ...L, hidden: !1 };
        } else if (ne) return { ...L, hidden: !0 };
        if (!k && !S) return nt ? { ...L, className: "search-highlight" } : L;
        if (S) {
          const He = A.nodeIds.has(L.id),
            Zo = A.inferredNodeIds.has(L.id),
            Qo = A.errorNodeIds.has(L.id),
            Jo = E === L.id,
            en = et === L.id,
            Nt = It.get(L.id);
          if (He) {
            const xe = ["trace-active"];
            Zo && xe.push("trace-inferred"),
              Qo && xe.push("trace-error"),
              Jo && xe.push("highlighted"),
              en && xe.push("trace-focused"),
              nt && xe.push("search-highlight");
            const tn =
              typeof Nt == "number"
                ? { ...L.data, traceSequenceNumber: Nt }
                : L.data;
            return { ...L, data: tn, className: xe.join(" ") };
          }
          return p === "hide" && ot
            ? { ...L, className: st("hidden") }
            : { ...L, className: st(ot ? "dimmed" : "") };
        }
        return {
          ...L,
          className: st(St.has(L.id) ? "highlighted" : ot ? "dimmed" : ""),
        };
      });
    }, [I, de, St, A, ge, Tt, p, et, It, Ce, Ee]),
    Bo = U(() => {
      const k = !!de,
        S = !!A && ge,
        E = Ce === "overview";
      return H.map((D) => {
        const L = !!D.data?.isSummaryEdge;
        if (E) return L ? { ...D, hidden: !1 } : { ...D, hidden: !0 };
        if (L) return { ...D, hidden: !0 };
        if (!k && !S) return D;
        if (S) {
          const ne = A.edgeIds.has(D.id);
          return {
            ...D,
            animated: ne,
            style: {
              ...D.style,
              stroke: ne ? s.colors.accent.fg : "#30363d",
              strokeWidth: ne ? 2 : 1,
              opacity: ne ? 1 : 0.15,
            },
          };
        }
        return {
          ...D,
          animated: Me.has(D.id),
          style: {
            ...D.style,
            stroke: Me.has(D.id) ? "#58a6ff" : "#30363d",
            strokeWidth: Me.has(D.id) ? 2 : 1,
            opacity: Me.has(D.id) ? 1 : 0.3,
          },
        };
      });
    }, [H, de, Me, A, ge, Ce]),
    Yo = G(
      (k, S) => {
        if (S.type === "summaryService" || S.type === "summaryTransport") {
          u?.("detail");
          return;
        }
        (S.type === "compact" || S.type === "route") &&
          (M?.id === S.id ? K(null) : (K(S), me("details"), Ne(!1)));
      },
      [u, M?.id, me, Ne]
    ),
    Uo = G((k, S) => {
      (typeof S.data.nodeType == "string" ? S.data.nodeType : "") === "saga" &&
        S.data.fullData &&
        (fe(S.data.fullData), K(null));
    }, []),
    _e = ve(null),
    Wo = G(
      (k, S) => {
        (S.type === "compact" || S.type === "route") &&
          (_e.current && clearTimeout(_e.current),
          (_e.current = setTimeout(() => {
            if ((Z(S.id), A)) {
              const E = A.nodeIdToActivities.get(S.id);
              E && E.length > 0 && (X(new Set(E.map((D) => D.id))), a?.(E[0]));
            }
          }, 50)));
      },
      [A, a]
    ),
    Ko = G(() => {
      _e.current && clearTimeout(_e.current),
        Z(null),
        A && (X(/* @__PURE__ */ new Set()), a?.(null));
    }, [A, a]),
    Et = G((k) => {
      fe(k);
    }, []),
    Xo = G(
      (k) => {
        wt(k);
        const S = I.find((E) => E.id === k);
        S && (K(S), me("details"), Ne(!1)), setTimeout(() => wt(null), 500);
      },
      [I, me, Ne]
    ),
    jo = G(() => {
      K(null);
    }, []),
    Vo =
      o ??
      (t
        ? /* @__PURE__ */ i(Ns, {
            trace: t,
            hoveredActivityIds: _,
            onActivityHover: (k) => {
              k
                ? (X(/* @__PURE__ */ new Set([k.id])), a?.(k))
                : (X(/* @__PURE__ */ new Set()), a?.(null));
            },
            onActivityClick: (k) => {
              if (A) {
                const S = A.activityToNodeId.get(k.id);
                if (S) {
                  const E = I.find((D) => D.id === S);
                  E && (K(E), me("details"));
                }
              }
            },
          })
        : void 0),
    qo = b
      ? /* @__PURE__ */ g(pe, {
          children: [
            $,
            /* @__PURE__ */ i(Pa, {
              onZoomIn: () => Ge.current?.zoomIn(),
              onZoomOut: () => Ge.current?.zoomOut(),
              onFitView: () =>
                Ge.current?.fitView({ padding: 0.1, duration: 300 }),
            }),
          ],
        })
      : void 0;
  return oe
    ? /* @__PURE__ */ g(Na, {
        children: [
          /* @__PURE__ */ i(Ca, {}),
          /* @__PURE__ */ i("span", { children: "Loading topology..." }),
        ],
      })
    : /* @__PURE__ */ g(Ia, {
        children: [
          /* @__PURE__ */ g(ht, {
            nodes: Ho,
            edges: Bo,
            onNodesChange: Q,
            onNodeClick: Yo,
            onNodeDoubleClick: Uo,
            onNodeMouseEnter: Wo,
            onNodeMouseLeave: Ko,
            onPaneClick: jo,
            nodeTypes: wa,
            edgeTypes: Sa,
            nodesDraggable: !1,
            nodesConnectable: !1,
            elementsSelectable: !1,
            minZoom: 0.05,
            fitView: !0,
            fitViewOptions: { padding: 0.1 },
            defaultEdgeOptions: {
              type: "smoothstep",
              animated: !1,
            },
            proOptions: { hideAttribution: !0 },
            children: [
              /* @__PURE__ */ i(gt, {
                variant: mt.Dots,
                gap: 20,
                size: 1,
                color: "#21262d",
              }),
              !C && /* @__PURE__ */ i(cn, {}),
              !T &&
                /* @__PURE__ */ i(ln, {
                  nodeColor: (k) =>
                    k.type === "compact"
                      ? s.colors.accent.fg
                      : k.type === "route"
                      ? s.colors.done.fg
                      : k.type === "group"
                      ? s.colors.border.default
                      : "transparent",
                  maskColor: "rgba(0, 0, 0, 0.7)",
                  pannable: !0,
                  zoomable: !0,
                }),
              /* @__PURE__ */ i(Ma, { nodeId: et }),
              /* @__PURE__ */ i(_a, { viewMode: Ce }),
              b && /* @__PURE__ */ i(La, { apiRef: Ge }),
            ],
          }),
          R &&
            /* @__PURE__ */ i(di, {
              saga: R,
              onClose: () => fe(null),
            }),
          !v &&
            !R &&
            /* @__PURE__ */ i(ta, {
              sagas: Ao,
              onSagaClick: Et,
              traceContent: Vo,
              onTraceFocusChange: Lo,
              searchIndex: Go,
              onSearchResultSelect: Xo,
              selectedNode: M,
              onNodeDeselect: () => K(null),
              onFocusSaga: Et,
              activeTab: Oo,
              onActiveTabChange: me,
              collapsed: Fo,
              onCollapsedChange: Ne,
              developerContent: qo,
              visibleTabs: w,
            }),
        ],
      });
}
const Ra = h.div`
  display: flex;
  flex-direction: column;
  gap: 6px;
`,
  Da = h.div`
  font-size: 11px;
  font-weight: 500;
  color: ${s.colors.fg.muted};
  text-transform: uppercase;
  letter-spacing: 0.5px;
`,
  za = h.div`
  display: flex;
  gap: 6px;
`,
  dt = h.button`
  display: flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  border: 1px solid ${s.colors.border.default};
  background: ${s.colors.canvas.inset};
  color: ${s.colors.fg.muted};
  border-radius: 4px;
  cursor: pointer;
  transition: all 0.15s ease;

  &:hover {
    background: ${s.colors.canvas.subtle};
    color: ${s.colors.fg.default};
    border-color: ${s.colors.border.muted};
  }

  &:active {
    background: ${s.colors.canvas.default};
  }
`;
function uc({ sagas: e, onSagaClick: t, bottomOffset: o = 0 }) {
  const [n, r] = Y(!0);
  return e.length === 0
    ? null
    : /* @__PURE__ */ g(Oa, {
        $collapsed: n,
        $bottomOffset: o,
        children: [
          /* @__PURE__ */ i(Fa, {
            onClick: () => r(!n),
            children: /* @__PURE__ */ g(Aa, {
              children: [
                /* @__PURE__ */ i(z, {
                  icon: n ? ao : co,
                  size: "xs",
                }),
                /* @__PURE__ */ i(z, { icon: ue, size: "xs" }),
                /* @__PURE__ */ i(Ga, { children: "Sagas" }),
                /* @__PURE__ */ i(Ha, { children: e.length }),
              ],
            }),
          }),
          !n &&
            /* @__PURE__ */ i(Ba, {
              children: e.map((a) =>
                /* @__PURE__ */ g(
                  Ya,
                  {
                    onClick: () => t(a),
                    children: [
                      /* @__PURE__ */ i(Ua, {
                        children: /* @__PURE__ */ i(z, { icon: ue }),
                      }),
                      /* @__PURE__ */ g(Wa, {
                        children: [
                          /* @__PURE__ */ i(Ka, { children: a.name }),
                          /* @__PURE__ */ g(Xa, {
                            children: [
                              a.states.length,
                              " states",
                              " · ",
                              a.states.reduce(
                                (d, p) => d + p.transitions.length,
                                0
                              ),
                              " transitions",
                            ],
                          }),
                        ],
                      }),
                      /* @__PURE__ */ i(Mo, { children: "Focus →" }),
                    ],
                  },
                  a.name
                )
              ),
            }),
        ],
      });
}
const Oa = h.div`
  position: absolute;
  bottom: ${(e) => e.$bottomOffset}px;
  left: 0;
  right: 0;
  background: ${s.colors.canvas.subtle};
  border-top: 1px solid ${s.colors.border.default};
  z-index: 11;
  display: flex;
  flex-direction: column;
  max-height: ${(e) => (e.$collapsed ? "32px" : "220px")};
  transition: max-height 0.2s ease;
`,
  Fa = h.div`
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 6px 12px;
  cursor: pointer;
  user-select: none;
  flex-shrink: 0;

  &:hover {
    background: ${s.colors.canvas.inset};
  }
`,
  Aa = h.div`
  display: flex;
  align-items: center;
  gap: 8px;
  color: ${s.colors.fg.muted};
`,
  Ga = h.span`
  font-size: 12px;
  font-weight: 600;
  color: ${s.colors.fg.default};
`,
  Ha = h.span`
  font-size: 10px;
  padding: 1px 6px;
  background: ${s.colors.canvas.inset};
  border: 1px solid ${s.colors.border.muted};
  border-radius: 10px;
  color: ${s.colors.fg.muted};
`,
  Ba = h.div`
  flex: 1;
  overflow-y: auto;
  padding: 4px 0;
  border-top: 1px solid ${s.colors.border.muted};
`,
  Mo = h.span`
  font-size: 10px;
  color: ${s.colors.accent.fg};
  border: 1px solid ${s.colors.border.default};
  padding: 2px 8px;
  border-radius: 4px;
  opacity: 0;
  transition: opacity 0.1s ease;
  flex-shrink: 0;
`,
  Ya = h.div`
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 8px 12px;
  cursor: pointer;

  &:hover {
    background: rgba(88, 166, 255, 0.08);

    ${Mo} {
      opacity: 1;
    }
  }
`,
  Ua = h.div`
  color: ${s.colors.accent.fg};
  font-size: 14px;
  flex-shrink: 0;
  width: 20px;
  text-align: center;
`,
  Wa = h.div`
  flex: 1;
  min-width: 0;
`,
  Ka = h.div`
  font-size: 12px;
  font-weight: 500;
  color: ${s.colors.fg.default};
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
`,
  Xa = h.div`
  font-size: 10px;
  color: ${s.colors.fg.subtle};
  margin-top: 1px;
`,
  ja = h.div`
  position: absolute;
  pointer-events: all;
`,
  Va = h.div`
  position: absolute;
  inset: 0;
  padding: ${(e) =>
    Array.isArray(e.$padding)
      ? `${e.$padding[0]}px ${e.$padding[1]}px`
      : `${e.$padding ?? 2}px`};
  border-radius: ${(e) => e.$borderRadius ?? 2}px;
`;
function no(e, t) {
  const o = e.measured?.width ?? e.width ?? 100,
    n = e.measured?.height ?? e.height ?? 50,
    r = e.internals?.positionAbsolute?.x ?? e.position.x,
    a = e.internals?.positionAbsolute?.y ?? e.position.y;
  switch (t) {
    case N.Left:
      return { x: r, y: a + n / 2 };
    case N.Right:
      return { x: r + o, y: a + n / 2 };
    case N.Top:
      return { x: r + o / 2, y: a };
    case N.Bottom:
      return { x: r + o / 2, y: a + n };
    default:
      return { x: r + o, y: a + n / 2 };
  }
}
function so(e) {
  if (!e) return N.Right;
  const t = e.toLowerCase();
  return t.startsWith("left")
    ? N.Left
    : t.startsWith("right")
    ? N.Right
    : t.startsWith("top")
    ? N.Top
    : t.startsWith("bottom")
    ? N.Bottom
    : N.Right;
}
function qa(e) {
  switch (e) {
    case N.Left:
      return "left";
    case N.Right:
      return "right";
    case N.Top:
      return "top";
    case N.Bottom:
      return "bottom";
    default:
      return "right";
  }
}
const Za = 20;
function ro(e, t, o = Za) {
  switch (t) {
    case N.Left:
      return { x: e.x - o, y: e.y };
    case N.Right:
      return { x: e.x + o, y: e.y };
    case N.Top:
      return { x: e.x, y: e.y - o };
    case N.Bottom:
      return { x: e.x, y: e.y + o };
    default:
      return { x: e.x + o, y: e.y };
  }
}
function fc(e) {
  const {
      id: t,
      source: o,
      target: n,
      sourceHandleId: r,
      targetHandleId: a,
      style: d,
      markerStart: p,
      markerEnd: l,
      data: c,
      label: u,
      labelStyle: f,
      labelShowBg: m,
      labelBgStyle: y,
      labelBgPadding: x,
      labelBgBorderRadius: b,
    } = e,
    $ = Ct(o),
    v = Ct(n),
    T = dn((I) =>
      Array.from(I.nodeLookup.values()).map((O) => ({
        ...O,
        position: O.internals?.positionAbsolute ?? O.position,
        width: O.measured?.width ?? O.width,
        height: O.measured?.height ?? O.height,
      }))
    ),
    C = U(
      () => ({
        ...wo,
        ...c?.pathfindingConfig,
      }),
      [c?.pathfindingConfig]
    ),
    w = U(() => {
      if (!$ || !v) return { path: "", labelX: 0, labelY: 0 };
      const I = so(r),
        O = so(a),
        H = no($, I),
        B = no(v, O);
      let Q = ro(H, I);
      const M = ro(B, O),
        K = c?.bundle?.laneOffset ?? 0;
      if (K !== 0) {
        const ee = qa(I);
        Q = ts(Q, ee, K);
      }
      const Z = Kn(Q, M, T, C);
      let _, X, oe;
      if (Z && Z.length >= 2) {
        const ee = [
          H,
          Q,
          ...Z.slice(1, -1),
          // Exclude first/last as they're our offset points
          M,
          B,
        ];
        _ = Oe(ee, C.borderRadius);
        const q = Math.floor(ee.length / 2);
        (X = ee[q].x), (oe = ee[q].y);
      } else
        (_ = Oe([H, Q, M, B], C.borderRadius)),
          (X = (H.x + B.x) / 2),
          (oe = (H.y + B.y) / 2);
      return { path: _, labelX: X, labelY: oe };
    }, [$, v, T, r, a, C, c]);
  return w.path
    ? /* @__PURE__ */ g(pe, {
        children: [
          /* @__PURE__ */ i(ft, {
            id: t,
            path: w.path,
            style: d,
            markerStart: p,
            markerEnd: l,
          }),
          u &&
            /* @__PURE__ */ i(io, {
              children: /* @__PURE__ */ g(ja, {
                style: {
                  transform: `translate(-50%, -50%) translate(${w.labelX}px, ${w.labelY}px)`,
                  ...f,
                },
                className: "nodrag nopan react-flow__edge-label",
                children: [
                  m &&
                    /* @__PURE__ */ i(Va, {
                      $padding: x,
                      $borderRadius: b,
                      style: y,
                    }),
                  u,
                ],
              }),
            }),
        ],
      })
    : null;
}
const _o = {
  /** Spacing between parallel lanes in pixels */
  laneSpacing: 3,
  /** Minimum edges required to form a bundle (2 = only bundle when there are multiple edges) */
  minBundleSize: 2,
};
function hc(e, t) {
  const o = ec(t),
    n = Ja(e),
    r = tc(n, o);
  return e.map((a) => {
    const d = r.get(a.id);
    return d
      ? {
          ...a,
          data: {
            ...a.data,
            bundle: d,
          },
        }
      : a;
  });
}
function Qa(e, t) {
  return { bundleId: `source:${e.source}` };
}
function Ja(e, t) {
  const o = /* @__PURE__ */ new Map();
  for (const n of e) {
    const r = Qa(n);
    if (!r) continue;
    const a = o.get(r.bundleId) || [];
    a.push(n), o.set(r.bundleId, a);
  }
  return o;
}
function ec(e) {
  const t = /* @__PURE__ */ new Map();
  for (const o of e) {
    const n = o.position.x,
      r = o.position.y,
      a = o.measured?.height || o.style?.height || 40;
    t.set(o.id, {
      x: n,
      y: r,
      centerY: r + a / 2,
    });
  }
  return t;
}
function tc(e, t) {
  const o = /* @__PURE__ */ new Map();
  for (const [n, r] of e) {
    if (r.length < _o.minBundleSize) continue;
    const a = [...r].sort((p, l) => {
        const c = t.get(p.source),
          u = t.get(l.source),
          f = t.get(p.target),
          m = t.get(l.target),
          y = (c?.centerY ?? 0) - (u?.centerY ?? 0);
        return Math.abs(y) > 5 ? y : (f?.centerY ?? 0) - (m?.centerY ?? 0);
      }),
      d = a.length;
    a.forEach((p, l) => {
      const c = oc(l, d);
      o.set(p.id, {
        bundleId: n,
        laneIndex: l,
        totalLanes: d,
        laneOffset: c,
      });
    });
  }
  return o;
}
function oc(e, t) {
  if (t === 1) return 0;
  const o = (t - 1) / 2;
  return (e - o) * _o.laneSpacing;
}
export {
  tr as CompactNode,
  Ui as DetailPanel,
  Cr as ElkRoutedEdge,
  Mr as FocusStateNode,
  Or as FocusTransitionEdge,
  dc as GlobalStyles,
  ta as LeftSidebar,
  wo as PATHFINDING_DEFAULT_CONFIG,
  di as SagaFocusOverlay,
  uc as SagaListBar,
  No as SagaStateMachine,
  Ri as SearchPanel,
  dr as SimpleGroupLabel,
  rr as SimpleRouteNode,
  ur as SimpleSectionLabel,
  fc as SmartSmoothStepEdge,
  $r as SummaryServiceNode,
  Nr as SummaryTransportNode,
  pc as TopologyFlow,
  Ns as TraceTimeline,
  ts as applyLaneOffset,
  hc as assignEdgeBundles,
  Ss as buildSearchIndex,
  kn as diagramToFlow,
  Kn as findSmartPath,
  Xt as fuzzyMatch,
  Oe as generateSmoothStepPath,
  Zr as layoutSagaFocus,
  rs as layoutTopologyWithElk,
  xs as mapTraceToTopology,
  s as theme,
};
//# sourceMappingURL=index.js.map
