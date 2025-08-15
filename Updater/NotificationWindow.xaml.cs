using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;

namespace Updater
{
	public partial class NotificationWindow : Window, IComponentConnector
	{
		
		public string Header
		{
			get;
			set;
		}

		public string Text
		{
			get;
			set;
		}
		
		public NotificationWindow(string header, string text, bool quest = false)
		{
			Header = header;
			Text = text;
			InitializeComponent();
			base.DataContext = this;
			if (quest)
			{
				OKInfo.Visibility = Visibility.Hidden;
				OkQuest.Visibility = Visibility.Visible;
				CancelQuest.Visibility = Visibility.Visible;
			}
		}
		

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			base.DialogResult = true;
		}

		private void Window_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed)
			{
				DragMove();
			}
		}

		private void Button_Click_1(object sender, RoutedEventArgs e)
		{
			base.DialogResult = false;
		}

	}
}
