![React Rasta](https://cdn.rawgit.com/ChilliCream/react-rasta-logo/master/img/react-rasta-banner-light.svg)

[![release](https://img.shields.io/github/release/ChilliCream/react-rasta.svg)](https://github.com/ChilliCream/react-rasta/releases) [
![package](https://img.shields.io/npm/v/react-rasta.svg)](https://www.npmjs.com/package/react-rasta) [![license](https://img.shields.io/github/license/ChilliCream/react-rasta.svg)](https://github.com/ChilliCream/react-rasta/blob/master/LICENSE)
[![build](https://img.shields.io/circleci/project/github/ChilliCream/react-rasta.svg)](https://circleci.com/gh/ChilliCream/react-rasta/tree/master) [![coverage](https://img.shields.io/coveralls/ChilliCream/prometheus.svg)](https://coveralls.io/github/ChilliCream/prometheus?branch=master) [![better code](https://bettercodehub.com/edge/badge/ChilliCream/react-rasta)](https://bettercodehub.com/results/ChilliCream/react-rasta) [![code prettier](https://img.shields.io/badge/code_style-prettier-ff69b4.svg)](https://github.com/prettier/prettier)

---

**The most powerful and flexible grid system for _React_**

_React Rasta_ is a 12 column grid system built on top of the _CSS flexbox_ layout and `styled-components`.

## Getting Started

Here you will find all you need to get started quickly.

### Install Package

First things first. Install the package `react-rasta` with _yarn_ or _npm_.

When using _yarn_ it looks like this.

```powershell
yarn add react-rasta
```

And when using _npm_ it looks like this.

```powershell
npm install react-rasta --save
```

#### Required Dependencies

_React Rasta_ depends on the following packages which need to be installed manually.

| Package             | Version      |
| ------------------- | ------------ |
| `react`             | 16 or higher |
| `styled-components` | 3 or higher  |

### Code Examples

```tsx
import React, {Component} from "react";
import {Column, Container, Row} from "react-rasta";

export default class App extends Component {
  render() {
    return (
      <Container>
        <Row>
          <Column size={3}>Left</Column>
          <Column size={{xs: 9, md: 3}}>Middle 1</Column>
          <Column size={{xs: 9, md: 3}}>Middle 2</Column>
          <Column size={3}>Right</Column>
        </Row>
      </Container>
    );
  }
}
```

Breakpoints (which will end up in media queries) could be redefined via `ThemeProvider`.

```tsx
import React, {Component} from "react";
import {Column, Container, Row, ThemeProvider} from "react-rasta";

const breakpoints = {
  phone: 0,
  tablet: 600,
  desktop: 800,
};

const containerWidth = {
  // do not specify phone here
  tablet: 560,
  desktop: 760,
};

export default class App extends Component {
  render() {
    return (
      <ThemeProvider theme={{breakpoints, containerWidth}}>
        <Container>
          <Row>
            <Column size={3}>Left</Column>
            <Column size={{phone: 9, tablet: 3}}>Middle 1</Column>
            <Column size={{phone: 9, tablet: 3}}>Middle 2</Column>
            <Column size={3}>Right</Column>
          </Row>
        </Container>
      </ThemeProvider>
    );
  }
}
```

## Documentation

Click [here](http://react-rasta.com) for the documentation.
