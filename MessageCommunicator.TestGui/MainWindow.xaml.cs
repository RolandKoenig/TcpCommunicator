﻿using System;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using MessageCommunicator.TestGui.ViewServices;
using ReactiveUI;

namespace MessageCommunicator.TestGui
{
    public class MainWindow : OwnWindow<MainWindowViewModel>
    {
        public MainWindow()
        {
            AvaloniaXamlLoader.Load(this);
#if DEBUG
            this.AttachDevTools();
#endif

            // Register this window on App object
            App.CurrentApp.RegisterWindow(this);

            // Update title
            var versionInfoAttrib = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            var versionString = versionInfoAttrib?.InformationalVersion ?? "";
            this.Title = $"{this.Title} {versionString}";
            this.Find<TextBlock>("TxtTitle").Text = this.Title;

            // Register view services
            var ctrlDialogHost = this.Find<FluentWindowFrame>("CtrlWindowFrame").DialogHostControl;
            var helpRepo = new IntegratedDocRepository(Assembly.GetExecutingAssembly());

            this.ViewServices.Add(new ConnectionConfigControlService(ctrlDialogHost));
            this.ViewServices.Add(new MessageBoxControlService(ctrlDialogHost));
            this.ViewServices.Add(new ExportViewService(ctrlDialogHost));
            this.ViewServices.Add(new ImportViewService(ctrlDialogHost));
            this.ViewServices.Add(new SaveFileDialogService(this));
            this.ViewServices.Add(new OpenFileDialogService(this));
            this.ViewServices.Add(new AboutDialogService(ctrlDialogHost));
            this.ViewServices.Add(new HelpBrowserService(this, helpRepo));
            this.ViewServices.Add(new ViewResourceService(App.CurrentApp));

            // Load initial main view model
            this.ViewModel = new MainWindowViewModel();
            this.DataContext = this.ViewModel;

            // Configure error handling
            CommonErrorHandling.Current.MainWindow = this;
        }

        private void OnMnuExit_PointerPressed(object sender, PointerPressedEventArgs eArgs)
        {
            this.Close();
        }

        private void OnMnuThemeLight_PointerPressed(object sender, PointerPressedEventArgs eArgs)
        {
            MessageBus.Current.SendMessage(new MessageThemeChangeRequest(MessageCommunicatorTheme.Light));
        }

        private void OnMnuThemeDark_PointerPressed(object sender, PointerPressedEventArgs eArgs)
        {
            MessageBus.Current.SendMessage(new MessageThemeChangeRequest(MessageCommunicatorTheme.Dark));
        }
    }
}
