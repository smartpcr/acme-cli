using System.Linq;
using Common;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

namespace AcmeCli
{
    internal class ContextTelemetryInitializer : ITelemetryInitializer
    {
        private readonly ServiceContext _serviceContext;

        public ContextTelemetryInitializer(ServiceContext serviceContext)
        {
            _serviceContext = serviceContext;
        }

        public void Initialize(ITelemetry telemetry)
        {
            telemetry.Context.Cloud.RoleName = _serviceContext.Role;
            telemetry.Context.Component.Version = _serviceContext.Version;
            if (_serviceContext.Tags?.Any() == true)
            {
                telemetry.Context.GlobalProperties["tags"] = string.Join(",", _serviceContext.Tags);
            }
        }
    }
}