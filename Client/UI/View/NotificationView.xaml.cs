using MahApps.Metro.Controls;

namespace Client.UI.View
{
	/// <summary>
	/// Interaction logic for NotificationView.xaml
	/// </summary>
	public partial class NotificationView : MetroWindow, IView
	{
		public NotificationView(ViewModel.NotificationViewModel vm)
		{
			InitializeComponent();

			DataContextChanged += OnDataContextChanged;

			DataContext = vm;
		}

		void OnDataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
		{
			var vm = e.NewValue as UI.ViewModel.NotificationViewModel;

			TitleText.Inlines.Clear();
			TitleText.Inlines.AddRange(vm.Title);

			MessageText.Inlines.Clear();
			MessageText.Inlines.AddRange(vm.Message);
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
