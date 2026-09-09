using System.ServiceProcess;

namespace SrvDesk;

/// <summary>关键服务保护与风险分级。</summary>
internal static class ServiceRiskCatalog
{
    private static readonly HashSet<string> Critical = new(StringComparer.OrdinalIgnoreCase)
    {
        "RpcSs", "RpcEptMapper", "DcomLaunch", "LSM", "EventLog", "EventSystem",
        "PlugPlay", "Power", "ProfSvc", "SamSs", "Schedule", "Winmgmt",
        "LanmanServer", "LanmanWorkstation", "BFE", "mpssvc", "WdNisSvc", "WinDefend",
        "CryptSvc", "Dhcp", "Dnscache", "NlaSvc", "netprofm", "nsi",
        "BrokerInfrastructure", "SystemEventsBroker", "UserManager", "FontCache",
    };

    private static readonly HashSet<string> High = new(StringComparer.OrdinalIgnoreCase)
    {
        "wuauserv", "UsoSvc", "WaaSMedicSvc", "bits", "DoSvc",
        "TermService", "SessionEnv", "UmRdpService", "WinRM", "sshd",
        "W3SVC", "WAS", "vmms", "docker", "com.docker.service",
    };

    public static OptimizeRisk GetRisk(string serviceName, ServiceRecommend recommend)
    {
        if (string.IsNullOrWhiteSpace(serviceName)) return OptimizeRisk.Medium;
        if (Critical.Contains(serviceName)) return OptimizeRisk.Critical;
        if (High.Contains(serviceName)) return OptimizeRisk.High;
        if (ServerProfile.MustKeepService(serviceName)) return OptimizeRisk.High;
        return recommend switch
        {
            ServiceRecommend.Disable => OptimizeRisk.Low,
            ServiceRecommend.Manual => OptimizeRisk.Low,
            ServiceRecommend.Auto => OptimizeRisk.Medium,
            _ => OptimizeRisk.Medium,
        };
    }

    public static string WhyKeep(string serviceName)
    {
        if (Critical.Contains(serviceName))
            return AppLang.L("系统核心服务，禁用会导致不稳定或无法启动。", "Core OS service; disabling risks instability.");
        if (ServerProfile.MustKeepService(serviceName))
            return AppLang.L("与当前服务器用途相关，建议保留。", "Required by current server profile; keep it.");
        if (High.Contains(serviceName))
            return AppLang.L("安全更新、远程或关键角色依赖，请谨慎。", "Tied to updates, remote access, or key roles; be careful.");
        return "";
    }
}

internal static class ServiceDependencyHelper
{
    public static (string[] DependsOn, string[] DependedBy) Get(string serviceName)
    {
        try
        {
            using var sc = new ServiceController(serviceName);
            var depends = sc.ServicesDependedOn?
                .Select(s => s.ServiceName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .ToArray() ?? Array.Empty<string>();

            var depended = new List<string>();
            foreach (var other in ServiceController.GetServices())
            {
                try
                {
                    if (other.ServiceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase))
                        continue;
                    foreach (var d in other.ServicesDependedOn ?? Array.Empty<ServiceController>())
                    {
                        if (d.ServiceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase))
                        {
                            depended.Add(other.ServiceName);
                            break;
                        }
                    }
                }
                catch { /* ignore */ }
                finally { other.Dispose(); }
            }

            return (depends, depended
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .Take(40)
                .ToArray());
        }
        catch
        {
            return (Array.Empty<string>(), Array.Empty<string>());
        }
    }

    public static string FormatExplain(string serviceName, string note, ServiceRecommend recommend)
    {
        var risk = ServiceRiskCatalog.GetRisk(serviceName, recommend);
        var (dep, by) = Get(serviceName);
        var why = recommend switch
        {
            ServiceRecommend.Disable => AppLang.L("当前场景通常不需要该服务持续运行。", "Usually not needed for this scenario."),
            ServiceRecommend.Manual => AppLang.L("按需启动即可，不必开机自动。", "Start on demand; need not run at boot."),
            ServiceRecommend.Auto => AppLang.L("桌面/角色体验依赖该服务。", "Needed for desktop/role experience."),
            _ => AppLang.L("保持系统默认或现状。", "Keep default / current."),
        };
        var keep = ServiceRiskCatalog.WhyKeep(serviceName);
        if (!string.IsNullOrEmpty(keep)) why = keep;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine(AppLang.L("原因：", "Why: ") + why);
        if (!string.IsNullOrWhiteSpace(note))
            sb.AppendLine(AppLang.L("说明：", "Note: ") + note.Trim());
        sb.AppendLine(AppLang.L("风险：", "Risk: ") + OptimizationLevelUi.RiskText(risk));
        sb.AppendLine(AppLang.L("依赖：", "Depends on: ") + (dep.Length == 0 ? "—" : string.Join(", ", dep.Take(8))));
        sb.AppendLine(AppLang.L("被依赖：", "Depended by: ") + (by.Length == 0 ? "—" : string.Join(", ", by.Take(8))));
        sb.Append(AppLang.L("可回滚：是（服务启动类型快照）", "Rollback: yes (service start-type snapshot)"));
        return sb.ToString();
    }
}
