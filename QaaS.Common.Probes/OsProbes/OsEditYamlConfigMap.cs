using System.Text;
using k8s;
using k8s.Autorest;
using k8s.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QaaS.Common.Probes.ConfigurationObjects.Os;
using QaaS.Framework.Serialization.Deserializers;

namespace QaaS.Common.Probes.OsProbes;

/// <summary>
/// Probe that edits yaml config maps
/// </summary>
public class OsEditYamlConfigMap : BaseOsProbe<OsEditYamlConfigMapConfig>
{
    protected override void RunOsProbe()
    {
        V1ConfigMap configMap;
        try
        {
            configMap = Kubernetes.ReadNamespacedConfigMap(Configuration.ConfigMapName,
                Configuration.Openshift!.Namespace);
        }
        catch (HttpOperationException)
        {
            throw new ArgumentException(
                $"Could not find configmap '{Configuration.ConfigMapName}' in the configured namespace");
        }

        byte[] yamlBytes;
        try
        {
            yamlBytes = Encoding.UTF8.GetBytes(configMap.Data[Configuration.ConfigMapYamlFileName]);
        }
        catch (KeyNotFoundException)
        {
            throw new ArgumentException($"Could not find yaml file '{Configuration.ConfigMapYamlFileName}' in the " +
                                        $"configured configmap '{Configuration.ConfigMapName}'");
        }

        var originalYaml = new Yaml().Deserialize(yamlBytes) as Dictionary<object, object>;

        // Convert to json
        var json = JsonConvert.SerializeObject(originalYaml);
        var jToken = JToken.Parse(json);

        foreach (var valueToEdit in Configuration.ValuesToEdit)
        {
            var token = jToken.SelectToken(valueToEdit.Key);
            if (token != null)
                token.Replace(JToken.FromObject(valueToEdit.Value));
            else
                Context.Logger.LogWarning("Yaml path not found: {yamlPath}", valueToEdit.Key);
        }

        // Convert back to yaml
        var updatedObject = ConvertToDotNetObject(jToken);
        var updatedYamlString =
            Encoding.UTF8.GetString(new Framework.Serialization.Serializers.Yaml().Serialize(updatedObject)!);
        configMap.Data[Configuration.ConfigMapYamlFileName] = updatedYamlString;

        Kubernetes.ReplaceNamespacedConfigMap(configMap, Configuration.ConfigMapName,
            Configuration.Openshift!.Namespace);
    }

    private static object? ConvertToDotNetObject(object token)
    {
        return token switch
        {
            JObject jObject => jObject.ToObject<Dictionary<string, object>>()!
                .ToDictionary(kvp => kvp.Key, kvp => ConvertToDotNetObject(kvp.Value)),
            JArray jArray => jArray.Select(ConvertToDotNetObject),
            JValue jValue => jValue.Value,
            _ => token
        };
    }
}