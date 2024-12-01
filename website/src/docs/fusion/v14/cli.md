---
title: "Fusion CLI Documentation"
---

The Fusion Command Line Interface is a tool designed to assist the development and management of APIs using Fusion. This documentation provides a comprehensive guide to the Fusion CLI, covering installation, commands, options, and usage examples to help you effectively utilize the tool in your projects.

## Introduction

Fusion CLI is a command-line tool that assists developers in composing, packaging, and managing federated schemas for Fusion gateways.

## Installation

To install the Fusion CLI, ensure you have the .NET SDK installed on your machine. Then, install the CLI globally using the .NET tool command:

```bash
dotnet tool install -g HotChocolate.Fusion.CommandLine
```

This command installs the Fusion CLI globally, making it accessible from any directory in your command line.

## Commands

### `fusion compose`

The `compose` command is used to create or update a Fusion gateway package (`.fgp` file) by composing multiple APIs packages (`.fsp` files). This package is then used by the Fusion gateway to serve a unified schema.

**Description:**

Composes multiple APIs into a Fusion gateway package.

**Usage:**

```bash
fusion compose [options]
```

**Options:**

- `-p`, `--package`, `--package-file <package-file>` (REQUIRED): Specifies the path to the Fusion gateway package file (`gateway.fgp`). This file will be created or updated by the compose command.
- `-s`, `--subgraph`, `--subgraph-package-file <subgraph-package-file>`: Specifies the path to a subgraph package file (`.fsp`) to include in the composition. This option can be used multiple times to add multiple subgraphs.
- `--package-settings`, `--package-settings-file`, `--settings <package-settings-file>`: Specifies the path to a Fusion package settings file (`fusion-subgraph.json`). This file contains additional settings for the composition.
- `-w`, `--working-directory <working-directory>`: Sets the working directory for the command. Defaults to the current executing directory.
- `--enable-nodes`: Enables the Node interface feature in the gateway, allowing it to understand `node(id: ...)` queries.
- `-r`, `--remove <subgraph-name>`: Removes a specified subgraph from the existing composition in the gateway package.
- `-?`, `-h`, `--help`: Shows help and usage information for the command.

---

### `fusion subgraph`

The `subgraph` command group contains commands related to subgraph management, such as packaging subgraphs for composition.

#### `fusion subgraph pack`

The `pack` command creates a Fusion subgraph package (`.fsp` file) from a subgraph's schema and configuration. This package is then used in the composition process.

**Description:**

Creates a Fusion subgraph package from a subgraph's schema and configuration files.

**Usage:**

```bash
fusion subgraph pack [options]
```

**Options:**

- `-p`, `--package`, `--package-file <package-file>`: Specifies the output path for the subgraph package file (`YourService.fsp`). Defaults to `<SourceName>.fsp` if not specified.
- `-s`, `--schema`, `--schema-file <schema-file>`: Specifies the path to the subgraph's schema file (`schema.graphql`).
- `-c`, `--config`, `--config-file <config-file>`: Specifies the path to the subgraph's configuration file (`subgraph-config.json`).
- `-e`, `--extension`, `--extension-file <extension-file>`: Specifies paths to any schema extension files to include. This option can be used multiple times for multiple files.
- `-w`, `--working-directory <working-directory>`: Sets the working directory for the command. Defaults to the current executing directory.
- `-?`, `-h`, `--help`: Shows help and usage information for the command.

## Examples

This section provides practical examples of using the Fusion CLI commands in common scenarios.

### Example 1: Packing a Downstream Service

Suppose you have a subgraph named `Products` with a schema file `schema.graphql` and a configuration file `subgraph-config.json`. To create a subgraph package, navigate to the subgraph's directory and run:

```bash
fusion subgraph pack
```

In case your schema and configuration files have different names or are located in a different directory, you can specify them using the `-s` and `-c` options:

```bash
fusion subgraph pack -s other-schema.graphql -c config.json
```

This command generates a `Products.fsp` package file in the current directory.

### Example 2: Composing a Gateway Package

To compose a Fusion gateway package from multiple subgraph packages, use the `compose` command. For example, to compose the `Products` and `Orders` subgraphs into a gateway package named `gateway.fgp`, run:

```bash
fusion compose -p gateway.fgp -s ../Products/Products.fsp -s ../Orders/Orders.fsp
```

This command creates or updates the `gateway.fgp` file with the composed schema from both subgraphs.

### Example 3: Removing a Downstream Service from the Composition

If you need to remove the `Orders` subgraph from an existing gateway package, use the `--remove` option:

```bash
fusion compose -p gateway.fgp -r Orders
```

This command updates `gateway.fgp`, removing the `Orders` subgraph from the composition.

### Example 4: Enabling Node Interface Support

To enable the Node interface feature in your gateway, allowing it to handle `node` queries, include the `--enable-nodes` flag during composition:

```bash
fusion compose -p gateway.fgp -s ../Products/Products.fsp --enable-nodes
```

### Example 5: Specifying Working Directory

If your schema and configuration files are located in a different directory, you can specify the working directory using `-w`:

```bash
fusion subgraph pack -s schema.graphql -c subgraph-config.json -w /path/to/subgraph
```

## Additional Resources

- **Fusion Documentation:** Explore the [official Fusion documentation](/docs/fusion/v14) for in-depth guides and references.
- **Fusion Quick Start Guide:** Get started with Fusion by following the [Quick Start Guide](/docs/fusion/v14/quick-start).
- **ChilliCream Community:** Join the [ChilliCream community](https://slack.chillicream.com) to ask questions, share experiences, and contribute to the project.
