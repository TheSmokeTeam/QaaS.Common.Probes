# CLAUDE.md — QaaS.Common.Probes (library)

> Project-level operating manual. See repo root `CLAUDE.md` and
> `project_specs.md`.

## Purpose

Pre-built `IProbe` implementations for environment setup / teardown:
RabbitMQ, Redis, MongoDB, S3, SQL (MS-SQL / PostgreSQL / Oracle),
Elasticsearch, OpenShift / Kubernetes.

## Source folders

- `OsProbes/` — 11 probes: scale / image / resources / env-vars /
  ConfigMap / restart-pods / **`OsExecuteCommandsInContainers.cs`**
  (Kubernetes WS exec, status-stream-aware).
- `RabbitMqProbes/` — queue / exchange / binding / vhost / user /
  permission CRUD + definitions up/download (11).
- `RedisProbes/` — flush-all, flush-db, scan-and-delete, execute (4).
- `MongoDbProbes/` — empty / drop collection (2).
- `S3Probes/` — create / delete / empty bucket (3).
- `SqlProbes/` — MS-SQL / PostgreSQL / Oracle truncation (3).
- `ElasticProbes/` — empty / delete indices (2).
- `ConfigurationObjects/`, `Extensions/`, `Infrastructure/`.

## Critical invariants

- `OsExecuteCommandsInContainers.cs:62-72` — open output / error /
  **status (stream 3)** *before* `demux.Start()`. Reordering loses
  non-zero exit detection.
- `ConfigurationObjects/Os/Openshift.cs:25` —
  `AllowInvalidServerCertificates` defaults to `true` to keep
  self-signed clusters working. **Do not "fix" this default.**

## Forbidden

1. Tightening `AllowInvalidServerCertificates` default to `false`.
2. Skipping the exec status stream (stream 3) — relying on stderr
   silently swallows non-zero exits.
3. Leaking WS connections — `using var demux` is mandatory.
4. Hard-coding TLS verification.
5. Truncating SQL without documenting FK cascade behaviour.
6. Deleting an S3 bucket without first emptying it.
7. Mutating shared global probe dictionary state without a `lock`.

## Build

```bash
dotnet build ../QaaS.Common.Probes.sln --nologo -clp:ErrorsOnly
csharpier format <changed-files>
```

Framework SDK 1.4.2; KubernetesClient 19.0.2.
