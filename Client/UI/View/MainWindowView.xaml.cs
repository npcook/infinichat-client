using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Client.UI.View
{
	public class TabTemplateSelector : DataTemplateSelector
	{
		public DataTemplate NewTabTemplate
		{ get; set; }

		public DataTemplate ChatTabTemplate
		{ get; set; }

		public override DataTemplate SelectTemplate(object item, DependencyObject container)
		{
			if (item is ViewModel.NewTabViewModel)
				return NewTabTemplate;
			else if (item is ViewModel.ConversationViewModel)
				return ChatTabTemplate;
			return null;
		}
	}

	/// <summary>
	/// Interaction logic for MainWindowView.xaml
	/// </summary>
	public partial class MainWindowView : MetroWindow, IView
	{
		public static readonly RoutedUICommand ChatTabClose = new RoutedUICommand("Close Tab", "ChatTabClose", typeof(MainWindowView));

		public MainWindowView(ViewModel.MainWindowViewModel vm)
		{
			InitializeComponent();

			DataContext = vm;

			PreviewKeyDown += (sender, e) =>
				{
					if (e.Key == Key.F5)
						System.Diagnostics.Debugger.Break();
				};
		}

		protected override void OnClosed(EventArgs e)
		{
			(DataContext as ViewModel.MainWindowViewModel).Dispose();

			base.OnClosed(e);
		}

		private void CommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			(DataContext as ViewModel.MainWindowViewModel).CloseChatCommand.Execute(e.Parameter);
		}

		private void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = (DataContext as ViewModel.MainWindowViewModel).CloseChatCommand.CanExecute(e.Parameter);
		}

		object IView.ViewModel
		{
			get { return DataContext; }
		}

		void IView.Show()
		{
			Show();
		}

		bool? IView.ShowModal()
		{
			return ShowDialog();
		}

		void IView.Close()
		{
			Close();
		}
	}
}
