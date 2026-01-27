# WinUI 3 控件使用说明

## TabControl vs TabView

### 问题
WinUI 3 中没有 `TabControl` 控件。

### 解决方案
使用 `TabView` 控件替代，这是WinUI 3的现代标签页控件。

#### 基本用法
```xaml
<TabView x:Name="tabMain" TabWidthMode="Equal">
    <TabView.TabItems>
        <TabViewItem Header="标签1">
            <Grid>
                <!-- 内容 -->
            </Grid>
        </TabViewItem>
        <TabViewItem Header="标签2">
            <Grid>
                <!-- 内容 -->
            </Grid>
        </TabViewItem>
    </TabView.TabItems>
</TabView>
```

#### TabView 的特性
- `TabWidthMode`: 控制标签宽度模式（Equal, SizeToContent, Compact）
- `IsClosable`: 是否允许关闭标签
- `CanReorderTabs`: 是否允许重新排序标签
- `CanDragTabs`: 是否允许拖拽标签

## DataTable/DataGrid 组件

### 已安装的包
`CommunityToolkit.Labs.WinUI.Controls.DataTable` (版本 0.1.260109-build.2471)

### 使用方法

1. **添加命名空间**
```xaml
xmlns:labs="using:CommunityToolkit.Labs.WinUI"
```

2. **使用DataTable**
```xaml
<labs:DataTable 
    x:Name="serverList"
    ItemsSource="{x:Bind ViewModel.Servers, Mode=OneWay}"
    AutoGenerateColumns="False"
    IsReadOnly="False"
    CanUserReorderColumns="True"
    CanUserSortColumns="True">
    
    <labs:DataTable.Columns>
        <labs:DataTableTextColumn 
            Header="服务器名称" 
            Binding="{Binding Remarks}"
            Width="200"/>
            
        <labs:DataTableTextColumn 
            Header="地址" 
            Binding="{Binding Address}"
            Width="150"/>
            
        <labs:DataTableTextColumn 
            Header="端口" 
            Binding="{Binding Port}"
            Width="80"/>
            
        <labs:DataTableTextColumn 
            Header="协议" 
            Binding="{Binding ConfigType}"
            Width="100"/>
            
        <labs:DataTableTextColumn 
            Header="延迟" 
            Binding="{Binding Delay}"
            Width="80"/>
    </labs:DataTable.Columns>
</labs:DataTable>
```

### 替代方案：ListView + ItemTemplate

如果DataTable不符合需求，也可以使用ListView：

```xaml
<ListView 
    x:Name="serverListView"
    ItemsSource="{x:Bind ViewModel.Servers, Mode=OneWay}"
    SelectionMode="Single">
    
    <ListView.ItemTemplate>
        <DataTemplate x:DataType="models:ProfileItem">
            <Grid Padding="8" ColumnSpacing="12">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="200"/>
                    <ColumnDefinition Width="150"/>
                    <ColumnDefinition Width="80"/>
                    <ColumnDefinition Width="100"/>
                    <ColumnDefinition Width="80"/>
                </Grid.ColumnDefinitions>
                
                <TextBlock Grid.Column="0" Text="{x:Bind Remarks}" VerticalAlignment="Center"/>
                <TextBlock Grid.Column="1" Text="{x:Bind Address}" VerticalAlignment="Center"/>
                <TextBlock Grid.Column="2" Text="{x:Bind Port}" VerticalAlignment="Center"/>
                <TextBlock Grid.Column="3" Text="{x:Bind ConfigType}" VerticalAlignment="Center"/>
                <TextBlock Grid.Column="4" Text="{x:Bind Delay}" VerticalAlignment="Center"/>
            </Grid>
        </DataTemplate>
    </ListView.ItemTemplate>
    
    <ListView.Header>
        <Grid Padding="8" Background="{ThemeResource CardBackgroundFillColorDefaultBrush}">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="200"/>
                <ColumnDefinition Width="150"/>
                <ColumnDefinition Width="80"/>
                <ColumnDefinition Width="100"/>
                <ColumnDefinition Width="80"/>
            </Grid.ColumnDefinitions>
            
            <TextBlock Grid.Column="0" Text="服务器名称" FontWeight="Bold"/>
            <TextBlock Grid.Column="1" Text="地址" FontWeight="Bold"/>
            <TextBlock Grid.Column="2" Text="端口" FontWeight="Bold"/>
            <TextBlock Grid.Column="3" Text="协议" FontWeight="Bold"/>
            <TextBlock Grid.Column="4" Text="延迟" FontWeight="Bold"/>
        </Grid>
    </ListView.Header>
</ListView>
```

