# QaaS.Common.Probes

[![NuGet Version](https://img.shields.io/nuget/v/QaaS.Common.Probes?label=NuGet%20Version)](https://www.nuget.org/packages/qaas.common.probes/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/QaaS.Common.Probes?label=NuGet%20Downloads)](https://www.nuget.org/packages/qaas.common.probes/)
[![QaaS.Common.Probes Coverage](https://img.shields.io/badge/coverage-47.99%25-yellow)](./QaaS.Common.Probes.Tests/TestResults)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)

Reusable probe implementations for QaaS workflows.
This repository contains one solution, `QaaS.Common.Probes.sln`, with one publishable NuGet package and one NUnit test project.

## Table of Contents

- [What Is Included](#what-is-included)
- [NuGet Package](#nuget-package)
- [Functionalities](#functionalities)
- [Installation](#installation)
- [Usage Pattern](#usage-pattern)
- [Testing and Coverage](#testing-and-coverage)
- [Documentation](#documentation)
- [Build and Release](#build-and-release)

## What Is Included

| Solution | Project | Type | Purpose |
| --- | --- | --- | --- |
| `QaaS.Common.Probes.sln` | `QaaS.Common.Probes` | Class library / NuGet package | Shared probes for OpenShift/Kubernetes, RabbitMQ, Redis, S3, SQL, and Elastic workflows |
| `QaaS.Common.Probes.sln` | `QaaS.Common.Probes.Tests` | NUnit test project | Unit tests and behavior checks for probe logic |

## NuGet Package

| Package | Latest | Downloads | Notes |
| --- | --- | --- | --- |
| [`QaaS.Common.Probes`](https://www.nuget.org/packages/qaas.common.probes/) | ![NuGet Version](https://img.shields.io/nuget/v/QaaS.Common.Probes?label=latest) | ![NuGet Downloads](https://img.shields.io/nuget/dt/QaaS.Common.Probes?label=downloads) | Published package for shared QaaS probes |

## Functionalities

### OpenShift / Kubernetes Probes

- Scale deployment pods: `OsScaleDeploymentPods`
- Scale stateful set pods: `OsScaleStatefulSetPods`
- Restart pods by labels and wait for recovery: `OsRestartPods`
- Change deployment environment variables: `OsChangeDeploymentEnvVars`
- Change stateful set environment variables: `OsChangeStatefulSetEnvVars`
- Update deployment image: `OsUpdateDeploymentImage`
- Update stateful set image: `OsUpdateStatefulSetImage`
- Update deployment resources: `OsUpdateDeploymentResources`
- Update stateful set resources: `OsUpdateStatefulSetResources`
- Edit YAML values inside ConfigMaps: `OsEditYamlConfigMap`
- Execute commands in pod containers: `OsExecuteCommandsInContainers`

### RabbitMQ Probes

- Create queues: `CreateRabbitMqQueues`
- Create exchanges: `CreateRabbitMqExchanges`
- Create bindings (exchange->queue / exchange->exchange): `CreateRabbitMqBindings`
- Delete queues: `DeleteRabbitMqQueues`
- Delete exchanges: `DeleteRabbitMqExchanges`
- Delete bindings: `DeleteRabbitMqBindings`
- Purge queue messages: `PurgeRabbitMqQueues`

### Redis Probes

- Flush all Redis data: `FlushAllRedis`
- Flush a specific Redis database: `FlushDbRedis`
- Empty Redis in chunks (scan/delete loop): `EmptyRedisByChunks<TConfig>`

### S3 Probes

- Empty objects in bucket (optional prefix): `EmptyS3Bucket`
- Empty and delete bucket: `DeleteS3Bucket`

### SQL Probes

- Truncate tables in MSSQL: `MsSqlDataBaseTablesTruncate`
- Truncate tables in PostgreSQL: `PostgreSqlDataBaseTablesTruncate`
- Truncate tables in Oracle: `OracleSqlDataBaseTablesTruncate`

### Elastic Probes

- Empty indices by pattern and query string: `EmptyElasticIndices`

## Installation

```bash
dotnet add package QaaS.Common.Probes
```

## Usage Pattern

The probes are designed for use from QaaS execution flows via `BaseProbe<TConfig>` and typed configuration objects.

```csharp
using QaaS.Common.Probes.ConfigurationObjects.Redis;
using QaaS.Common.Probes.RedisProbes;

var probe = new FlushDbRedis
{
    Context = qaasContext,
    Configuration = new RedisDataBaseProbeBaseConfig
    {
        HostNames = ["redis:6379"],
        RedisDataBase = 2,
        Password = "***"
    }
};

probe.Run(sessionDataList, dataSourceList);
```

For full probe setup examples and framework integration details, see the project docs.

## Testing and Coverage

Coverage was generated on **March 6, 2026** with:

```bash
dotnet test QaaS.Common.Probes.sln --configuration Release --collect:"XPlat Code Coverage"
```

### Project Coverage

| Project | Coverage Badge | Details |
| --- | --- | --- |
| `QaaS.Common.Probes` | ![QaaS.Common.Probes Coverage](https://img.shields.io/badge/line%20coverage-47.99%25-yellow) | Lines: `299/623`, Branches: `92/166` (`55.42%`) |
| `QaaS.Common.Probes.Tests` | ![QaaS.Common.Probes.Tests](https://img.shields.io/badge/coverage-test%20project-lightgrey) | `49` tests passed in latest run |

### Coverage by Area (`QaaS.Common.Probes`)

| Area | Coverage |
| --- | --- |
| ConfigurationObjects | ![ConfigurationObjects](https://img.shields.io/badge/ConfigurationObjects-65.00%25-yellowgreen) |
| ElasticProbes | ![ElasticProbes](https://img.shields.io/badge/ElasticProbes-55.88%25-yellow) |
| Extensions | ![Extensions](https://img.shields.io/badge/Extensions-45.45%25-orange) |
| OsProbes | ![OsProbes](https://img.shields.io/badge/OsProbes-21.07%25-red) |
| RabbitMqProbes | ![RabbitMqProbes](https://img.shields.io/badge/RabbitMqProbes-80.72%25-brightgreen) |
| RedisProbes | ![RedisProbes](https://img.shields.io/badge/RedisProbes-77.78%25-yellowgreen) |
| S3Probes | ![S3Probes](https://img.shields.io/badge/S3Probes-71.79%25-yellowgreen) |
| SqlProbes | ![SqlProbes](https://img.shields.io/badge/SqlProbes-89.47%25-brightgreen) |

## Documentation

- QaaS documentation: [thesmoketeam.github.io/qaas-docs](https://thesmoketeam.github.io/qaas-docs/)
- Probe source code: [`/QaaS.Common.Probes`](./QaaS.Common.Probes)
- Tests: [`/QaaS.Common.Probes.Tests`](./QaaS.Common.Probes.Tests)

## Build and Release

```bash
dotnet restore QaaS.Common.Probes.sln
dotnet build QaaS.Common.Probes.sln --configuration Release
dotnet test QaaS.Common.Probes.sln --configuration Release
dotnet pack QaaS.Common.Probes/QaaS.Common.Probes.csproj --configuration Release --output BuildOutput
```

GitHub Actions workflow: [`/.github/workflows/ci.yml`](./.github/workflows/ci.yml)

Package publishing is triggered from version tags in CI.
