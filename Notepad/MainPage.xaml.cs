using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.UI.ViewManagement;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Notepad
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private AppWindow RootAppWindow = null;

        private const string DataIdentifier = "MyTabItem";
        public MainPage()
        {
            this.InitializeComponent();

            Tabs.TabItemsChanged += Tabs_TabItemsChanged;
        }

        private async void Tabs_TabItemsChanged(TabView sender, IVectorChangedEventArgs args)
        {
            if (sender.TabItems.Count == 0)
            {
                if (RootAppWindow != null)
                {
                    await RootAppWindow.CloseAsync();
                }
                else
                {
                    Window.Current.Close();
                }
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            SetupWindow(null);
        }

        void SetupWindow(AppWindow window)
        {
            if (window == null)
            {
                for (int i = 0; i < 3; i++)
                {
                    Tabs.TabItems.Add(new TabViewItem()
                    {
                        IconSource = new Microsoft.UI.Xaml.Controls.SymbolIconSource() {Symbol = Symbol.Placeholder},
                        Header = $"Item {i}", Content = new NotepadControl() {DataContext = $"Page {i}"}
                    });
                }

                Tabs.SelectedIndex = 0;

                var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
                coreTitleBar.ExtendViewIntoTitleBar = true;
                coreTitleBar.LayoutMetricsChanged += CoreTitleBar_LayoutMetricsChanged;

                var titleBar = ApplicationView.GetForCurrentView().TitleBar;
                titleBar.ButtonBackgroundColor = Windows.UI.Colors.Transparent;
                titleBar.ButtonInactiveBackgroundColor = Windows.UI.Colors.Transparent;

                Window.Current.SetTitleBar(CustomDragRegion);
            }
            else
            {
                RootAppWindow = window;

                window.TitleBar.ExtendsContentIntoTitleBar = true;
                window.TitleBar.ButtonBackgroundColor = Windows.UI.Colors.Transparent;
                window.TitleBar.ButtonInactiveBackgroundColor = Windows.UI.Colors.Transparent;

                CustomDragRegion.MinWidth = 188;

                window.Frame.DragRegionVisuals.Add(CustomDragRegion);
            }
        }

        private void CoreTitleBar_LayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args)
        {
            if (FlowDirection == FlowDirection.LeftToRight)
            {
                CustomDragRegion.MinWidth = sender.SystemOverlayRightInset;
                ShellTitlebarInset.MinWidth = sender.SystemOverlayLeftInset;
            }
            else
            {
                CustomDragRegion.MinWidth = sender.SystemOverlayLeftInset;
                ShellTitlebarInset.MinWidth = sender.SystemOverlayRightInset;
            }

            CustomDragRegion.Height = ShellTitlebarInset.Height = sender.Height;
        }

        public void AddTabToTabs(TabViewItem tab)
        {
            Tabs.TabItems.Add(tab);
        }

        private void Tabs_OnAddTabButtonClick(TabView sender, object args)
        {
            sender.TabItems.Add(new TabViewItem()
            {
                IconSource = new Microsoft.UI.Xaml.Controls.SymbolIconSource() { Symbol = Symbol.Placeholder }, 
                Header = "New Item", 
                Content = new NotepadControl()
                {
                    DataContext = "New Item",

                }
            });
        }

        private void Tabs_OnTabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
        {
            sender.TabItems.Remove(args.Tab);
        }

        private async void Tabs_OnTabDroppedOutside(TabView sender, TabViewTabDroppedOutsideEventArgs args)
        {
            if (!ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                return;
            }

            AppWindow newWindow = await AppWindow.TryCreateAsync();

            var newPage = new MainPage();
            newPage.SetupWindow(newWindow);

            ElementCompositionPreview.SetAppWindowContent(newWindow, newPage);

            Tabs.TabItems.Remove(args.Tab);
            newPage.AddTabToTabs(args.Tab);

            await newWindow.TryShowAsync();
        }

        private void Tabs_OnTabStripDragOver(object sender, DragEventArgs e)
        {
            if (e.DataView.Properties.ContainsKey(DataIdentifier))
            {
                e.AcceptedOperation = DataPackageOperation.Move;
            }
        }

        private async void Tabs_OnTabStripDrop(object sender, DragEventArgs e)
        {
            if (e.DataView.Properties.TryGetValue(DataIdentifier, out object obj))
            {
                if (obj == null)
                {
                    return;
                }

                var destinationTabView = sender as TabView;
                var destinationItems = destinationTabView.TabItems;

                if (destinationItems != null)
                {
                    var index = -1;

                    for (int i = 0; i < destinationTabView.TabItems.Count; i++)
                    {
                        var item = destinationTabView.ContainerFromIndex(i) as TabViewItem;

                        if (e.GetPosition(item).X - item.ActualWidth < 0)
                        {
                            index = i;
                            break;
                        }
                    }

                    var destinationTabViewListView = ((obj as TabViewItem).Parent as TabViewListView);
                    destinationTabViewListView.Items.Remove(obj);

                    if (index < 0)
                    {
                        destinationItems.Add(obj);
                    }
                    else if (index < destinationTabView.TabItems.Count)
                    {
                        destinationItems.Insert(index, obj);
                    }

                    destinationTabView.SelectedItem = obj;
                }
            }
        }

        private void Tabs_OnTabDragStarting(TabView sender, TabViewTabDragStartingEventArgs args)
        {
            var firstItem = args.Tab;

            args.Data.Properties.Add(DataIdentifier, firstItem);

            args.Data.RequestedOperation = DataPackageOperation.Move;
        }
    }
}
