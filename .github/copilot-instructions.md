# Copilot instructions — QaaS.Common.Probes

Read `AGENTS.md` at the repo root first — it lists all 7 probe families (41 probes) and the cross-repo naming contracts.

Essentials:
- net10.0; NUnit tests; build `dotnet build -m`, test `dotnet test --no-build`.
- Probes implement `IProbe` via `BaseProbe<TConfiguration>`; discovered by Framework assembly scanning — class names are referenced verbatim in user YAML and generated JSON schemas, so renames are ecosystem-breaking.
- Configuration classes feed published family schemas (QaaS.PackageMirror) — property renames break user editor validation.
- Versions are tag-driven (`VersionPrefix 0.0.0` + stable Git tags); CI collects and reports line coverage on windows-latest (no threshold enforced).
- Tests must mock infrastructure clients (K8s, RabbitMQ, Redis, S3, SQL, Mongo, Elastic) — never require live services.
- Conventional commits; tests first.
