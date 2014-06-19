using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Client.UI
{
	/// <summary>
	/// Interaction logic for MenuButton.xaml
	/// </summary>
	public partial class MenuButton : ToggleButton
	{
		public MenuButton()
		{
			InitializeComponent();
		}

		private void DropDownChecked(object sender, RoutedEventArgs e)
		{
			if (ContextMenu != null)
			{
				ContextMenu.Closed += DropDownMenuClosed;
				ContextMenu.PlacementTarget = this;
				ContextMenu.Placement = PlacementMode.Bottom;
				ContextMenu.IsOpen = true;
			}
			else
				IsChecked = false;
		}

		private void DropDownMenuClosed(object sender, RoutedEventArgs e)
		{
			IsChecked = false;
		}

		private void DropDownUnchecked(object sender, RoutedEventArgs e)
		{
			if (ContextMenu != null)
				ContextMenu.IsOpen = false;
		}
	}
}
