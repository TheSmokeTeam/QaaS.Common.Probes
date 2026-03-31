using System.Text;
using Microsoft.Extensions.Configuration;
using k8s;
using k8s.Autorest;
using k8s.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QaaS.Common.Probes.ConfigurationObjects.Os;
using QaaS.Common.Probes.Infrastructure.ProbeGlobalDict;
using QaaS.Framework.Serialization.Deserializers;

namespace QaaS.Common.Probes.OsProbes;

/// <summary>
/// Probe that edits yaml config maps
/// </summary>
/// <qaas-docs group="Cluster orchestration" subgroup="Config maps" />
public class OsEditYamlConfigMap : BaseOsProbeWithGlobalDict<OsEditYamlConfigMapConfig>
{
    protected override IEnumerable<ProbeGlobalDictReadRequest> GetAdditionalGlobalDictionaryReadRequests(
        IConfiguration localConfiguration)
    {
        var configMapName = localConfiguration[nameof(OsEditYamlConfigMapConfig.ConfigMapName)];
        if (!string.IsNullOrWhiteSpace(configMapName))
        {
            yield return new ProbeGlobalDictReadRequest("recovery",
                BuildGlobalDictionaryAliasPath("Os", "Recovery", "ConfigMap", configMapName));
        }
    }

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

        var json = JsonConvert.SerializeObject(originalYaml);
        var jToken = JToken.Parse(json);
        var originalValues = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (var valueToEdit in Configuration.ValuesToEdit)
        {
            var token = jToken.SelectToken(valueToEdit.Key);
            if (token != null)
            {
                originalValues[valueToEdit.Key] = ConvertToDotNetObject(token);
                token.Replace(JToken.FromObject(valueToEdit.Value));
            }
            else
            {
                Context.Logger.LogWarning("Yaml path not found: {yamlPath}", valueToEdit.Key);
            }
        }

        var updatedObject = ConvertToDotNetObject(jToken);
        var updatedYamlString =
            Encoding.UTF8.GetString(new Framework.Serialization.Serializers.Yaml().Serialize(updatedObject)!);
        configMap.Data[Configuration.ConfigMapYamlFileName] = updatedYamlString;

        Kubernetes.ReplaceNamespacedConfigMap(configMap, Configuration.ConfigMapName,
            Configuration.Openshift!.Namespace);

        if (Configuration.UseGlobalDict)
        {
            SaveGlobalDictionaryPayload("recovery", new { ValuesToEdit = originalValues }, GetRecoveryAliasPath());
        }
    }

    private IReadOnlyList<string> GetRecoveryAliasPath()
        => BuildGlobalDictionaryAliasPath("Os", "Recovery", "ConfigMap", Configuration.ConfigMapName!);

    private static object? ConvertToDotNetObject(object token)
    {
        return token switch
        {
            JObject jObject => jObject.ToObject<Dictionary<string, object>>()!
                .ToDictionary(kvp => kvp.Key, kvp => ConvertToDotNetObject(kvp.Value)),
            JArray jArray => jArray.Select(ConvertToDotNetObject).ToList(),
            JValue jValue => jValue.Value,
            _ => token
        };
    }
}
