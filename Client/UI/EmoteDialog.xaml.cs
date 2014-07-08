using MahApps.Metro.Controls;

namespace Client.UI
{
	/// <summary>
	/// Interaction logic for EmoteDialog.xaml
	/// </summary>
	public partial class EmoteDialog : MetroWindow, IView
	{
		public EmoteDialog()
		{
			InitializeComponent();

			foreach (var emote in App.Current.Emoticons)
				EmoteListBox.Items.Add(emote);
		}

		void OnQuitClick(object sender, System.Windows.RoutedEventArgs e)
		{
			Close();
		}

		object IView.ViewModel
		{
			get { return this; }
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
