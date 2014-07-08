using Client.UI.ViewModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.UI.View
{
	public class ContactTemplateSelector : DataTemplateSelector
	{
		public DataTemplate UserTemplate
		{ get; set; }

		public DataTemplate GroupTemplate
		{ get; set; }

		public override DataTemplate SelectTemplate(object item, DependencyObject container)
		{
			if (item is UserViewModel)
				return UserTemplate;
			else if (item is GroupViewModel)
				return GroupTemplate;
			else
				return null;
		}
	}

	/// <summary>
	/// Interaction logic for ConversationView.xaml
	/// </summary>
	public partial class ConversationView : UserControl
	{
		public ConversationView()
		{
			InitializeComponent();
		}

		protected override void OnVisualParentChanged(DependencyObject oldParent)
		{
			// This is a dirty hack. The WPF TabControl destroys views when unselected.
			// The ViewModel for this class has a FlowDocument member which cannot be 
			// used in FlowDocumentScrollViewers at the same time; we set the DataContext
			// to null in order to avoid this situation.
			if (oldParent != null)
				DataContext = null;

			base.OnVisualParentChanged(oldParent);
		}

		void OnHistoryTextInput(object sender, TextCompositionEventArgs e)
		{
			ChatInput.SelectedText = e.Text;
			ChatInput.Select(ChatInput.SelectionStart + ChatInput.SelectionLength, 0);
			ChatInput.Focus();
		}
	}
}
