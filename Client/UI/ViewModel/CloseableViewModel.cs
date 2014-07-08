using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Client.UI.ViewModel
{
	class CloseableViewModel : BaseViewModel
	{
		public event EventHandler<EventArgs> CloseRequested;

		public ICommand CloseCommand
		{ get; private set; }

		public CloseableViewModel()
		{
			CloseCommand = new RelayCommand(_ => RequestClose());
		}

		public virtual void RequestClose()
		{
			CloseRequested.SafeInvoke(this, EventArgs.Empty);
		}
	}
}
