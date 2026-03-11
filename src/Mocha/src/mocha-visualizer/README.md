# @chillicream/mocha-visualizer

Reusable topology visualization component for messaging systems.

## Installation

```bash
npm install @chillicream/mocha-visualizer
```

## Peer Dependencies

This library requires the following peer dependencies:

```bash
npm install react react-dom @xyflow/react elkjs @fortawesome/fontawesome-svg-core @fortawesome/free-solid-svg-icons @fortawesome/react-fontawesome
```

## Usage

### Standalone React App

```tsx
import { TopologyFlow, type DiagramData } from "@chillicream/mocha-visualizer";

function App() {
  const data: DiagramData = {
    services: [
      /* ... */
    ],
    transports: [
      /* ... */
    ],
  };

  return (
    <div style={{ width: "100vw", height: "100vh" }}>
      <TopologyFlow data={data} />
    </div>
  );
}
```

### MDX Documentation

```mdx
import { TopologyFlow } from "@chillicream/mocha-visualizer";
import topologyData from "./topology.json";

<div style={{ height: "800px" }}>
  <TopologyFlow data={topologyData} />
</div>
```

## Exports

### Components

- `TopologyFlow` - Main visualization component
- `DetailPanel` - Node detail popup
- `SagaStateMachine` - State machine visualization
- `CompactNode`, `SimpleRouteNode`, `SimpleGroupLabel`, `SimpleSectionLabel` - Node types
- `SmartSmoothStepEdge` - Custom edge with pathfinding

### Types

- `DiagramData` - Root data structure
- `Service`, `Transport`, `Consumer`, `Saga`, `MessageType`, etc.

### Utilities

- `diagramToFlow` - Transform DiagramData to React Flow nodes/edges
- `layoutTopologyWithElk` - ELK-based automatic layout
- `assignEdgeBundles` - Metro-style edge bundling
- `findSmartPath`, `generateSmoothStepPath` - Pathfinding utilities

## CSS

Styles are automatically included when you import the library. The library uses CSS custom properties for theming with a GitHub Dark theme by default.