## 其他常见WinUI 3控件差异

### 1. MessageBox
WinUI 3没有MessageBox，使用ContentDialog：

```csharp
var dialog = new ContentDialog
{
    Title = "确认",
    Content = "是否删除此服务器？",
    PrimaryButtonText = "确定",
    CloseButtonText = "取消",
    XamlRoot = this.Content.XamlRoot
};

var result = await dialog.ShowAsync();
if (result == ContentDialogResult.Primary)
{
    // 执行删除操作
}
```

### 2. ContextMenu
使用MenuFlyout：

```xaml
<Grid>
    <Grid.ContextFlyout>
        <MenuFlyout>
            <MenuFlyoutItem Text="编辑" Click="Edit_Click"/>
            <MenuFlyoutItem Text="删除" Click="Delete_Click"/>
            <MenuFlyoutSeparator/>
            <MenuFlyoutItem Text="复制" Click="Copy_Click"/>
        </MenuFlyout>
    </Grid.ContextFlyout>
</Grid>
```

### 3. 文件对话框
使用Windows.Storage.Pickers：

```csharp
var picker = new Windows.Storage.Pickers.FileOpenPicker();
picker.FileTypeFilter.Add(".json");

var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

var file = await picker.PickSingleFileAsync();
if (file != null)
{
    // 处理文件
}
```

## 数据绑定

### x:Bind vs Binding

WinUI 3推荐使用 `x:Bind` 而不是 `Binding`：

```xaml
<!-- 旧方式 (Binding) -->
<TextBlock Text="{Binding ServerName}"/>

<!-- 新方式 (x:Bind) - 编译时检查，性能更好 -->
<TextBlock Text="{x:Bind ViewModel.ServerName, Mode=OneWay}"/>
```

注意：`x:Bind` 默认是 OneTime 模式，需要明确指定 Mode=OneWay 或 TwoWay。

## 样式和主题

### 使用Fluent Design System

WinUI 3原生支持Fluent Design：

```xaml
<!-- Acrylic 背景 -->
<Grid Background="{ThemeResource AcrylicBackgroundFillColorDefaultBrush}"/>

<!-- Mica 背景（仅Window） -->
<Window.SystemBackdrop>
    <MicaBackdrop/>
</Window.SystemBackdrop>

<!-- 卡片样式 -->
<Border 
    Background="{ThemeResource CardBackgroundFillColorDefaultBrush}"
    BorderBrush="{ThemeResource CardStrokeColorDefaultBrush}"
    BorderThickness="1"
    CornerRadius="8">
    <!-- 内容 -->
</Border>
```

## 下一步实现建议

1. **服务器列表页面**
   - 使用DataTable或ListView显示服务器列表
   - 添加右键菜单（编辑、删除、测速等）
   - 实现拖拽排序

2. **数据绑定**
   - 使用 x:Bind 绑定 ViewModel.Servers
   - 实现选中项双向绑定
   - 添加搜索/过滤功能

3. **响应式UI**
   - 使用AdaptiveTrigger适配不同屏幕尺寸
   - 实现窗口大小调整时的列宽自适应

4. **命令绑定**
   - 使用ReactiveUI的ReactiveCommand
   - 绑定菜单项和按钮的Command属性
