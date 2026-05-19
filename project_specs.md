# project_specs.md — QaaS.Common.Probes

40+ pre-built `IProbe` implementations covering environment lifecycle:
OpenShift / Kubernetes orchestration, message-broker administration,
cache / store maintenance, database cleanup, indexing operations.

## Categories

- **OpenShift / Kubernetes (11)** — scale / update / restart / exec.
- **RabbitMQ admin (11)** — queue, exchange, binding, vhost, user,
  permission, definitions.
- **Redis (4)** — flush-all, flush-db, scan-empty, execute commands.
- **MongoDB (2)** — empty / drop collection.
- **S3 (3)** — bucket lifecycle.
- **SQL (3)** — MS-SQL, PostgreSQL, Oracle truncation.
- **Elasticsearch (2)** — empty / delete indices.

## OpenShift specifics

- `Openshift.AllowInvalidServerCertificates` defaults to `true`.
- `OsExecuteCommandsInContainers` parses Kubernetes' status stream
  (channel 3) to detect non-zero exits. Streams must be created
  **before** `StreamDemuxer.Start()`.

## Public surface

- `BaseProbe<TConfig>` is the inheritance root.
- Configurations are records with DataAnnotations.

## Build, packaging, CI

- Target: `.NET 10.0`. NuGet: `QaaS.Common.Probes`.
- CI: standard pipeline + package metadata validation
  (`.github/workflows/ci.yml`, ~446 lines).

## Dependencies

- `KubernetesClient` 19.0.2.
- `AWSSDK.S3` 4.x.
- `Microsoft.Data.SqlClient` 6.x.
- `QaaS.Framework.SDK` 1.4.2 family.

## References

- Live docs: <https://docs.qaas.online/probes/>
