using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace QaaS.Common.Probes.ConfigurationObjects.Os;

public record Openshift
{
    [Required, Description("The openshift cluster api (for example REDA)")]
    public string? Cluster { get; set; }

    [Required, Description("Username with access to the openshift namespace and application")]
    public string? Username { get; set; }

    [Required, Description("Password of the username with access to the openshift namespace and application")]
    public string? Password { get; set; }

    [Required, Description("The openshift namespace the application is at")]
    public string? Namespace { get; set; }
}