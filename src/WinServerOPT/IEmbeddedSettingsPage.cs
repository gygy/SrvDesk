namespace WinOpt;

/// <summary>主窗口嵌入的设置页：修改立即生效，需支持从系统重新读取状态。</summary>
internal interface IEmbeddedSettingsPage
{
    void RefreshFromSystem();
}
