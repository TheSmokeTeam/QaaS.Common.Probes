# CLAUDE.md — QaaS.Common.Probes.Tests

> Test operating manual. See repo root `CLAUDE.md`.

## Purpose

Coverage for 40+ probes across OpenShift, RabbitMQ, Redis, MongoDB, S3,
SQL, and Elasticsearch. Network and cluster I/O is faked or contained;
only a couple of integration suites hit a live container.

## Layout

- One test file per probe, e.g. `OsExecuteCommandsInContainersTests.cs`,
  `OsRestartPodsTests.cs`, `RabbitMqManagementProbesTests.cs`,
  `S3ProbesTests.cs`, `EmptyMongoDbCollectionTests.cs`,
  `EmptyRedisByChunksTests.cs`, `DeleteElasticIndicesTests.cs`.
- `OsProbeLogicTests.cs` / `OsReplicaSetMutationTests.cs` /
  `ReplicaSetUpdateExtensionsTests.cs` — pure-logic helpers.
- `BaseProbeCoverageTests.cs`, `ProbeGlobalDictionaryTests.cs` —
  base-class + global-dict invariants.
- `RabbitMqProbeBranchTests.cs` — branch-coverage harness.
- HTTP test infra: `TestHttpServer.cs`, `HttpRecordingMessageHandler.cs`.
- `PostgreSqlDataBaseTablesTruncateIntegrationTests.cs` — live
  Postgres integration (env-gated).
- `Globals.cs` — Serilog→MEL `Logger` at `Warning`, plus an
  `InternalContext` carrying an empty `RunningSessions` (see
  `Globals.cs:17-22`).

## Conventions

- **NUnit**. Mocking via Moq + hand-rolled HTTP fakes
  (`TestHttpServer`, `HttpRecordingMessageHandler`).
- Kubernetes: stub `IKubernetes` / WebSocket demux via interfaces.
  Never touch a real cluster from a unit test.
- Integration tests gate on env vars / connection strings and skip
  cleanly when unset — do **not** use `[Test(Ignore=...)]` to dodge
  cluster setup; use a runtime probe + category instead.
- Logger level `Warning` to keep CI logs readable.

## Forbidden

1. `[Test(Ignore=...)]` / `[Explicit]` for environment-dependent tests
   — gate at runtime instead.
2. Hitting a real Kubernetes / S3 / RabbitMQ / Redis from a unit test.
3. Sharing the `TestHttpServer` port across parallel fixtures.
4. Asserting on `stderr` alone for exec exit detection — must check the
   status stream parser.
5. Leaking `HttpClient` / WebSocket — wrap in `using`.

## Run

```bash
dotnet test ../QaaS.Common.Probes.sln --nologo --no-build
```
