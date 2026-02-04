# v2rayWinUI 源代码生成器集成指南

## 概述

为 v2rayWinUI 项目创建专用的源代码生成器模块，自动生成：
1. 依赖注入（DI）注册代码
2. ObservableProperty 包装器
3. RelayCommand 声明

参考 SnapHutao.SourceGeneration 架构。

## 架构设计

### 项目结构

```
v2rayN/
├── v2rayWinUI.SourceGeneration/          (新建)
│   ├── src/
│   │   └── v2rayWinUI.SourceGeneration/
│   │       ├── Attributes/               DI 和属性特性定义
│   │       ├── DependencyInjection/      DI 生成器实现
│   │       ├── Properties/               属性生成器实现
│   │       ├── Commands/                 命令生成器实现
│   │       ├── Model/                    中间数据结构
│   │       ├── Extension/                辅助扩展
│   │       └── Primitive/                基础工具
│   └── v2rayWinUI.SourceGeneration.csproj
│
└── v2rayWinUI/
    └── v2rayWinUI.csproj                (引用生成器)
```

## 详细规划

### 阶段 1: 特性定义（Attributes）

创建文件：`v2rayWinUI.SourceGeneration/src/Attributes/DependencyInjectionAttribute.cs`

```csharp
namespace v2rayWinUI.Attributes;

/// <summary>
/// 标记需要自动 DI 注册的服务
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ServiceAttribute : Attribute
{
    /// <summary>
    /// 服务的生命周期：Transient, Scoped, Singleton
    /// </summary>
    public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Transient;
    
    /// <summary>
    /// 可选的接口类型，如果不指定则自动检测
    /// </summary>
    public Type? InterfaceType { get; set; }
}

/// <summary>
/// 服务生命周期
/// </summary>
public enum ServiceLifetime
{
    /// <summary>
    /// 每次请求创建新实例
    /// </summary>
    Transient,
    
    /// <summary>
    /// 作用域内共享实例
    /// </summary>
    Scoped,
    
    /// <summary>
    /// 应用生命周期内单一实例
    /// </summary>
    Singleton
}
```

创建文件：`v2rayWinUI.SourceGeneration/src/Attributes/ObservablePropertyAttribute.cs`

```csharp
namespace v2rayWinUI.Attributes;

/// <summary>
/// 标记需要自动生成 Observable 属性
/// 生成器将创建：
/// - 私有 _fieldName 字段
/// - 公开 PropertyName 属性（带 INotifyPropertyChanged）
/// - 自动的 PropertyChanged 事件触发
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class ObservablePropertyAttribute : Attribute
{
    /// <summary>
    /// 属性名称（如果不指定，自动从字段名推导）
    /// </summary>
    public string? PropertyName { get; set; }
    
    /// <summary>
    /// 是否生成 OnPropertyChanged 虚方法以支持自定义逻辑
    /// </summary>
    public bool GenerateOnChanged { get; set; } = true;
    
    /// <summary>
    /// 是否生成 OnChanging 虚方法
    /// </summary>
    public bool GenerateOnChanging { get; set; } = false;
}
```

### 阶段 2: DI 生成器（ServiceGenerator）

创建文件：`v2rayWinUI.SourceGeneration/src/DependencyInjection/ServiceGenerator.cs`

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace v2rayWinUI.SourceGeneration.DependencyInjection;

[Generator(LanguageNames.CSharp)]
public sealed class ServiceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 1. 收集所有标记 [Service] 的类
        var serviceProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "v2rayWinUI.Attributes.ServiceAttribute",
                predicate: IsValidServiceClass,
                transform: ExtractServiceInfo)
            .Where(x => x is not null)
            .Collect();
        
        // 2. 生成 ServiceCollectionExtension 静态类
        context.RegisterImplementationSourceOutput(
            serviceProvider,
            GenerateServiceCollectionExtension);
    }
    
    private static bool IsValidServiceClass(SyntaxNode node, CancellationToken ct)
    {
        return node is ClassDeclarationSyntax { IsRecord: false };
    }
    
    private static ServiceInfo? ExtractServiceInfo(
        GeneratorAttributeSyntaxContext ctx,
        CancellationToken ct)
    {
        // 提取类名、接口、生命周期等信息
        // 返回 ServiceInfo 记录供后续处理
        return null; // TODO: 实现提取逻辑
    }
    
    private static void GenerateServiceCollectionExtension(
        SourceProductionContext ctx,
        ImmutableArray<ServiceInfo> services)
    {
        if (services.IsEmpty) return;
        
        // 生成代码：
        // public static class ServiceCollectionExtensions
        // {
        //     public static IServiceCollection AddApplicationServices(
        //         this IServiceCollection services)
        //     {
        //         services.AddTransient<IService, ServiceImpl>();
        //         // ... 更多注册
        //         return services;
        //     }
        // }
    }
}
```

### 阶段 3: 属性生成器（PropertyGenerator）

创建文件：`v2rayWinUI.SourceGeneration/src/Properties/PropertyGenerator.cs`

```csharp
namespace v2rayWinUI.SourceGeneration.Properties;

