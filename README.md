# QaaS.Common.Probes

Composable .NET probes for QaaS workflow setup and environment data/state manipulation.

[![CI](https://img.shields.io/badge/CI-GitHub_Actions-2088FF)](./.github/workflows/ci.yml)
[![Docs](https://img.shields.io/badge/docs-qaas--docs-blue)](https://thesmoketeam.github.io/qaas-docs/)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)

## Contents
- [Overview](#overview)
- [Packages](#packages)
- [Functionalities](#functionalities)
- [Protocol Support](#protocol-support)
- [Quick Start](#quick-start)
- [Build and Test](#build-and-test)
- [Documentation](#documentation)

## Overview
This repository contains one solution: [`QaaS.Common.Probes.sln`](./QaaS.Common.Probes.sln).

The solution is split into a publishable NuGet package for shared probe implementations and a dedicated NUnit test project.

## Packages
| Package | Latest Version | Total Downloads |
|---|---|---|
| [QaaS.Common.Probes](https://www.nuget.org/packages/qaas.common.probes) | [![NuGet](https://img.shields.io/nuget/v/QaaS.Common.Probes?logo=nuget)](https://www.nuget.org/packages/qaas.common.probes) | [![Downloads](https://img.shields.io/nuget/dt/QaaS.Common.Probes?logo=nuget)](https://www.nuget.org/packages/qaas.common.probes) |

## Functionalities
### [QaaS.Common.Probes](./QaaS.Common.Probes/)
- OpenShift/Kubernetes probes for pod scaling, image/resource updates, env var mutation, config-map YAML edits, pod restarts, and command execution in containers.
- RabbitMQ probes for queue/exchange create/delete, binding create/delete, and queue purge operations.
- Redis probes for `FLUSHALL`, `FLUSHDB`, and chunked key cleanup via `SCAN` + delete.
- S3 probes for bucket object cleanup (optional prefix) and bucket deletion flows.
- SQL probes for table truncation in MSSQL, PostgreSQL, and Oracle.
- Elastic probes for index cleanup by index pattern and query string.
- Shared configuration objects and extensions used across probe implementations.

### [QaaS.Common.Probes.Tests](./QaaS.Common.Probes.Tests/)
- NUnit test project covering probe logic and branch behavior.
- Uses Moq-based test doubles for protocol/client interaction testing.

## Protocol Support
Supported operational targets in `QaaS.Common.Probes`:

| Family | Implementations |
|---|---|
| Container Orchestration | OpenShift/Kubernetes |
| Messaging / Queueing | RabbitMQ |
| Databases / Cache | Redis, MSSQL, PostgreSQL, Oracle |
| Search / Indexing | Elasticsearch |
| Object Storage | S3 |

## Quick Start
Install package:

```bash
dotnet add package QaaS.Common.Probes
```

Update package:

```bash
dotnet add package QaaS.Common.Probes --version 1.0.0-alpha.2
dotnet restore
```

## Build and Test
```bash
dotnet restore QaaS.Common.Probes.sln
dotnet build QaaS.Common.Probes.sln -c Release --no-restore
dotnet test QaaS.Common.Probes.sln -c Release --no-build
```

## Documentation
- Official docs: [thesmoketeam.github.io/qaas-docs](https://thesmoketeam.github.io/qaas-docs/)
- CI workflow: [`.github/workflows/ci.yml`](./.github/workflows/ci.yml)
- NuGet package: [QaaS.Common.Probes](https://www.nuget.org/packages/qaas.common.probes)
