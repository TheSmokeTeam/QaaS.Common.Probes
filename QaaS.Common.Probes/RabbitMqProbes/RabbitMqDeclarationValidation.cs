using System.Text.Json;
using RabbitMQ.Client.Exceptions;

namespace QaaS.Common.Probes.RabbitMqProbes;

internal static class RabbitMqDeclarationValidation
{
    public static bool IsConfigurationMismatch(Exception exception)
        => exception is OperationInterruptedException or AlreadyClosedException
           && (exception.Message.Contains("PRECONDITION_FAILED", StringComparison.OrdinalIgnoreCase)
               || exception.Message.Contains("inequivalent arg", StringComparison.OrdinalIgnoreCase));

    public static InvalidOperationException CreateConfigurationMismatchException(string objectType, string objectName,
        string requestedConfiguration, Exception innerException)
        => new(
            $"RabbitMQ {objectType} '{objectName}' already exists with a different configuration than requested. Requested configuration: {requestedConfiguration}. Broker error: {innerException.Message}",
            innerException);

    public static string FormatArguments(IReadOnlyDictionary<string, object?>? arguments)
    {
        if (arguments is null || arguments.Count == 0)
        {
            return "{}";
        }

        return JsonSerializer.Serialize(arguments.OrderBy(argument => argument.Key)
            .ToDictionary(argument => argument.Key, argument => argument.Value));
    }
}
