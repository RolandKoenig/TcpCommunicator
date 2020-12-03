﻿using System;
using System.Reactive;
using System.Text;
using System.Text.RegularExpressions;
using MessageCommunicator.TestGui.Logic;
using MessageCommunicator.Util;
using ReactiveUI;

namespace MessageCommunicator.TestGui.Views
{
    public class ConnectionProfileViewModel : OwnViewModelBase
    {
        private bool _isRunning;
        private ConnectionState _connState;
        private string _remoteEndpointDescription;

        public IConnectionProfile Model { get; }

        public ReactiveCommand<object?, Unit> Command_Start { get; }

        public ReactiveCommand<object?, Unit> Command_Stop { get; }

        public ReactiveCommand<string?, Unit> Command_SendMessage { get; }

        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                if (_isRunning != value)
                {
                    _isRunning = value;
                    this.RaisePropertyChanged(nameof(this.IsRunning));
                    this.RaisePropertyChanged(nameof(this.CanStart));
                    this.RaisePropertyChanged(nameof(this.CanStop));
                }
            }
        }

        public bool CanStart => !this.IsRunning;

        public bool CanStop => this.IsRunning;

        public SendFormattingMode SendFormattingMode { get; set; }

        public SendFormattingMode[] SendFormattingModeList => (SendFormattingMode[])Enum.GetValues(typeof(SendFormattingMode));

        public ConnectionState State
        {
            get => _connState;
            set
            {
                if (_connState != value)
                {
                    _connState = value;
                    this.RaisePropertyChanged(nameof(this.State));
                }
            }
        }

        public string RemoteEndpointDescription
        {
            get => _remoteEndpointDescription;
            set
            {
                if (_remoteEndpointDescription != value)
                {
                    _remoteEndpointDescription = value;
                    this.RaisePropertyChanged(nameof(this.RemoteEndpointDescription));
                }
            }
        }

        public LoggingViewModel MessageLoggingViewModel { get; }

        public LoggingViewModel DetailLoggingViewModel { get; }

        public ConnectionProfileViewModel(IConnectionProfile connProfile)
        {
            this.Model = connProfile;
            _remoteEndpointDescription = string.Empty;

            this.MessageLoggingViewModel = new LoggingViewModel(connProfile.Messages);
            this.DetailLoggingViewModel = new LoggingViewModel(connProfile.DetailLogging);

            this.Command_Start = ReactiveCommand.CreateFromTask<object?>(async arg =>
            {
                if (!this.Model.IsRunning)
                {
                    await this.Model.StartAsync();
                }
            });
            this.Command_Stop = ReactiveCommand.CreateFromTask<object?>(async arg =>
            {
                if (this.Model.IsRunning)
                {
                    await this.Model.StopAsync();
                }
            });
            this.Command_SendMessage = ReactiveCommand.CreateFromTask<string?>(async message =>
            {
                try
                {
                    message ??= string.Empty;

                    switch (this.SendFormattingMode)
                    {
                        case SendFormattingMode.Plain:
                            break;

                        case SendFormattingMode.Escaped:
                            message = Regex.Unescape(message);
                            break;

                        case SendFormattingMode.BinaryHex:
                            var encoding = Encoding.GetEncoding(this.Model.Parameters.RecognizerSettings.Encoding);
                            message = encoding.GetString(HexFormatUtil.ToByteArray(message));
                            break;

                        default:
                            throw new InvalidOperationException($"Unhandled {nameof(Views.SendFormattingMode)} {this.SendFormattingMode}!");
                    }

                    await this.Model.SendMessageAsync(message);
                }
                catch (Exception e)
                {
                    CommonErrorHandling.Current.ShowErrorDialog(e);
                }
            });
        }

        public void RefreshData()
        {
            this.IsRunning = this.Model.IsRunning;
            this.State = this.Model.State;
            this.RemoteEndpointDescription = this.Model.RemoteEndpointDescription;
        }
    }
}
