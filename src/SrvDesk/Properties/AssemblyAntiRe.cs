using System.Runtime.CompilerServices;

// 抑制 ildasm / 部分托管反编译器默认入口；真正抗分析仍靠交付 Obfuscar（Unicode 名 + 字符串加密）。
[assembly: SuppressIldasm]
