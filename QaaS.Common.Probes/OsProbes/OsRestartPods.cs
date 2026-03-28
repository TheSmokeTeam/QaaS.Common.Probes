using k8s;
using k8s.Autorest;
using k8s.Models;
using Microsoft.Extensions.Logging;
using QaaS.Common.Probes.ConfigurationObjects.Os;
using Timer = System.Timers.Timer;

namespace QaaS.Common.Probes.OsProbes;

/// <summary>
/// Probe that restarts all pods with configured labels in the configured namespace
/// </summary>
/// <qaas-docs group="Cluster orchestration" subgroup="Pod restarts" />
public class OsRestartPods : BaseOsProbe<OsRestartPodsConfig>
{
    protected override void RunOsProbe()
    {
        var podsList = new List<V1PodList>();
        foreach (var label in Configuration.ApplicationLabels!)
        {
            try
            {
                var podsForLabel =
                    Kubernetes.ListNamespacedPod(Configuration.Openshift!.Namespace, labelSelector: label);
                podsList.Add(podsForLabel);
                Context.Logger.LogDebug("Found {NumberOfPods} pods for label {LabelOfPods}",
                    podsForLabel.Items.Count, label);
            }
            catch (HttpOperationException exception)
            {
                Context.Logger.LogError("Encountered HttpOperationException when getting pods with label " +
                                        "{LabelOfPods}, the exception is {ExceptionContent}",
                    label, $"{exception.Response.Content}\n{exception}");
            }
        }

        var totalPodCount = podsList.Sum(item => item.Items.Count);
        Context.Logger.LogInformation("Found {NumberOfPods} pods for given labels {GivenLabels}",
            totalPodCount, string.Join(", ", Configuration.ApplicationLabels!));

        // if no pods were found skip rest of the function
        if (totalPodCount <= 0)
        {
            Context.Logger.LogError("Found no pods matching any of the given labels {GivenLabels}," +
                                    " restart pods probe won't run",
                string.Join(", ", Configuration.ApplicationLabels!));
            return;
        }

        Context.Logger.LogInformation("Now Deleting pods...");
        foreach (var pod in podsList.SelectMany(pods => pods.Items))
        {
            try
            {
                Kubernetes.DeleteNamespacedPod(pod.Name(), Configuration.Openshift!.Namespace);
                Context.Logger.LogDebug("Deleted pod {PodName}", pod.Name());
            }
            catch (HttpOperationException exception)
            {
                Context.Logger.LogError("Encountered HttpOperationException when deleting pod {PodName} " +
                                        ", the exception is {ExceptionContent}",
                    pod.Name(), $"{exception.Response.Content}\n{exception}");
            }
        }

        Context.Logger.LogInformation("Waiting for pods to be ready again...");
        var timer = new Timer { Interval = Configuration.TimeoutWaitForDesiredStateSeconds * 1000, AutoReset = false };
        timer.Start();
        try
        {
            Wait(AreAppsReady, timer);
            Wait(() => !AreAppsReady(), timer);
            Context.Logger.LogInformation("All microservices are ready");
        }
        catch (TimeoutException e)
        {
            Context.Logger.LogError("Encountered Exception while waiting for apps to be ready -" +
                                    " {ExceptionMessage}",
                e.Message);
        }

        timer.Dispose();
    }

    private void Wait(Func<bool> func, Timer timer)
    {
        while (func())
        {
            Thread.Sleep(Configuration.IntervalBetweenDesiredStateChecksMs);

            if (timer.Enabled) continue;
            throw new TimeoutException("Timeout for microservices to be ready has been exceeded");
        }
    }

    private bool AreAppsReady()
    {
        var areDeploymentsReady = AreDeploymentsReady() ?? true;
        var areStatefulSetsReady = AreStatefulSetsReady() ?? true;
        return areDeploymentsReady && areStatefulSetsReady;
    }

    private bool? AreStatefulSetsReady()
    {
        var statefulSetLists = new List<V1StatefulSetList>();
        foreach (var label in Configuration.ApplicationLabels!)
        {
            V1StatefulSetList statefulSetList;
            try
            {
                statefulSetList =
                    Kubernetes.ListNamespacedStatefulSet(Configuration.Openshift!.Namespace, labelSelector: label);
            }
            catch (HttpOperationException exception)
            {
                throw new HttpOperationException($"{exception.Response.Content}\n{exception}");
            }

            if (statefulSetList.Items.Count > 0) statefulSetLists.Add(statefulSetList);
        }

        if (statefulSetLists.Count <= 0) return null;

        return statefulSetLists
            .SelectMany(statefulSets => statefulSets.Items)
            .All(statefulSet => statefulSet.Status.ReadyReplicas.Equals(statefulSet.Spec.Replicas));
    }

    private bool? AreDeploymentsReady()
    {
        var deploymentList = new List<V1DeploymentList>();

        foreach (var label in Configuration.ApplicationLabels!.ToList())
        {
            V1DeploymentList deployments;
            try
            {
                deployments =
                    Kubernetes.ListNamespacedDeployment(Configuration.Openshift!.Namespace, labelSelector: label);
            }
            catch (HttpOperationException exception)
            {
                throw new HttpOperationException($"{exception.Response.Content}\n{exception}");
            }

            if (deployments.Items.Count > 0) deploymentList.Add(deployments);
        }

        if (deploymentList.Count <= 0) return null;

        return !deploymentList
            .Any(deployments => deployments.Items
                .Any(deployment => deployment.Status.Conditions
                    .Any(condition => condition.Type.Equals("Available") && condition.Status.Equals("False"))));
    }
}
