# project_specs.md — QaaS.Common.Probes (package project)

Single package project, probes grouped by target system.

## Folders

- `OsProbes/` — OpenShift / Kubernetes.
- `RabbitMqProbes/`, `RedisProbes/`, `MongoDbProbes/`,
  `S3Probes/`, `SqlProbes/`, `ElasticProbes/`.
- `ConfigurationObjects/` — config records (e.g. `Os/Openshift.cs`).

## Forbidden

- Tightening security-affecting defaults without a major-version bump
  (e.g. `AllowInvalidServerCertificates`).
- Bypassing the Kubernetes exec status stream.
- Adding non-probe hook implementations.
