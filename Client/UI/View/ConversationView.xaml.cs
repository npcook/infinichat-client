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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Client.UI.View
{
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
	}
}
