using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Media;

namespace Client.UI.ViewModel
{
	public class NotificationViewModel : BaseViewModel
	{
		ICollection<Inline> title = new Inline[] {};
		ICollection<Inline> message = new Inline[] {};
		Brush barBrush;

		public ICollection<Inline> Title
		{
			get { return title; }
			set
			{
				title = value;
				NotifyPropertyChanged("Title");
			}
		}

		public ICollection<Inline> Message
		{
			get { return message; }
			set
			{
				message = value;
				NotifyPropertyChanged("Message");
			}
		}

		public Brush BarBrush
		{
			get { return barBrush; }
			set
			{
				barBrush = value;
				NotifyPropertyChanged("BarBrush");
			}
		}

		public NotificationViewModel()
		{

		}
	}
}