[Generator(LanguageNames.CSharp)]
public sealed class PropertyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 类似 ServiceGenerator 的流程，但针对 [ObservableProperty] 字段
        // 为每个标记的字段生成公开属性及 PropertyChanged 通知
    }
}
```

## 集成步骤

### 步骤 1: 创建 SourceGeneration 项目

```bash
cd v2rayN
mkdir -p v2rayWinUI.SourceGeneration/src/v2rayWinUI.SourceGeneration
cd v2rayWinUI.SourceGeneration/src/v2rayWinUI.SourceGeneration

# 创建 .csproj 文件
cat > v2rayWinUI.SourceGeneration.csproj << 'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <IsRoslynComponent>true</IsRoslynComponent>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.8.0" 
                      PrivateAssets="All" />
    <PackageReference Include="Microsoft.CodeAnalysis.Analyzers" Version="3.3.4" 
                      PrivateAssets="All" />
  </ItemGroup>
</Project>
EOF
```

### 步骤 2: 注册到 v2rayWinUI.csproj

在 `v2rayWinUI.csproj` 中添加：

```xml
<ItemGroup>
  <ProjectReference Include="..\v2rayWinUI.SourceGeneration\src\v2rayWinUI.SourceGeneration\v2rayWinUI.SourceGeneration.csproj" 
                    OutputItemType="Analyzer" 
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```

### 步骤 3: 定义共享特性库

创建 `v2rayWinUI/Attributes/` 目录并放入特性定义，供 v2rayWinUI 使用：

```
v2rayWinUI/
├── Attributes/
│   ├── ServiceAttribute.cs
│   ├── ObservablePropertyAttribute.cs
│   └── RelayCommandAttribute.cs
└── ...
```

## 使用示例

### DI 生成器使用

```csharp
// v2rayWinUI/Services/ProfileService.cs
using v2rayWinUI.Attributes;

[Service(Lifetime = ServiceLifetime.Singleton, InterfaceType = typeof(IProfileService))]
public sealed class ProfileService : IProfileService
{
    public async Task<List<Profile>> GetProfilesAsync()
    {
        // 实现
    }
}

// 生成器自动生成：
// namespace v2rayWinUI.Generated;
// public static class ServiceCollectionExtensions
// {
//     public static IServiceCollection AddApplicationServices(this IServiceCollection services)
//     {
//         services.AddSingleton<IProfileService, ProfileService>();
//         return services;
//     }
// }
```

在 App.xaml.cs 中使用：

```csharp
public partial class App : Application
{
    public App()
    {
        var services = new ServiceCollection();
        services.AddApplicationServices(); // ← 自动生成的方法
        Services = services.BuildServiceProvider();
    }
}
```

### ObservableProperty 生成器使用

```csharp
public partial class ProfilesPageViewModel : ObservableObject
{
    [ObservableProperty]
    private string _serverFilter = string.Empty;
    
    [ObservableProperty]
    private int _selectedCount = 0;
    
    // 生成器自动生成：
    // public string ServerFilter
    // {
    //     get => _serverFilter;
    //     set => SetProperty(ref _serverFilter, value);
    // }
    //
    // public int SelectedCount
    // {
    //     get => _selectedCount;
    //     set => SetProperty(ref _selectedCount, value);
    // }
}
```

## 生成代码验证

生成的代码将在：
```
v2rayWinUI/obj/Debug/generated/
```

文件示例：
- `ServiceCollectionExtension.g.cs` - DI 注册
- `ProfilesPageViewModel.Properties.g.cs` - 属性声明

## 测试验证

### 编译测试
```bash
dotnet build v2rayN/v2rayWinUI/v2rayWinUI.csproj -c Debug
dotnet build v2rayN/v2rayWinUI/v2rayWinUI.csproj -c Release
```

### 生成代码查看
```bash
# Visual Studio: 解决方案浏览器 > obj 文件夹
# 或使用命令行
find v2rayWinUI/obj -name "*.g.cs"
```

## 性能考虑

- **增量编译**：使用 `ImmutableArray<T>` 和 `IEquatable<T>` 实现增量生成
- **缓存**：避免重复生成已处理的类
- **输出大小**：生成的代码体积预计不超过 50KB

## 迁移路线图

| 阶段 | 任务                           | 优先级 | 预计时间 |
| ---- | ------------------------------ | ------ | -------- |
| 3.1  | 创建 SourceGeneration 项目结构 | 高     | 2小时    |
| 3.2  | 实现 ServiceGenerator          | 高     | 4小时    |
| 3.3  | 实现 PropertyGenerator         | 中     | 3小时    |
| 3.4  | 迁移 v2rayWinUI 中的服务       | 中     | 2小时    |
| 3.5  | 迁移 ViewModel 属性            | 中     | 3小时    |
| 3.6  | 完整集成测试                   | 高     | 2小时    |

## 参考实现

- SnapHutao.SourceGeneration：`v2rayN/v2rayN/Snap.Hutao.SourceGeneration/`
- Microsoft.CodeAnalysis 文档
- Roslyn API 示例：https://github.com/dotnet/roslyn-analyzers
