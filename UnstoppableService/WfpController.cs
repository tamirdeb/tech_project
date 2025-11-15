using H.Wfp;
using System.Net;

namespace UnstoppableService;

public class WfpController
{
    private static readonly Guid WfpBlockProviderGuid = new("{33333333-3333-3333-3333-333333333333}");
    private WfpSession? _session;

    public void Start()
    {
        _session = new WfpSession(provider: WfpBlockProviderGuid);

        // Block all outbound traffic to google.com (172.217.168.238)
        var ipAddress = IPAddress.Parse("172.217.168.238");
        _session.Filters.Add(new WfpFilter(
            layer: WfpLayer.AleAuthConnectV4,
            name: "Block Google outbound",
            action: WfpActionType.Block)
        {
            Conditions = new[]
            {
                new WfpCondition(WfpField.IpRemoteAddress, WfpMatchType.Equal, ipAddress),
            }
        });
    }

    public void Stop()
    {
        _session?.Dispose();
    }
}
