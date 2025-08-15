#define DEBUG
using System;

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Shapes;
//using Hardcodet.Wpf.TaskbarNotification;
using Updater.Annotations;
using Updater.DataContractModels;
using Updater.HashZip.ZIPLib.Zip;
using Updater.Localization;
using Updater.Models;
using Updater.Properties;
using Updater.UtillsClasses;
using System.CodeDom.Compiler;
using System.Net;
using Updater.ViewModels;

namespace Updater
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void Grid_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
    }
}
