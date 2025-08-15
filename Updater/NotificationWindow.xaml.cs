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

		//internal Button CancelQuest;

		//internal Button OkQuest;

		//internal Button OKInfo;

		//private bool _contentLoaded;
		
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

		/*
		[DebuggerNonUserCode]
		[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
		public void InitializeComponent()
		{
			if (!_contentLoaded)
			{
				_contentLoaded = true;
				Uri resourceLocator = new Uri("/VLGame;component/notificationwindow.xaml", UriKind.Relative);
				Application.LoadComponent(this, resourceLocator);
			}
		}

		[DebuggerNonUserCode]
		[GeneratedCode("PresentationBuildTasks", "4.0.0.0")]
		[EditorBrowsable(EditorBrowsableState.Never)]
		void IComponentConnector.Connect(int connectionId, object target)
		{
			switch (connectionId)
			{
			case 1:
				((NotificationWindow)target).MouseDown += Window_MouseDown;
				break;
			case 2:
				CancelQuest = (Button)target;
				CancelQuest.Click += Button_Click_1;
				break;
			case 3:
				OkQuest = (Button)target;
				OkQuest.Click += Button_Click;
				break;
			case 4:
				OKInfo = (Button)target;
				OKInfo.Click += Button_Click_1;
				break;
			default:
				_contentLoaded = true;
				break;
			}
		}*/
	}
}
