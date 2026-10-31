namespace SlipShot.UI;

internal static partial class Theme
{
    // Templates retain WPF control classes, commands and automation peers.
    private const string ControlStyles = """
    <ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
      <Style TargetType="ToolTip">
        <Setter Property="Background" Value="{DynamicResource Panel}"/><Setter Property="Foreground" Value="{DynamicResource Ink}"/>
        <Setter Property="Template"><Setter.Value><ControlTemplate TargetType="ToolTip"><Border Background="{TemplateBinding Background}" BorderBrush="{DynamicResource Border}" BorderThickness="1" CornerRadius="8" Padding="10,7" MaxWidth="360"><ContentPresenter><ContentPresenter.Resources><Style TargetType="TextBlock"><Setter Property="TextWrapping" Value="Wrap"/><Setter Property="Foreground" Value="{DynamicResource Ink}"/></Style></ContentPresenter.Resources></ContentPresenter></Border></ControlTemplate></Setter.Value></Setter>
      </Style>
      <Style TargetType="ContextMenu">
        <Setter Property="Background" Value="{DynamicResource Panel}"/><Setter Property="Foreground" Value="{DynamicResource Ink}"/>
        <Setter Property="Template"><Setter.Value><ControlTemplate TargetType="ContextMenu"><Border Background="{TemplateBinding Background}" BorderBrush="{DynamicResource Border}" BorderThickness="1" CornerRadius="8" Padding="4"><ItemsPresenter KeyboardNavigation.DirectionalNavigation="Cycle"/></Border></ControlTemplate></Setter.Value></Setter>
      </Style>
      <!-- SlipShot's export menu is flat; native file dialogs retain OS chrome. -->
      <Style TargetType="MenuItem">
        <Setter Property="Foreground" Value="{DynamicResource Ink}"/><Setter Property="Padding" Value="12,8"/>
        <Setter Property="Template"><Setter.Value><ControlTemplate TargetType="MenuItem"><Border x:Name="Chrome" Background="Transparent" BorderBrush="Transparent" BorderThickness="1" CornerRadius="6" Padding="{TemplateBinding Padding}"><Grid><Grid.ColumnDefinitions><ColumnDefinition/><ColumnDefinition Width="Auto"/></Grid.ColumnDefinitions><ContentPresenter ContentSource="Header" RecognizesAccessKey="True"/><TextBlock Grid.Column="1" Text="{TemplateBinding InputGestureText}" Foreground="{DynamicResource Muted}" Margin="24,0,0,0"/></Grid></Border><ControlTemplate.Triggers><Trigger Property="IsHighlighted" Value="True"><Setter TargetName="Chrome" Property="Background" Value="{DynamicResource Selected}"/><Setter TargetName="Chrome" Property="BorderBrush" Value="{DynamicResource Focus}"/></Trigger><Trigger Property="IsEnabled" Value="False"><Setter Property="Opacity" Value="0.45"/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter>
      </Style>
      <Style TargetType="ListBoxItem">
        <Setter Property="Foreground" Value="{DynamicResource Ink}"/><Setter Property="Padding" Value="8,6"/><Setter Property="Margin" Value="0,2"/><Setter Property="HorizontalContentAlignment" Value="Stretch"/>
        <Setter Property="Template"><Setter.Value><ControlTemplate TargetType="ListBoxItem"><Border x:Name="Chrome" Background="Transparent" BorderBrush="Transparent" BorderThickness="1" CornerRadius="6" Padding="{TemplateBinding Padding}"><ContentPresenter HorizontalAlignment="{TemplateBinding HorizontalContentAlignment}"/></Border><ControlTemplate.Triggers><Trigger Property="IsMouseOver" Value="True"><Setter TargetName="Chrome" Property="Background" Value="{DynamicResource Hover}"/></Trigger><Trigger Property="IsSelected" Value="True"><Setter TargetName="Chrome" Property="Background" Value="{DynamicResource Selected}"/><Setter TargetName="Chrome" Property="BorderBrush" Value="{DynamicResource Focus}"/></Trigger><Trigger Property="IsKeyboardFocusWithin" Value="True"><Setter TargetName="Chrome" Property="BorderBrush" Value="{DynamicResource Focus}"/><Setter TargetName="Chrome" Property="BorderThickness" Value="2"/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter>
      </Style>
      <Style x:Key="ScrollPage" TargetType="RepeatButton"><Setter Property="Focusable" Value="False"/><Setter Property="Template"><Setter.Value><ControlTemplate TargetType="RepeatButton"><Border Background="Transparent"/></ControlTemplate></Setter.Value></Setter></Style>
      <Style x:Key="ScrollThumb" TargetType="Thumb"><Setter Property="Template"><Setter.Value><ControlTemplate TargetType="Thumb"><Border x:Name="Grip" Background="{DynamicResource ControlBorder}" CornerRadius="4" Margin="3" MinHeight="6" MinWidth="6"/><ControlTemplate.Triggers><Trigger Property="IsMouseOver" Value="True"><Setter TargetName="Grip" Property="Background" Value="{DynamicResource Muted}"/></Trigger><Trigger Property="IsDragging" Value="True"><Setter TargetName="Grip" Property="Background" Value="{DynamicResource Focus}"/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter></Style>
      <Style TargetType="ScrollBar">
        <Setter Property="Width" Value="14"/><Setter Property="Background" Value="{DynamicResource Surface}"/>
        <Setter Property="Template"><Setter.Value><ControlTemplate TargetType="ScrollBar"><Grid Background="{TemplateBinding Background}"><Track x:Name="PART_Track" IsDirectionReversed="True" Orientation="{TemplateBinding Orientation}"><Track.DecreaseRepeatButton><RepeatButton x:Name="PageBack" Style="{StaticResource ScrollPage}" Command="ScrollBar.PageUpCommand"/></Track.DecreaseRepeatButton><Track.Thumb><Thumb Style="{StaticResource ScrollThumb}"/></Track.Thumb><Track.IncreaseRepeatButton><RepeatButton x:Name="PageForward" Style="{StaticResource ScrollPage}" Command="ScrollBar.PageDownCommand"/></Track.IncreaseRepeatButton></Track></Grid><ControlTemplate.Triggers><Trigger Property="Orientation" Value="Horizontal"><Setter TargetName="PART_Track" Property="IsDirectionReversed" Value="False"/><Setter TargetName="PageBack" Property="Command" Value="ScrollBar.PageLeftCommand"/><Setter TargetName="PageForward" Property="Command" Value="ScrollBar.PageRightCommand"/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter>
        <Style.Triggers><Trigger Property="Orientation" Value="Horizontal"><Setter Property="Width" Value="Auto"/><Setter Property="Height" Value="14"/></Trigger></Style.Triggers>
      </Style>
    </ResourceDictionary>
    """;
}
