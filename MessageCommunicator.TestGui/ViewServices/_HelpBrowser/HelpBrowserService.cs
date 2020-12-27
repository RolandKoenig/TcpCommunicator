﻿using System.Threading.Tasks;
using Avalonia.Controls;

namespace MessageCommunicator.TestGui.ViewServices
{
    public class HelpBrowserService : ViewServiceBase, IHelpViewerService
    {
        private WindowBase _parentWindow;
        private IntegratedDocRepository _docRepository;

        private HelpBrowserWindow? _openedBrowser;
        private HelpBrowserViewModel? _openedViewModel;
        private Task? _openedBrowserTask;
        
        public HelpBrowserService(WindowBase parentWindow, IntegratedDocRepository docRepository)
        {
            _parentWindow = parentWindow;
            _docRepository = docRepository;
        }
        
        /// <inheritdoc />
        public Task ShowHelpPageAsync(string pageTitle)
        {
            var taskComplSource = new TaskCompletionSource<object?>();

            // Ensure that help browser is shown
            if (_openedBrowser != null)
            {
                // Bring window to front
                _openedBrowser.Activate();
            }
            else
            {
                var viewModel = new HelpBrowserViewModel(_docRepository);
                
                var helpBrowserWindow = new HelpBrowserWindow();
                helpBrowserWindow.Owner = _parentWindow;
                helpBrowserWindow.DataContext = viewModel;
                helpBrowserWindow.Closed += (_, _) =>
                {
                    taskComplSource.TrySetResult(null);
                    _openedBrowser = null;
                    _openedViewModel = null;
                    _openedBrowserTask = null;
                };
                helpBrowserWindow.Show();
                
                _openedBrowser = helpBrowserWindow;
                _openedViewModel = viewModel;
                _openedBrowserTask = taskComplSource.Task;
            }
            
            // Navigate to help page
            _openedViewModel!.NavigateTo(pageTitle);

            return _openedBrowserTask!;
        }
    }
}