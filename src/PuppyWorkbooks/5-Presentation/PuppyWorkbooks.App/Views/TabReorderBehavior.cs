
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using AvaloniaEdit.Utils;
using ReactiveUI;

namespace PuppyWorkbooks.App.Views;

public static class TabReorderBehavior
{
    public static readonly AttachedProperty<bool> EnableReorderProperty =
        AvaloniaProperty.RegisterAttached<TabControl, bool>("EnableReorder", typeof(TabControl));

    static TabReorderBehavior()
    {
        EnableReorderProperty.Changed.Subscribe(
        args =>
        {
            if (args.Sender is TabControl tab)
            {
                if (args.NewValue.Value)
                    Attach(tab);
            }
        });
    }

    private static void Attach(TabControl tab)
    {
        tab.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
        tab.AddHandler(DragDrop.DropEvent, OnDrop);
    }

    private static void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is TabControl tab &&
            e.GetCurrentPoint(tab).Properties.IsLeftButtonPressed)
        {
            DragDrop.DoDragDrop(e, new DataObject(), DragDropEffects.Move);
        }
    }

    private static void OnDrop(object? sender, DragEventArgs e)
    {
        if (sender is TabControl tab &&
            e.Source is Control source &&
            source.DataContext is object item)
        {
            var items = tab.Items!;
            var oldIndex = items.IndexOf(item);

            var pos = e.GetPosition(tab);
            var newIndex = 0;

            foreach (var container in tab.GetVisualDescendants().OfType<TabItem>())
            {
                var bounds = container.Bounds;
                if (pos.X > bounds.X + bounds.Width / 2)
                    newIndex++;
            }

            if (newIndex != oldIndex)
            {
                items.RemoveAt(oldIndex);
                items.Insert(newIndex, item);
            }
        }
    }
}
