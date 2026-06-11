using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Spur.ViewModels;

namespace Spur.Views;

/// <summary>DataTemplateSelector that routes SectionLabel vs SearchResult.</summary>
public sealed class ResultTemplateSelector : DataTemplateSelector
{
    public DataTemplate? SectionTemplate { get; set; }
    public DataTemplate? ResultTemplate  { get; set; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        => item is SectionLabel ? SectionTemplate : ResultTemplate;
}

public partial class UnifiedResultsView : UserControl
{
    public UnifiedResultsView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        ResultsBox.ItemContainerGenerator.StatusChanged += OnItemContainerStatusChanged;
    }

    private void OnItemContainerStatusChanged(object? sender, EventArgs e)
    {
        if (ResultsBox.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
        {
            if (DataContext is not MainViewModel vm) return;
            bool animate = vm.Config.AnimationEnabled && !SpurMotion.IsReduceMotion;

            for (int i = 0; i < ResultsBox.Items.Count; i++)
            {
                if (ResultsBox.ItemContainerGenerator.ContainerFromIndex(i) is FrameworkElement container)
                {
                    // Clean up any old animations
                    container.BeginAnimation(UIElement.OpacityProperty, null);
                    if (container.RenderTransform is TranslateTransform oldTt)
                        oldTt.BeginAnimation(TranslateTransform.YProperty, null);

                    if (animate)
                    {
                        var tt = container.RenderTransform as TranslateTransform;
                        if (tt is null)
                        {
                            tt = new TranslateTransform(0, 4);
                            container.RenderTransform = tt;
                        }
                        else
                        {
                            tt.Y = 4;
                        }
                        container.Opacity = 0;

                        var ease = SpurMotion.EaseOut();
                        var delay = TimeSpan.FromMilliseconds(i * 16);

                        var yAnim = new DoubleAnimation(4, 0, TimeSpan.FromMilliseconds(120)) { EasingFunction = ease, BeginTime = delay };
                        var opAnim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(120)) { EasingFunction = ease, BeginTime = delay };

                        tt.BeginAnimation(TranslateTransform.YProperty, yAnim);
                        container.BeginAnimation(UIElement.OpacityProperty, opAnim);
                    }
                    else
                    {
                        container.RenderTransform = null;
                        container.Opacity = 1;
                    }
                }
            }
        }
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is MainViewModel old)
            old.PropertyChanged -= OnVmChanged;
        if (e.NewValue is MainViewModel vm)
            vm.PropertyChanged += OnVmChanged;
    }

    private void OnVmChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.SelectedIndex))
            ScrollToSelected();
    }

    private void ScrollToSelected()
    {
        if (DataContext is not MainViewModel vm) return;
        int idx = vm.SelectedIndex;
        if (idx < 0 || idx >= vm.Results.Count) return;

        var container = ResultsBox.ItemContainerGenerator.ContainerFromIndex(idx) as FrameworkElement;
        container?.BringIntoView();
    }

    private void OnListMouseClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject src)
        {
            var item = ItemsControl.ContainerFromElement(ResultsBox, src) as ListBoxItem;
            if (item?.DataContext is SearchResult result)
            {
                if (DataContext is MainViewModel vm)
                {
                    int idx = vm.Results.IndexOf(result);
                    if (idx >= 0)
                        vm.SelectedIndex = idx;

                    vm.OpenSelectedCommand.Execute(null);
                    e.Handled = true;
                }
            }
        }
    }

}
