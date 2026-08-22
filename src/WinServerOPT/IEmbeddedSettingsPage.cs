namespace WinOpt;

/// <summary>主窗口嵌入的设置页：修改立即生效，需支持从系统重新读取状态。</summary>
internal interface IEmbeddedSettingsPage
{
    void RefreshFromSystem();

    /// <summary>
    /// 预热已加载成功时返回 true 并清除标记，供首次挂载跳过立刻再刷。
    /// 未实现预热跳过的页面恒为 false。
    /// </summary>
    bool ConsumeWarmLoadSkip();
}
