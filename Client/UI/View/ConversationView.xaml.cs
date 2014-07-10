using Client.UI.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

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

	public class UserToCommaSeparatedConverter : IValueConverter
	{
		public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			var users = (IEnumerable<UserViewModel>) value;
			var names = (from user in users select user.DisplayName).ToArray();
			if (names.Length == 0)
				return "";
			else if (names.Length == 1)
				return names[0];
			else if (names.Length == 2)
				return names[0] + " and " + names[1];
			{
				var builder = new StringBuilder();
				for (int i = 0; i < names.Length - 1; ++i)
					builder.Append(names[i]).Append(", ");
				builder.Append("and ").Append(names[names.Length - 1]);
				return builder.ToString();
			}
		}

		public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
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

			DataContextChanged += OnDataContextChanged;
		}

		void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			var oldValue = e.OldValue as ConversationViewModel;
			var newValue = e.NewValue as ConversationViewModel;

			if (oldValue != null)
			{
				oldValue.ChatHistory.CollectionChanged -= OnChatHistoryChanged;
			}

			if (newValue != null)
			{
				newValue.ChatHistory.CollectionChanged += OnChatHistoryChanged;
				HistoryDocument.Blocks.AddRange(newValue.ChatHistory);
			}
		}

		void OnChatHistoryChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			var vm = DataContext as ConversationViewModel;

			if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
			{
				bool isAtEnd = (e.NewStartingIndex + e.NewItems.Count == vm.ChatHistory.Count);
				if (isAtEnd)
				{
					HistoryDocument.Blocks.AddRange(e.NewItems);

					if (ChatHistory.ScrollViewer.VerticalOffset == ChatHistory.ScrollViewer.ScrollableHeight)
						ChatHistory.ScrollViewer.ScrollToEnd();
				}
			}
		}

		void OnHistoryTextInput(object sender, TextCompositionEventArgs e)
		{
			ChatInput.SelectedText = e.Text;
			ChatInput.Select(ChatInput.SelectionStart + ChatInput.SelectionLength, 0);
			ChatInput.Focus();
		}
	}

	// Adapted from http://stackoverflow.com/questions/561029/scroll-a-wpf-flowdocumentscrollviewer-from-code
	public class HistoryDocumentViewer : FlowDocumentScrollViewer
	{
		ScrollViewer scrollViewer;
		public ScrollViewer ScrollViewer
		{
			get
			{
				if (scrollViewer == null)
				{
					DependencyObject child = this;

					do
					{
						if (VisualTreeHelper.GetChildrenCount(child) > 0)
							child = VisualTreeHelper.GetChild(child, 0);
						else
							return null;
					} while (!(child is ScrollViewer));

					scrollViewer = child as ScrollViewer;
				}
				return scrollViewer;
			}
		}
	}
}
