using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using MahApps.Metro.Controls;

namespace Client.UI
{
	/// <summary>
	/// Interaction logic for FontDialog.xaml
	/// </summary>
	public partial class FontDialog : MetroWindow
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public Protocol.FontOptions SelectedFont
		{
			get
			{
				var font = new Protocol.FontOptions();
				font.Family = SampleTextBlock.FontFamily.Source;
				font.Color = (SampleTextBlock.Foreground as SolidColorBrush).Color;
				if (SampleTextBlock.FontWeight == FontWeights.Bold)
					font.Style |= Protocol.FontStyle.Bold;
				if (SampleTextBlock.FontStyle == FontStyles.Italic)
					font.Style |= Protocol.FontStyle.Italic;
				if (SampleTextBlock.TextDecorations.Count > 0)
					font.Style |= Protocol.FontStyle.Underline;

				return font;
			}
		}

		public FontDialog(Protocol.FontOptions currentFont)
		{
			InitializeComponent();

			var families = Fonts.SystemFontFamilies.OrderBy((family) =>
			{
				return family.Source;
			});

			foreach (var family in families)
			{
				var item = new ListBoxItem()
				{
					Content = family.Source,
					VerticalContentAlignment = VerticalAlignment.Top,
					FontFamily = family,
					Padding = new Thickness(2),
					FontSize = 14,
				};
				FontFamilyListBox.Items.Add(item);
				if (family.Source == currentFont.Family)
					FontFamilyListBox.SelectedItem = item;
			}
			FontFamilyListBox.ScrollIntoView(FontFamilyListBox.SelectedItem);

			var colorMap = new Dictionary<string, Color>();
			colorMap.Add("Black", Colors.Black);
			colorMap.Add("Gray", Colors.Gray);
			colorMap.Add("Pink", Colors.Pink);
			colorMap.Add("Red", Colors.Red);
			colorMap.Add("Dark Red", Colors.DarkRed);
			colorMap.Add("Light Blue", Colors.LightBlue);
			colorMap.Add("Blue", Colors.Blue);
			colorMap.Add("Teal", Colors.Teal);
			colorMap.Add("Dark Blue", Colors.DarkBlue);
			colorMap.Add("Light Green", Colors.LightGreen);
			colorMap.Add("Green", Colors.Green);
			colorMap.Add("Dark Green", Colors.DarkGreen);

			foreach (var pair in colorMap)
			{
				var brush = new SolidColorBrush(pair.Value);
				brush.Freeze();

				ColorComboBox.Items.Add(new ComboBoxItem()
				{
					Content = pair.Key,
					Foreground = brush,
				});
			}
			ColorComboBox.SelectedIndex = 0;

			int[] sizes = { 6, 8, 9, 10, 11, 12, 14, 16, 18, 20, 22, 24, 26, 28, 30, 32, 36, 40 };

			foreach (int size in sizes)
				SizeComboBox.Items.Add(new ComboBoxItem() { Content = size.ToString() });
			SizeComboBox.SelectedIndex = 5;

			if (currentFont.Style.HasFlag(Protocol.FontStyle.Bold))
				BoldCheckBox.IsChecked = true;
			if (currentFont.Style.HasFlag(Protocol.FontStyle.Italic))
				ItalicCheckBox.IsChecked = true;
			if (currentFont.Style.HasFlag(Protocol.FontStyle.Underline))
				UnderlineCheckBox.IsChecked = true;
		}

		private void OKButtonClick(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
			Close();
		}

		private void CancelButtonClick(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}

		private void FontFamilyListBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count > 0)
			{
				if (!FontFamilyTextBox.IsFocused)
					FontFamilyTextBox.Text = (e.AddedItems[0] as ListBoxItem).Content as String;
			}
		}

		private void CheckBoxChecked(object sender, RoutedEventArgs e)
		{
			CheckBox source = e.Source as CheckBox;
			if (source == null)
				return;
			switch (source.Name)
			{
			case "BoldCheckBox":
				SampleTextBlock.FontWeight = FontWeights.Bold;
				break;

			case "ItalicCheckBox":
				SampleTextBlock.FontStyle = FontStyles.Italic;
				break;

			case "UnderlineCheckBox":
				SampleTextBlock.TextDecorations.Add(TextDecorations.Underline);
				break;
			}
		}

		private void CheckBoxUnchecked(object sender, RoutedEventArgs e)
		{
			CheckBox source = e.Source as CheckBox;
			if (source == null)
				return;
			switch (source.Name)
			{
			case "BoldCheckBox":
				SampleTextBlock.FontWeight = FontWeights.Normal;
				break;

			case "ItalicCheckBox":
				SampleTextBlock.FontStyle = FontStyles.Normal;
				break;

			case "UnderlineCheckBox":
				SampleTextBlock.TextDecorations.RemoveAt(0);
				break;
			}
		}

		private void FontFamilyTextBoxTextChanged(object sender, TextChangedEventArgs e)
		{
			if (String.IsNullOrEmpty(FontFamilyTextBox.Text))
				return;
			foreach (object o in FontFamilyListBox.Items)
			{
				ListBoxItem li = o as ListBoxItem;
				if ((li.Content as String).StartsWith(FontFamilyTextBox.Text, StringComparison.CurrentCultureIgnoreCase))
				{
					if (FontFamilyListBox.SelectedItem != li)
					{
						FontFamilyListBox.SelectedItem = li;
						FontFamilyListBox.ScrollIntoView(li);
					}
					break;
				}
			}
		}
	}
}
