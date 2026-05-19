# CLAUDE.md — QaaS.Common.Probes

> Operating manual; see `project_specs.md`. Live docs:
> <https://docs.qaas.online/probes/>.

## Mission

Pre-built `IProbe` implementations for environment setup / teardown:
RabbitMQ admin, Redis maintenance, MongoDB cleanup, S3 buckets, SQL
truncation, Elasticsearch indices, OpenShift / Kubernetes orchestration.

## Build / Test

```bash
dotnet build QaaS.Common.Probes.sln --nologo -clp:ErrorsOnly
dotnet test  QaaS.Common.Probes.sln --nologo --no-build
csharpier format <changed-files>
```

## Shipped probes (40+)

- **OpenShift / Kubernetes (11):** scale deployments / statefulsets,
  update images, update resources, change env vars, edit ConfigMaps,
  restart pods, **`OsExecuteCommandsInContainers`** (status-stream-aware
  exec).
- **RabbitMQ (11):** queue / exchange / binding / vhost / user /
  permission CRUD + definitions up/download.
- **Redis (4):** flush-all, flush-db, scan-and-delete, execute commands.
- **MongoDB (2):** empty / drop collection.
- **S3 (3):** create / delete / empty bucket.
- **SQL (3):** MS-SQL, PostgreSQL, Oracle table truncation.
- **Elastic (2):** empty / delete indices.

## Critical: OpenShift exec

`OsExecuteCommandsInContainers` parses Kubernetes' WebSocket exec
**status stream (3)** — not stderr — to detect non-zero exits. Streams
must be opened **before** `demux.Start()` (ordering matters; see
`OsProbes/OsExecuteCommandsInContainers.cs:62-72`).

`Openshift.AllowInvalidServerCertificates` defaults to **`true`** to
preserve historical behaviour with self-signed clusters
(`ConfigurationObjects/Os/Openshift.cs:25`). Do not "fix" this default —
opt-in stricter validation requires an explicit caller setting.

## Forbidden

1. Tighten `AllowInvalidServerCertificates` default to `false` —
   breaks every existing user with self-signed clusters.
2. Skip the exec status stream (stream 3) — relying on stderr alone
   silently swallows non-zero exits.
3. Leak WebSocket connections — `using var demux` is mandatory.
4. Hard-code TLS verification.
5. Truncate SQL without documenting FK cascade requirements.
6. Delete S3 bucket without first emptying.
7. Use `[Test(Ignore=…)]` to avoid Kubernetes-cluster setup; mark the
   test category and skip via runtime probe instead.
8. Modify shared global probe state without a `lock`.

## Must-verify

1. `dotnet build` / `dotnet test` green.
2. Framework SDK 1.4.2 (`csproj:31`).
3. KubernetesClient 19.0.2 compatibility.
4. `AllowInvalidServerCertificates` default = `true`.
5. Exec status JSON parser handles malformed payloads.
6. SCAN cursor terminates on 0.
7. CI green.

## Recent

- PR #19 (`feature/docs-claude`) — CLAUDE.md drop.
- `f4d0135` — exec status hardening.
- `930b0ed` — OpenShift auth defaults restored.
