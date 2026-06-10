# AGENTS.md — QaaS.Common.Probes

Guidance for AI agents working in this repository.

## What this repo is

The stock **`IProbe` hook library** for the QaaS platform: 41 pre-built probes for infrastructure setup, teardown, and diagnostics before/after test pipelines. Probes inherit `BaseProbe<TConfiguration>` (from `QaaS.Framework.SDK`) and are discovered at runtime by Framework assembly scanning (order: `QaaS.*` → `Common.*` → user assemblies). Tier-1: sits between QaaS.Framework and Runner/Mocker/user test projects; ships as the `QaaS.Common.Probes` NuGet package.

## Probe families

| Folder | Count | Examples |
|---|---|---|
| `OsProbes/` (Kubernetes/OpenShift) | 11 | OsScaleDeploymentPods, OsUpdateDeploymentImage, OsRestartPods, OsExecuteCommandsInContainers |
| `RabbitMqProbes/` | 15 | Create/Delete Exchange/Queue/Bindings/Users/Vhosts/Permissions, PurgeRabbitMqQueues, Download/UploadDefinitions, UpsertRabbitMqPermissions |
| `RedisProbes/` | 5 | FlushAllRedis, FlushDbRedis, EmptyRedisByChunks (SCAN+batch), ExecuteRedisCommand(s) |
| `S3Probes/` | 3 | CreateS3Bucket, DeleteS3Bucket, EmptyS3Bucket |
| `SqlProbes/` | 3 | MsSql/OracleSql/PostgreSql DataBaseTablesTruncate |
| `MongoDbProbes/` | 2 | DropMongoDbCollection, EmptyMongoDbCollection |
| `ElasticProbes/` | 2 | index cleanup probes |

Each probe has a strongly-typed configuration class (DataAnnotations-validated) consumed from Runner YAML.

## Build & test

```powershell
dotnet restore
dotnet build -m --no-restore
dotnet test --no-build          # NUnit, QaaS.Common.Probes.Tests
```

## Critical gotchas

- **Naming is contract**: probe class names are referenced verbatim in user YAML (`Probe: FlushAllRedis`) and baked into generated JSON schemas via QaaS.JsonSchemaExtensions → renames are breaking changes across the ecosystem and docs.
- Configuration class shape feeds the **family JSON schema** published by QaaS.PackageMirror — property renames break editor validation for every user.
- `OsExecuteCommandsInContainers` uses Kubernetes status-stream (stream 3) demultiplexing to detect non-zero exit codes — fragile; test against a real cluster before changing.
- Versioning: csproj `VersionPrefix 0.0.0`; real versions come from stable Git tags (`X.X.X`) during CI packaging. Don't hand-edit versions.
- CI (windows-latest) collects line coverage via dotnet-coverage and reports it; no minimum threshold is enforced.
- Probes run against live infrastructure (K8s, RabbitMQ, Redis, S3, SQL, Mongo, Elastic) — unit tests must mock clients, never require live services.

## Process

Non-trivial changes follow the QaaS harness pipeline: plan → contract → implement → adversarial evaluation (rubric: correctness, completeness, craft, robustness — each ≥7/10). Write failing NUnit tests first; never mock the class under test. Conventional commits (`feat:`, `fix:`, `chore:`).
