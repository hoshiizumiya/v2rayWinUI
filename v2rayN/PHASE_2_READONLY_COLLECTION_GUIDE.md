# v2rayWinUI 数据绑定重构指南

## 问题描述

**当前问题：**
- UI 在 View 中直接调用 `ProfileItems.Clear()` + `ProfileItems.AddRange()` 
- WinUI 3 的线程封送（Marshalling）处理不当导致 COMException
- 首次加载时数据不显示，需手动刷新

**根本原因：**
1. ObservableCollection 不是为跨线程批量操作设计的
2. Clear() 导致 UI 重置，然后 AddRange() 的每一项都触发单个更新事件
3. 线程上下文切换（Reactive Observable→MainThreadScheduler）导致封送问题

## 解决方案

### 架构模式：ReadOnly-WriteOnly 分离

```
ViewModel 层
    ↓
[Internal] ObservableCollection<T> _items
    ↓
[Public] IReadOnlyList<T> Items (via wrapper)
    ↓
View 层 (只读访问)
```

### 步骤 1: ViewModel 实现

```csharp
public class ProfilesPageViewModel : ObservableObject
{
    // 内部可写集合
    private readonly ObservableCollection<ProfileItem> _profileItems 
        = new ObservableCollection<ProfileItem>();
    
    // 对外暴露只读包装
    private ReadOnlyObservableCollectionWrapper<ProfileItem> _profileItemsWrapper;
    
    public IReadOnlyList<ProfileItem> ProfileItems 
    {
        get => _profileItemsWrapper ??= new ReadOnlyObservableCollectionWrapper<ProfileItem>(_profileItems);
    }
    
    // 业务逻辑使用内部集合（ViewModel 层可以修改）
    public async Task RefreshProfilesAsync()
    {
        try
        {
            var profiles = await LoadProfilesAsync();
            
            // 使用安全的替换操作，避免 COMException
            _profileItems.SafeReplace(profiles);
        }
        catch (Exception ex)
        {
            HandleException(ex);
        }
    }
}
```

### 步骤 2: View 绑定

```xaml
<!-- 直接绑定到只读属性 -->
<ListView ItemsSource="{Binding ProfileItems}">
    <!-- 不能修改集合，数据清晰来自 ViewModel -->
</ListView>
```

### 步骤 3: 代码后置处理

```csharp
public sealed partial class ProfilesView : Page
{
    // ❌ 不要这样做：
    // ViewModel.ProfileItems.Clear();
    // ViewModel.ProfileItems.AddRange(newItems);
    
    // ✅ 应该这样做：
    private void RefreshUI()
    {
        // 由 ViewModel 的观察者处理
        ViewModel?.RefreshProfilesCommand.Execute().Subscribe();
    }
}
```

## 迁移计划

### 阶段 1: 基础基础设施
- ✅ 创建 `ReadOnlyObservableCollectionWrapper<T>` 类
- ✅ 创建 `SafeReplace()` 扩展方法
- □ 在 ServiceLib 中应用

### 阶段 2: ServiceLib ViewModel 迁移
目标文件：
- `ServiceLib/ViewModels/ProfilesViewModel.cs`
  ```csharp
  // 当前：public ObservableCollection<ProfileItemModel> ProfileItems { get; set; }
  // 改为：public IReadOnlyList<ProfileItemModel> ProfileItems => _profileItemsWrapper;
  ```

- `ServiceLib/ViewModels/AddServerViewModel.cs`
  - 检查所有 ObservableCollection 成员
  
- `ServiceLib/ViewModels/*.cs`
  - 所有 MVVM ViewModel

### 阶段 3: v2rayWinUI View 层审计
- 去除所有直接的 `.Clear()` + `.AddRange()` 调用
- 改为通过 ViewModel 命令刷新
- 示例：
  ```csharp
  // ❌ 旧方式
  private void LoadData()
  {
      ViewModel.Items.Clear();
      ViewModel.Items.AddRange(newItems);
  }
  
  // ✅ 新方式
  private void LoadData()
  {
      ViewModel?.LoadItemsCommand.Execute().Subscribe();
  }
  ```

## 初始化数据问题修复

**问题：** 首次进入页面不显示数据

