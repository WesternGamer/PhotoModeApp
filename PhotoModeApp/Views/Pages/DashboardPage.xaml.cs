﻿using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Windows.Foundation.Collections;
using Wpf.Ui.Common.Interfaces;
using Wpf.Ui.Controls.Interfaces;
using Wpf.Ui.Mvvm.Contracts;

namespace PhotoModeApp.Views.Pages
{
    /// <summary>
    /// Interaction logic for DashboardPage.xaml
    /// </summary>
    public partial class DashboardPage : INavigableView<ViewModels.DashboardViewModel>
    {
        private readonly IDialogControl _dialogControl;

        private enum PROCESSING_STATUS
        {
            Done = 999
        };

        public ViewModels.DashboardViewModel ViewModel
        {
            get;
        }

        private int totalNumberOfFiles;

        public DashboardPage(ViewModels.DashboardViewModel viewModel, IDialogService dialogService)
        {
            ViewModel = viewModel;

            InitializeComponent();
            PathAction.Content = Helpers.Config.GetPath();


            ToastNotificationManagerCompat.OnActivated += toastArgs =>
            {
                ToastArguments args = ToastArguments.Parse(toastArgs.Argument);
                ValueSet userInput = toastArgs.UserInput;

                Application.Current.Dispatcher.Invoke(delegate
                {
                    Process.Start("explorer.exe", Helpers.Config.GetPath());
                });
            };
        }

        public void Setup()
        {
            if (!Helpers.Config.GetPath().Equals(string.Empty))
            {
                PathAction.IsEnabled = true;
                PathAction.Content = Helpers.Config.GetPath();
            }
        }

        private void PathAction_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
            if (dialog.ShowDialog().GetValueOrDefault())
            {
                Helpers.Config.WritePath(dialog.SelectedPath);
                PathAction.Content = Helpers.Config.GetPath();
            }
        }

        private async Task<int> ProcessPicturesAsync(IProgress<int> progress)
        {
            totalNumberOfFiles = Helpers.Win32Files.GetFileCount(Helpers.Config.GetPath(), true);
            int processCount = await Task.Run<int>(() =>
            {
                int tempCount = 0;

                Process converterProcess = new Process();

                converterProcess.StartInfo.RedirectStandardOutput = true;
                converterProcess.StartInfo.UseShellExecute = false;
                converterProcess.StartInfo.CreateNoWindow = true;

                converterProcess.StartInfo.FileName = "ragephoto-extract.exe";

                foreach (var image in Directory.GetFiles(Helpers.Config.GetPath(), "*.*", SearchOption.AllDirectories))
                {
                    converterProcess.StartInfo.Arguments = image + " " + image + ".jpg";
                    converterProcess.Start();
                    converterProcess.WaitForExitAsync();

                    tempCount++;
                    if (progress != null) progress.Report((tempCount));

                }

                return tempCount;
            });

            if (processCount == totalNumberOfFiles) return (int)PROCESSING_STATUS.Done;
            return processCount;
          
        }

        private async void ConvertButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ProgressText.Visibility = Visibility.Visible;

            int proc = await ProcessPicturesAsync(new Progress<int>(percent => UpdateProgressUI(percent)));

            if (proc == (int)PROCESSING_STATUS.Done)
            {
                new ToastContentBuilder()
                    .AddArgument("action", "viewConversation")
                    .AddArgument("conversationId", 9813)
                    .AddText("Photos successfully converted! 🎉")
                    .AddButton(new ToastButton()
                        .SetContent("Show")
                        .AddArgument("action", "reply")
                        .SetBackgroundActivation())
                    .Show();

                ConvertButton.Content = "Convert";
            }
        }

        private void UpdateProgressUI(int value)
        {
            ProgressText.Text = String.Format("Procesing: {0}/{1}", value, totalNumberOfFiles);
        }
    }
}