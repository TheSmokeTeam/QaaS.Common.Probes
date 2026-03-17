using QaaS.Framework.SDK.ContextObjects;
using QaaS.Framework.SDK.Session.SessionDataObjects;
using QaaS.Framework.SDK.Session.SessionDataObjects.RunningSessionsObjects;
using Serilog;
using Serilog.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace QaaS.Common.Probes.Tests;

public static class Globals
{
    public static readonly ILogger Logger = new SerilogLoggerFactory(
        new LoggerConfiguration().MinimumLevel.Warning()
            .WriteTo.NUnitOutput()
            .CreateLogger()).CreateLogger("TestsLogger");

    public static readonly InternalContext Context = new()
    {
        Logger = Logger,
        InternalRunningSessions =
            new RunningSessions(new Dictionary<string, RunningSessionData<object, object>>())
    };
}
