using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using PuppyWorkbooks.ViewModels;
using Syncfusion.Windows.Tools.Controls;
using DockState = Syncfusion.Windows.Tools.Controls.DockState;

namespace PuppyWorkbooks.App.Wpf.Views.Docking
{
    /// <summary>
    /// Interaction logic for DockingAdapter.xaml
    /// </summary>
    public partial class DockingAdapter : UserControl
    {
        public DockingAdapter()
        {
            InitializeComponent();
            PART_DockingManager.Loaded += PART_DockingManager_Loaded;
        }

        private void PART_DockingManager_Loaded(object sender, RoutedEventArgs e)
        {
            ((DocumentContainer)PART_DockingManager.DocContainer).AddTabDocumentAtLast = true;
        }

        public WorkSheetViewModel ActiveDocument
        {
            get { return (WorkSheetViewModel)GetValue(ActiveDocumentProperty); }
            set { SetValue(ActiveDocumentProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ActiveDocument.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ActiveDocumentProperty =
            DependencyProperty.Register(nameof(ActiveDocument), typeof(WorkSheetViewModel), typeof(DockingAdapter), new PropertyMetadata(null, new PropertyChangedCallback(OnActiveDocumentChanged)));

        public IList ItemsSource
        {
            get { return (IList)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ItemsSource.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(IList), typeof(DockingAdapter), new PropertyMetadata(null));

        private static void OnActiveDocumentChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            if (sender is DockingAdapter adapter)
            {
                foreach (FrameworkElement element in adapter.PART_DockingManager.Children)
                {
                    if (element is ContentControl)
                    {
                        if (element is ContentControl control && control.Content == args.NewValue)
                        {
                            adapter.PART_DockingManager.ActiveWindow = control;
                            break;
                        }
                    }
                }
            }
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property.Name == "ItemsSource")
            {
                if (e.OldValue != null)
                {
                    var oldcollection = e.OldValue as INotifyCollectionChanged;
                    oldcollection.CollectionChanged -= CollectionChanged;
                }

                if (e.NewValue != null)
                {
                    ((DocumentContainer)PART_DockingManager.DocContainer).AddTabDocumentAtLast = true;
                    var newcollection = e.NewValue as INotifyCollectionChanged;
                    var count = 0;
                    foreach (var item in ((IList)e.NewValue))
                    {
                        if (item is DockItem dockItem)
                        {
                            var control = dockItem.Content;//new ContentControl() { Content = dockItem.Content };
                            DockingManager.SetHeader(control, dockItem.Header);
                            if (dockItem.State == DockState.Document)
                            {
                                DockingManager.SetState(control, DockState.Document);
                            }
                            else
                            {
                                if (count != 0)
                                {
                                    DockingManager.SetTargetNameInDockedMode(control, "item" + (count-1).ToString());
                                    DockingManager.SetSideInDockedMode(control, DockSide.Bottom);
                                }
                                DockingManager.SetDesiredWidthInDockedMode(control, 220);
                                control.Name = "item" + (count++).ToString();
                            }
                            PART_DockingManager.Children.Add(control);
                        }
                    }
                    newcollection.CollectionChanged += CollectionChanged;
                }
            }
            base.OnPropertyChanged(e);
        }

        private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    var control = (from ContentControl element in PART_DockingManager.Children
                                              where element.Content == item
                                              select element).FirstOrDefault();
                    if (control != null)
                    {
                        PART_DockingManager.Children.Remove(control);
                    }
                }
            }

            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    if (item is not DockItem dockItem) continue;
                    var control = dockItem.Content; //new ContentControl() { Content = dockItem.Content };
                    DockingManager.SetHeader(control, dockItem.Header);
                    DockingManager.SetState(control, dockItem.State);
                    PART_DockingManager.Children.Add(control);
                }
            }
        }

        public DataTemplate FindDataTemplate(Type type, FrameworkElement element)
        {
            if (element.TryFindResource(type) is DataTemplate dataTemplate)
            {
                return dataTemplate;
            }

            if (type.BaseType != null && type.BaseType != typeof(object))
            {
                dataTemplate = FindDataTemplate(type.BaseType, element);
                if (dataTemplate != null)
                {
                    return dataTemplate;
                }
            }

            foreach (var interfaceType in type.GetInterfaces())
            {
                dataTemplate = FindDataTemplate(interfaceType, element);
                if (dataTemplate != null)
                {
                    return dataTemplate;
                }
            }

            return null;
        }

        private void PART_DockingManager_ActiveWindowChanged_1(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is WorkSheetView content)
            {
                var dataContext = content.ViewModel;
                if (dataContext is WorkSheetViewModel workSheetViewModel)
                {
                    ActiveDocument = workSheetViewModel;
                }
            }
        }
    }
}
