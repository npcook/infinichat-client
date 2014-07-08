using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
	public interface IView
	{
		object ViewModel
		{ get; }

		void Show();
		bool? ShowModal();

		void Close();
	}

	public interface IViewController
	{
		IView CreateLoginView();
		IView CreateEmoteView();
		IView CreateFontView();
		IView CreateMainView();

		void Navigate(IView target);
	}
}