**原因：** 
1. OnNavigatedTo 未被调用
2. 数据加载时 ViewModel 未完全初始化
3. 绑定建立在数据加载之前

**修复方案：**

在 ProfilesView.xaml.cs 的 Loaded 事件中：

```csharp
private void ProfilesView_Loaded(object sender, RoutedEventArgs e)
{
    // 强制初始化，确保数据已加载
    if (ViewModel?.ProfileItems.Count == 0)
    {
        // 主动触发加载
        ViewModel?.RefreshProfilesCommand.Execute()
            .Subscribe(
                onNext: _ => { },
                onError: ex => ShowError(ex)
            );
    }
}
```

## 数据绑定最佳实践

### ✅ 推荐做法

```csharp
// ViewModel
public class ItemListViewModel : ObservableObject
{
    private readonly ObservableCollection<Item> _items = new();
    
    public IReadOnlyList<Item> Items 
    {
        get => new ReadOnlyCollection<Item>(_items);
    }
    
    public IAsyncRelayCommand RefreshCommand { get; }
    
    public ItemListViewModel()
    {
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
    }
    
    private async Task RefreshAsync()
    {
        var newItems = await FetchItemsAsync();
        _items.SafeReplace(newItems);
    }
}

// View
<ListView ItemsSource="{Binding Items}" />

// Code-behind
private void OnRefreshRequested()
{
    ViewModel?.RefreshCommand.Execute();
}
```

### ❌ 避免做法

```csharp
// ❌ View 直接修改集合
ViewModel.Items.Clear();
foreach (var item in newItems)
{
    ViewModel.Items.Add(item);
}

// ❌ 公开可写的 ObservableCollection
public ObservableCollection<Item> Items { get; set; }

// ❌ 在 PropertyChanged 时替换整个集合
Items = new ObservableCollection<Item>(newItems);
```

## 测试验证

### 单元测试
```csharp
[TestMethod]
public void ReadOnlyWrapper_AllowsEnumeration()
{
    var items = new List<int> { 1, 2, 3 };
    var wrapper = new ReadOnlyObservableCollectionWrapper<int>(items);
    
    Assert.AreEqual(3, wrapper.Count);
    CollectionAssert.AreEqual(items, wrapper.ToList());
}

[TestMethod]
[ExpectedException(typeof(NotSupportedException))]
public void ReadOnlyWrapper_ThrowsOnClear()
{
    var wrapper = new ReadOnlyObservableCollectionWrapper<int>();
    wrapper.Clear(); // 应该抛出异常
}
```

### 集成测试
```csharp
[TestMethod]
public async Task ProfilesView_LoadsDataOnce_WithoutManualRefresh()
{
    var view = new ProfilesView();
    var viewModel = CreateTestViewModel();
    view.DataContext = viewModel;
    
    // 加载视图
    view.OnLoaded(null, null);
    
    // 等待异步操作
    await Task.Delay(500);
    
    // 验证数据已加载
    Assert.IsTrue(view.lstProfiles.Items.Count > 0);
}
```

## 相关文件清单

| 文件                                                                                                                 | 状态     | 备注             |
| -------------------------------------------------------------------------------------------------------------------- | -------- | ---------------- |
| [v2rayWinUI/Common/ReadOnlyObservableCollectionWrapper.cs](v2rayWinUI/Common/ReadOnlyObservableCollectionWrapper.cs) | ✅ 完成   | 包装类和扩展方法 |
| ServiceLib/ViewModels/ProfilesViewModel.cs                                                                           | □ 待迁移 | 主要 ViewModel   |
| ServiceLib/ViewModels/*.cs                                                                                           | □ 待审计 | 其他 ViewModel   |
| v2rayWinUI/Views/ProfilesView.xaml.cs                                                                                | □ 待审计 | 检查集合直接操作 |
| v2rayWinUI/Views/*.xaml.cs                                                                                           | □ 待审计 | 所有 CodeBehind  |

## 参考资源

- SnapHutao 实现参考：Snap.Hutao/ViewModel 目录
- WinUI 3 最佳实践：Microsoft 官方文档
- ReactiveUI 集合处理：ReactiveUI 文档
