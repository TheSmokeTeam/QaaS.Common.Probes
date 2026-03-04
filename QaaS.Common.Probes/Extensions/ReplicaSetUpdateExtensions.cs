using k8s.Models;
using Microsoft.IdentityModel.Tokens;
using QaaS.Common.Probes.ConfigurationObjects.Os;

namespace QaaS.Common.Probes.Extensions;

public static class ReplicaSetUpdateExtensions
{
    private const string Cpu = "cpu", Memory = "memory";

    private static string? GetQuantity(IDictionary<string, ResourceQuantity>? resources, string key)
    {
        if (resources == null || !resources.TryGetValue(key, out var resourceQuantity))
            return null;
        return resourceQuantity.ToString();
    }

    private static V1Container GetContainerFromTemplate(V1PodTemplateSpec template, string containerName,
        string replicaSetName)
    {
        var containersList = template.Spec.Containers;
        var container = containersList.FirstOrDefault(container => container.Name == containerName) ??
                        throw new InvalidOperationException(
                            $"Container {containerName} not found in {replicaSetName} replicaSet's containers");
        return container;
    }

    /// <summary>
    /// Updates replicaset's template's resources
    /// </summary>
    /// <param name="template">The replicaset's template</param>
    /// <param name="containerName">The name of the container to update its resources</param>
    /// <param name="replicaSetName">The replicaset's name</param>
    /// <param name="desiredResources">The resources to update the replicaset with</param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException">If can't find the given container in the given template</exception>
    public static V1PodTemplateSpec UpdateReplicaSetResources(this V1PodTemplateSpec template, string containerName,
        string replicaSetName, Resources? desiredResources)
    {
        var container = GetContainerFromTemplate(template, containerName, replicaSetName);
        var containerResources = container.Resources;

        // Overrides replicaSet based on configuration
        var newLimits = new Dictionary<string, ResourceQuantity>();
        var newRequests = new Dictionary<string, ResourceQuantity>();
        var cpuLimit = desiredResources?.Limits?.Cpu ??
                       GetQuantity(containerResources.Limits, Cpu);
        var memoryLimit = desiredResources?.Limits?.Memory ??
                          GetQuantity(containerResources.Limits, Memory);
        var cpuRequests = desiredResources?.Requests?.Cpu ??
                          GetQuantity(containerResources.Requests, Cpu);
        var memoryRequests = desiredResources?.Requests?.Memory ??
                             GetQuantity(containerResources.Requests, Memory);
        if (cpuLimit != null)
            newLimits.Add(Cpu, new ResourceQuantity(cpuLimit));
        if (memoryLimit != null)
            newLimits.Add(Memory, new ResourceQuantity(memoryLimit));
        if (cpuRequests != null)
            newRequests.Add(Cpu, new ResourceQuantity(cpuRequests));
        if (memoryRequests != null)
            newRequests.Add(Memory, new ResourceQuantity(memoryRequests));

        container.Resources = new V1ResourceRequirements
        {
            Limits = newLimits,
            Requests = newRequests
        };
        return template;
    }

    public static V1PodTemplateSpec UpdateReplicaSetImage(this V1PodTemplateSpec template, string containerName,
        string replicaSetName,
        string desiredImage)
    {
        var container = GetContainerFromTemplate(template, containerName, replicaSetName);
        container.Image = desiredImage;
        return template;
    }

    /// <summary>
    /// Changes replica sets containers envVar.
    /// Changes all containers envVar if no specific container to change is specified.
    /// If containerToChange is specified then its the only container that is changed.
    /// </summary>
    /// <param name="containers"> The containers of the replicaset </param>
    /// <param name="envVarsToUpdate"> The envVars you wish to change key - envVar name, value - the new value </param>
    /// <param name="envVarsToRemove"> The envVars you wish to remove </param>
    /// <param name="containerToChange"> Specifies the name of the specific container youd like to update,
    ///                                     if you dont wish to update the entire replicaset </param>
    /// <exception cref="ArgumentException"> Is thrown if the containerToChange does not exist </exception>
    public static void ChangeReplicaSetEnvVars(IList<V1Container> containers,
        Dictionary<string, string?> envVarsToUpdate,
        IList<string> envVarsToRemove, string? containerToChange)
    {
        // if the user configured a specific container to change
        if (containerToChange != null)
        {
            var container = containers.FirstOrDefault(c => c.Name.Equals(containerToChange)) ??
                            throw new ArgumentException($"Could not find a container named '{containerToChange}'");
            ChangeContainerEnvVars(container, envVarsToUpdate, envVarsToRemove);
        }
        else
            foreach (var container in containers)
                ChangeContainerEnvVars(container, envVarsToUpdate, envVarsToRemove);
    }

    private static void ChangeContainerEnvVars(V1Container container, Dictionary<string, string?> envVarsToUpdate,
        IList<string> envVarsToRemove)
    {
        var envDict = container.Env == null ? [] : container.Env.ToDictionary(e => e.Name, e => e);

        foreach (var desiredEnvVar in envVarsToUpdate)
        {
            envDict[desiredEnvVar.Key] = new V1EnvVar { Name = desiredEnvVar.Key, Value = desiredEnvVar.Value };
        }

        foreach (var envVarToRemove in envVarsToRemove)
        {
            if (!envDict.Remove(envVarToRemove))
                throw new ArgumentException($"Could not find an env var named '{envVarToRemove}'");
        }

        container.Env = envDict.Values.ToList();
    }
}
