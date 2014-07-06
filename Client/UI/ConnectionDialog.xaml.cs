using Client.Protocol;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Client.UI
{
	/// <summary>
	/// Interaction logic for ConnectionDialog.xaml
	/// </summary>
	public partial class ConnectionDialog : MetroWindow
	{
		public ConnectionDialog(string title, string progressMessage)
		{
			InitializeComponent();

			Title = title;
			ProgressText.Text = progressMessage;
		}

		public void NotifyConnectionResult(bool success)
		{
			Dispatcher.BeginInvoke(new Action(() =>
			{
				DialogResult = success;
				Close();
			}));
		}

		private void CancelButtonClick(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}
	}
}
