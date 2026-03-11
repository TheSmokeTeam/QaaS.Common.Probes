using k8s.Models;
using NUnit.Framework;
using QaaS.Common.Probes.ConfigurationObjects.Os;
using QaaS.Common.Probes.OsProbes;

namespace QaaS.Common.Probes.Tests;

[TestFixture]
public class OsProbeLogicTests
{
    private sealed class TestableDeploymentAvailabilityProbe : BaseOsUpdateDeployment<OsUpdatePodsProbeConfig>
    {
        public bool InvokeIsReplicaSetAvailable(V1Deployment deployment) => IsReplicaSetAvailable(deployment);

        protected override V1Deployment UpdateReplicaSet(V1Deployment replicaSet) => replicaSet;
    }

    private sealed class TestableStatefulSetAvailabilityProbe : BaseOsUpdateStatefulSet<OsUpdatePodsProbeConfig>
    {
        public bool InvokeIsReplicaSetAvailable(V1StatefulSet statefulSet) => IsReplicaSetAvailable(statefulSet);

        protected override V1StatefulSet UpdateReplicaSet(V1StatefulSet replicaSet) => replicaSet;
    }

    private sealed class TestableRunLoopProbe : BaseOsUpdatePodsProbe<OsUpdatePodsProbeConfig, string>
    {
        private readonly Queue<bool> _availabilityDecisions;
        private readonly Queue<long?> _observedGenerations;

        public int ReadReplicaSetCalls { get; private set; }
        public int UpdateReplicaSetCalls { get; private set; }
        public int AvailabilityChecks { get; private set; }
        public long? CurrentGeneration { get; private set; }

        public TestableRunLoopProbe(IEnumerable<bool> availabilityDecisions, IEnumerable<long?>? observedGenerations = null)
        {
            _availabilityDecisions = new Queue<bool>(availabilityDecisions);
            _observedGenerations = new Queue<long?>(observedGenerations ?? []);
        }

        public void InvokeRunOsProbe() => RunOsProbe();

        protected override bool IsReplicaSetAvailable(string replicaSet)
        {
            AvailabilityChecks++;
            if (_availabilityDecisions.Count <= 0)
                return false;
            return _availabilityDecisions.Dequeue();
        }

        protected override long? GetReplicaSetGeneration(string replicaSet) => CurrentGeneration;

        protected override long? GetObservedGeneration(string replicaSet)
        {
            return _observedGenerations.Count > 0 ? _observedGenerations.Dequeue() : CurrentGeneration;
        }

        protected override string ReadReplicaSet()
        {
            ReadReplicaSetCalls++;
            return "replica-set";
        }

        protected override string UpdateReplicaSet(string replicaSet)
        {
            UpdateReplicaSetCalls++;
            CurrentGeneration = (CurrentGeneration ?? 0) + 1;
            return replicaSet;
        }
    }

    [Test]
    public void TestDeploymentAvailability_WhenAllExpectedStatusValuesMatch_ShouldReturnTrue()
    {
        // Arrange
        var probe = new TestableDeploymentAvailabilityProbe
        {
            Configuration = CreateBaseConfig("deployment-a"),
            Context = Globals.Context
        };
        var deployment = new V1Deployment
        {
            Metadata = new V1ObjectMeta { Generation = 3 },
            Spec = new V1DeploymentSpec { Replicas = 3 },
            Status = new V1DeploymentStatus
            {
                ObservedGeneration = 3,
                UnavailableReplicas = null,
                AvailableReplicas = 3,
                Replicas = 3,
                UpdatedReplicas = 3,
                ReadyReplicas = 3
            }
        };

        // Act
        var isAvailable = probe.InvokeIsReplicaSetAvailable(deployment);

        // Assert
        Assert.That(isAvailable, Is.True);
    }

    [Test]
    public void TestDeploymentAvailability_WhenUnavailableReplicasExist_ShouldReturnFalse()
    {
        // Arrange
        var probe = new TestableDeploymentAvailabilityProbe
        {
            Configuration = CreateBaseConfig("deployment-a"),
            Context = Globals.Context
        };
        var deployment = new V1Deployment
        {
            Metadata = new V1ObjectMeta { Generation = 3 },
            Spec = new V1DeploymentSpec { Replicas = 3 },
            Status = new V1DeploymentStatus
            {
                ObservedGeneration = 3,
                UnavailableReplicas = 1,
                AvailableReplicas = 2,
                Replicas = 3,
                UpdatedReplicas = 3,
                ReadyReplicas = 2
            }
        };

        // Act
        var isAvailable = probe.InvokeIsReplicaSetAvailable(deployment);

        // Assert
        Assert.That(isAvailable, Is.False);
    }

