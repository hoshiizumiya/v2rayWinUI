# v2rayN WinUI3 - 快速参考

## ✅ 已完成功能

### 基础设施
- [x] 项目配置和依赖
- [x] ServiceLib 集成
- [x] App 初始化流程
- [x] Logging 正确引用

### 主窗口
- [x] 窗口框架和布局
- [x] Mica 背景效果
- [x] 菜单栏 (MenuBar)
- [x] 标签页 (TabView)
- [x] 状态栏

### 服务器管理
- [x] 工具栏 (添加/删除/编辑)
- [x] 搜索框
- [x] 列表视图 (ListView)
- [x] 列表头部
- [x] 右键菜单
- [x] 选择处理
- [x] AddServerWindow - 添加服务器窗口 ✨

### 设置窗口
- [x] OptionSettingWindow - 选项设置 ✨
- [x] RoutingSettingWindow - 路由设置 ✨
- [x] DNSSettingWindow - DNS设置 ✨
- [x] SubSettingWindow - 订阅管理 ✨

### 命令系统
- [x] 菜单命令绑定
- [x] 工具栏事件
- [x] ReactiveCommand 集成
- [x] ContentDialog 对话框
- [x] 窗口打开调用

## 🚧 进行中

### 数据绑定
- [x] 服务器列表框架
- [ ] 完整的双向绑定
- [ ] AppEvents 订阅完善

### 核心功能
- [ ] 服务器启动/停止
- [ ] 系统代理设置
- [ ] 订阅更新功能

## ⏳ 待实现

### 窗口
- [ ] GlobalHotkeySettingWindow - 全局热键
- [ ] 测速窗口
- [ ] 日志窗口

### 系统集成
- [ ] 系统托盘 (WinUIEx)
- [ ] 全局热键
- [ ] 自动启动
- [ ] 开机自启

## 🎯 当前优先级

1. **核心功能完善** - 启动/停止服务器
2. **订阅更新** - 实现订阅更新逻辑
3. **系统集成** - 托盘和热键

## 📝 快速命令

### 运行项目
```powershell
# Visual Studio
F5

# 命令行
dotnet run --project v2rayWinUI
```

### 构建
```powershell
dotnet build v2rayWinUI/v2rayWinUI.csproj
```

### 还原包
```powershell
dotnet restore
```

## 🔧 关键代码位置

### 窗口
- `v2rayWinUI/MainWindow.xaml(.cs)` - 主窗口
- `v2rayWinUI/Views/AddServerWindow.xaml(.cs)` - 添加服务器
- `v2rayWinUI/Views/OptionSettingWindow.xaml(.cs)` - 选项设置
- `v2rayWinUI/Views/RoutingSettingWindow.xaml(.cs)` - 路由设置
- `v2rayWinUI/Views/DNSSettingWindow.xaml(.cs)` - DNS设置
- `v2rayWinUI/Views/SubSettingWindow.xaml(.cs)` - 订阅管理

### 应用程序
- `v2rayWinUI/App.xaml(.cs)` - 应用初始化
- `v2rayWinUI/Styles/DefaultStyles.xaml` - 样式

### ViewModel
- `ServiceLib/ViewModels/MainWindowViewModel.cs` - 主窗口VM
- `ServiceLib/ViewModels/ProfilesViewModel.cs` - 列表VM
- `ServiceLib/ViewModels/AddServerViewModel.cs` - 添加服务器VM

## 📊 进度概览

```
Phase 1: 基础框架    ████████████████████ 100%
Phase 2: 核心窗口    ████████████████████  95%
Phase 3: 功能窗口    ████████████░░░░░░░░  60%
Phase 4: 系统集成    ░░░░░░░░░░░░░░░░░░░░   0%
Phase 5: 优化打磨    ░░░░░░░░░░░░░░░░░░░░   0%

总体进度: ██████████████░░░░░░░░░░░░░░ 55%
```

## 💡 提示

### WinUI 3 vs WPF
- `TabControl` → `TabView`
- `MessageBox` → `ContentDialog`
- `ContextMenu` → `MenuFlyout`
- `Binding` → `x:Bind` (推荐)

### 对话框
```csharp
var dialog = new ContentDialog
{
    XamlRoot = this.Content.XamlRoot, // 必须设置!
    Title = "标题",
    Content = "内容",
    PrimaryButtonText = "确定",
    CloseButtonText = "取消"
};
await dialog.ShowAsync();
```

### 窗口打开
```csharp
var window = new SomeWindow();
window.Activate();
```

## 🐛 已知问题

- ⚠️ 必须在 Visual Studio 2022 首次构建
- ⚠️ ContentDialog 必须设置 XamlRoot
- ⚠️ x:Bind 默认是 OneTime 模式
- ✅ 所有主要窗口已完成

## 📚 文档

- `README.md` - 项目概述
- `SETUP.md` - 安装配置
- `PROGRESS.md` - 详细进度
- `WinUI3-Controls-Guide.md` - 控件指南
- `COMPILE-FIX-REPORT.md` - 编译修复
- `STAGE-UPDATE.md` - 阶段更新

## 🔗 有用链接

- [WinUI 3 文档](https://docs.microsoft.com/windows/apps/winui/winui3/)
- [ReactiveUI](https://www.reactiveui.net/)
- [WinUIEx](https://github.com/dotMorten/WinUIEx)

---

**版本**: 0.55.0-alpha | **状态**: 快速开发中 🚀 | **更新**: 2025-01-16
