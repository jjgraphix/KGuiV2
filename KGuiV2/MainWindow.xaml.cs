﻿using System.Windows;
using System.Windows.Controls;

namespace KGuiV2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Creates a new <see cref="MainWindow"/> instance.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            TestSizeBox.Focus();
        }
    }
}