    [Test]
    public void TestStatefulSetAvailability_WhenAllExpectedStatusValuesMatch_ShouldReturnTrue()
    {
        // Arrange
        var probe = new TestableStatefulSetAvailabilityProbe
        {
            Configuration = CreateBaseConfig("statefulset-a"),
            Context = Globals.Context
        };
        var statefulSet = new V1StatefulSet
        {
            Metadata = new V1ObjectMeta { Generation = 2 },
            Spec = new V1StatefulSetSpec { Replicas = 2 },
            Status = new V1StatefulSetStatus
            {
                ObservedGeneration = 2,
                ReadyReplicas = 2,
                Replicas = 2,
                UpdatedReplicas = 2
            }
        };

        // Act
        var isAvailable = probe.InvokeIsReplicaSetAvailable(statefulSet);

        // Assert
        Assert.That(isAvailable, Is.True);
    }

    [Test]
    public void TestStatefulSetAvailability_WhenNotAllReplicasReady_ShouldReturnFalse()
    {
        // Arrange
        var probe = new TestableStatefulSetAvailabilityProbe
        {
            Configuration = CreateBaseConfig("statefulset-a"),
            Context = Globals.Context
        };
        var statefulSet = new V1StatefulSet
        {
            Metadata = new V1ObjectMeta { Generation = 2 },
            Spec = new V1StatefulSetSpec { Replicas = 2 },
            Status = new V1StatefulSetStatus
            {
                ObservedGeneration = 2,
                ReadyReplicas = 1,
                Replicas = 2,
                UpdatedReplicas = 2
            }
        };

        // Act
        var isAvailable = probe.InvokeIsReplicaSetAvailable(statefulSet);

        // Assert
        Assert.That(isAvailable, Is.False);
    }

    [Test]
    public void TestRunLoop_WhenReplicaSetEventuallyBecomesAvailable_ShouldPollAndComplete()
    {
        // Arrange
        var probe = new TestableRunLoopProbe([false, true])
        {
            Configuration = CreateBaseConfig("replica-set-a") with
            {
                IntervalBetweenDesiredStateChecksMs = 0,
                TimeoutWaitForDesiredStateSeconds = 5
            },
            Context = Globals.Context
        };

        // Act
        probe.InvokeRunOsProbe();

        // Assert
        Assert.That(probe.UpdateReplicaSetCalls, Is.EqualTo(1));
        Assert.That(probe.ReadReplicaSetCalls, Is.GreaterThanOrEqualTo(2));
        Assert.That(probe.AvailabilityChecks, Is.EqualTo(2));
    }

    [Test]
    public void TestRunLoop_WhenGenerationNotObservedYet_ShouldKeepPollingUntilObserved()
    {
        var probe = new TestableRunLoopProbe([true, true], [0, 1])
        {
            Configuration = CreateBaseConfig("replica-set-a") with
            {
                IntervalBetweenDesiredStateChecksMs = 0,
                TimeoutWaitForDesiredStateSeconds = 5
            },
            Context = Globals.Context
        };

        probe.InvokeRunOsProbe();

        Assert.That(probe.UpdateReplicaSetCalls, Is.EqualTo(1));
        Assert.That(probe.ReadReplicaSetCalls, Is.GreaterThanOrEqualTo(3));
        Assert.That(probe.AvailabilityChecks, Is.EqualTo(1));
    }

    [Test]
    public void TestRunLoop_WhenTimeoutReached_ShouldReturnWithoutThrowing()
    {
        // Arrange
        var probe = new TestableRunLoopProbe([false, false, false])
        {
            Configuration = CreateBaseConfig("replica-set-a") with
            {
                IntervalBetweenDesiredStateChecksMs = 0,
                TimeoutWaitForDesiredStateSeconds = 0
            },
            Context = Globals.Context
        };

        // Act + Assert
        Assert.DoesNotThrow(() => probe.InvokeRunOsProbe());
        Assert.That(probe.UpdateReplicaSetCalls, Is.EqualTo(1));
        Assert.That(probe.AvailabilityChecks, Is.EqualTo(1));
    }

    private static OsUpdatePodsProbeConfig CreateBaseConfig(string replicaSetName)
    {
        return new OsUpdatePodsProbeConfig
        {
            ReplicaSetName = replicaSetName,
            Openshift = new Openshift
            {
                Cluster = "cluster",
                Namespace = "namespace",
                Username = "username",
                Password = "password"
            }
        };
    }
}
