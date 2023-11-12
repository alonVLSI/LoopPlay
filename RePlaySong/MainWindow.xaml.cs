using RePlaySong;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using DataFormats = System.Windows.DataFormats;
using DataObject = System.Windows.DataObject;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using ListBox = System.Windows.Controls.ListBox;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Window = System.Windows.Window;

namespace RePlaySong
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }

        private Point startPoint;
        private bool isDragging;
        private ListBoxItem draggedItem;

        private void MoveUp_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as MainViewModel;
            int selectedIndex = targetList.SelectedIndex;
            if (selectedIndex > 0)
            {
                string selectedItem = viewModel.TargetSongs[selectedIndex];
                viewModel.TargetSongs.RemoveAt(selectedIndex);
                viewModel.TargetSongs.Insert(selectedIndex - 1, selectedItem);
                targetList.SelectedIndex = selectedIndex - 1;
            }
        }

        private void MoveDown_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as MainViewModel;

            int selectedIndex = targetList.SelectedIndex;
            if (selectedIndex < viewModel.TargetSongs.Count - 1 && selectedIndex >= 0)
            {
                string selectedItem = viewModel.TargetSongs[selectedIndex];
                viewModel.TargetSongs.RemoveAt(selectedIndex);
                viewModel.TargetSongs.Insert(selectedIndex + 1, selectedItem);
                targetList.SelectedIndex = selectedIndex + 1;
            }
        }
        private void SourceListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            startPoint = e.GetPosition(null);
        }

        private void SourceListBox_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point mousePos = e.GetPosition(null);
                Vector diff = startPoint - mousePos;

                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    System.Windows.Controls.ListBox listBox = sender as ListBox;
                    if (listBox != null)
                    {
                        try
                        {
                            // Start the drag-and-drop oper ation
                            DragDrop.DoDragDrop(listBox, listBox.SelectedItem, DragDropEffects.Move);
                        }
                        catch(Exception)
                        {

                        }

                    }
                }
            }
        }

        private void TargetListView_Drop(object sender, DragEventArgs e)
        {
            // Handle the drop event in the target ListView
            if (e.Data.GetDataPresent(DataFormats.StringFormat))
            {
                string item = (string)e.Data.GetData(DataFormats.StringFormat);
                var viewModel = DataContext as MainViewModel;
                    if (!viewModel.TargetSongs.Contains(item))
                        viewModel.TargetSongs.Add(item);

            }
        }

        private void ListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ListBoxItem item = FindAncestor<ListBoxItem>((DependencyObject)e.OriginalSource);
            startPoint = e.GetPosition(null);

            if (item != null)
            {
                isDragging = true;
                draggedItem = item;
                Mouse.Capture(draggedItem);
            }
        }

        private void ListBox_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                Point position = e.GetPosition(null);
                Vector diff = startPoint - position;
                ListBoxItem targetItem = FindAncestor<ListBoxItem>((DependencyObject)e.OriginalSource);

                if (e.LeftButton == MouseButtonState.Pressed &&
                    (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance || Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
                {
                    DataObject data = new DataObject("MyListBoxItemFormat", draggedItem);
                    DragDrop.DoDragDrop(draggedItem, data, DragDropEffects.Move);
                    isDragging = false;
                    Mouse.Capture(null);
                }
            }
        }

        private void ListBox_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent("ListBoxItem") || sender == e.Source)
            {
                e.Effects = DragDropEffects.All;
            }
        }

        private void ListBox_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("ListBoxItem"))
            {
                ListBoxItem targetItem = FindAncestor<ListBoxItem>((DependencyObject)e.OriginalSource);

                if (targetItem != null)
                {
                    ListBox parentListBox = ItemsControl.ItemsControlFromItemContainer(targetItem) as ListBox;

                    int targetIndex = targetList.Items.IndexOf(targetItem);
                    int sourceIndex = targetList.Items.IndexOf(draggedItem.Content);

                    targetList.Items.RemoveAt(sourceIndex);

                    if (sourceIndex != targetIndex)
                    {
                        var viewmodel = DataContext as MainViewModel;
                        targetList.Items.Insert(targetIndex, draggedItem);
                        viewmodel.TargetSongs.RemoveAt(targetIndex);
                        viewmodel.TargetSongs.Insert(sourceIndex, draggedItem.Name);
                    }

                }
            }
        }

        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            do
            {
                if (current is T ancestor)
                {
                    return ancestor;
                }

                current = VisualTreeHelper.GetParent(current);
            } while (current != null);

            return null;
        }

        private void ListViewTarget_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var viewmodel = DataContext as MainViewModel;
            if(targetList.SelectedItems!=null && targetList.SelectedItems.Count>0)
            viewmodel.SelectedTargetSongs = targetList.SelectedItems[0].ToString();
        }

        private void ListViewSource_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var viewmodel = DataContext as MainViewModel;
            if (sourceList.SelectedItems != null && sourceList.SelectedItems.Count > 0)
                viewmodel.SelectedSongs = sourceList.SelectedItems[0].ToString();
        }


    }



}
