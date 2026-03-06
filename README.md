# QaaS.Common.Probes

Composable .NET probes for QaaS workflow setup and environment data/state manipulation.

[![CI](https://img.shields.io/badge/CI-GitHub_Actions-2088FF)](./.github/workflows/ci.yml)
[![Docs](https://img.shields.io/badge/docs-qaas--docs-blue)](https://thesmoketeam.github.io/qaas-docs/)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)

## Contents
- [Overview](#overview)
- [Packages](#packages)
- [Architecture](#architecture)
- [Install and Upgrade](#install-and-upgrade)
- [Documentation](#documentation)

## Overview
This repository contains one solution: [`QaaS.Common.Probes.sln`](./QaaS.Common.Probes.sln).

The solution provides reusable probe implementations consumed by QaaS workflows for infrastructure and data-state manipulation across OpenShift/Kubernetes, RabbitMQ, Redis, S3, SQL, and Elastic environments.

## Packages
| Package | Latest Version | Total Downloads |
|---|---|---|
| [QaaS.Common.Probes](https://www.nuget.org/packages/qaas.common.probes) | [![NuGet](https://img.shields.io/nuget/v/QaaS.Common.Probes?logo=nuget)](https://www.nuget.org/packages/qaas.common.probes) | [![Downloads](https://img.shields.io/nuget/dt/QaaS.Common.Probes?logo=nuget)](https://www.nuget.org/packages/qaas.common.probes) |

## Architecture
### [QaaS.Common.Probes](./QaaS.Common.Probes/)
- **OpenShift/Kubernetes probes (`OsProbes/`)**:
  - Deployment/StatefulSet scaling.
  - Deployment/StatefulSet image updates.
  - Deployment/StatefulSet resource updates.
  - Deployment/StatefulSet environment variable updates.
  - ConfigMap YAML mutation.
  - Pod restart with desired-state wait logic.
  - Command execution inside containers.
- **RabbitMQ probes (`RabbitMqProbes/`)**:
  - Queue/exchange create and delete.
  - Binding create and delete (exchange->queue and exchange->exchange).
  - Queue purge operations.
- **Redis probes (`RedisProbes/`)**:
  - `FLUSHALL` and `FLUSHDB` operations.
  - Batched key cleanup using SCAN + delete.
- **S3 probes (`S3Probes/`)**:
  - Bucket object cleanup (with optional prefix).
  - Bucket cleanup + bucket deletion flow.
- **SQL probes (`SqlProbes/`)**:
  - Table truncation for MSSQL, PostgreSQL, and Oracle.
- **Elastic probes (`ElasticProbes/`)**:
  - Index cleanup by index pattern and query string.
- **Configuration models (`ConfigurationObjects/`)**:
  - Strongly typed probe configuration objects per probe family.
- **Shared extensions (`Extensions/`)**:
  - OpenShift auth/bootstrap helpers.
  - Kubernetes object mutation helpers.

### [QaaS.Common.Probes.Tests](./QaaS.Common.Probes.Tests/)
- NUnit test project for branch/logic coverage of probe behavior and helper extensions.
- Uses Moq-based test doubles for protocol/client interactions.

## Install and Upgrade
Install:

```bash
dotnet add package QaaS.Common.Probes
```

Upgrade:

```bash
dotnet add package QaaS.Common.Probes --version 1.0.0-alpha.2
dotnet restore
```

## Documentation
- Official docs: [thesmoketeam.github.io/qaas-docs](https://thesmoketeam.github.io/qaas-docs/)
- CI workflow: [`.github/workflows/ci.yml`](./.github/workflows/ci.yml)
- NuGet package: [QaaS.Common.Probes](https://www.nuget.org/packages/qaas.common.probes)
