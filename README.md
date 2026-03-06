# QaaS.Common.Probes

Composable .NET probes for QaaS workflow setup and environment data/state manipulation.

[![CI](https://img.shields.io/badge/CI-GitHub_Actions-2088FF)](./.github/workflows/ci.yml)
[![Docs](https://img.shields.io/badge/docs-qaas--docs-blue)](https://thesmoketeam.github.io/qaas-docs/)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)

## Contents
- [Overview](#overview)
- [Packages](#packages)
- [Functionalities](#functionalities)
- [Quick Start](#quick-start)
- [Build and Test](#build-and-test)
- [Documentation](#documentation)

## Overview
This repository contains one solution: [`QaaS.Common.Probes.sln`](./QaaS.Common.Probes.sln).

The solution is split into:
- A publishable probes package: [`QaaS.Common.Probes`](./QaaS.Common.Probes/)
- A test project: [`QaaS.Common.Probes.Tests`](./QaaS.Common.Probes.Tests/)

## Packages
| Package | Latest Version | Total Downloads |
|---|---|---|
| [QaaS.Common.Probes](https://www.nuget.org/packages/qaas.common.probes/) | [![NuGet](https://img.shields.io/nuget/v/QaaS.Common.Probes?logo=nuget)](https://www.nuget.org/packages/qaas.common.probes/) | [![Downloads](https://img.shields.io/nuget/dt/QaaS.Common.Probes?logo=nuget)](https://www.nuget.org/packages/qaas.common.probes/) |

## Functionalities
### [OpenShift / Kubernetes probes](./QaaS.Common.Probes/OsProbes/)
- Pod scaling for Deployments and StatefulSets.
- Rolling updates for container image and resources.
- Environment variable mutation for Deployments and StatefulSets.
- ConfigMap YAML editing via key-path based updates.
- Pod restart flow with readiness/state wait logic.
- Command execution inside selected pod containers.

### [RabbitMQ probes](./QaaS.Common.Probes/RabbitMqProbes/)
- Queue and exchange creation.
- Queue and exchange deletion.
- Binding creation and deletion (exchange->queue and exchange->exchange).
- Queue purge operations.

### [Redis probes](./QaaS.Common.Probes/RedisProbes/)
- `FLUSHALL` operation.
- `FLUSHDB` operation for selected DB.
- Chunked key-space cleanup using SCAN + batch delete.

### [S3 probes](./QaaS.Common.Probes/S3Probes/)
- Bucket object cleanup (optionally by prefix).
- Bucket cleanup followed by bucket deletion.

### [SQL probes](./QaaS.Common.Probes/SqlProbes/)
- Table truncation for MSSQL.
- Table truncation for PostgreSQL.
- Table truncation for Oracle.

### [Elastic probes](./QaaS.Common.Probes/ElasticProbes/)
- Index cleanup by index pattern and query string.

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
- NuGet package: [QaaS.Common.Probes](https://www.nuget.org/packages/qaas.common.probes/)
