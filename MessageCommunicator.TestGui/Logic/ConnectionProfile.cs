﻿using System;
using System.Collections.ObjectModel;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MessageCommunicator.TestGui.Data;

namespace MessageCommunicator.TestGui.Logic
{
    public class ConnectionProfile : IMessageReceiveHandler, IMessageCommunicatorLogger
    {
        private SynchronizationContext _syncContext;

        private MessageChannel _messageChannel;

        public string Name => this.Parameters.Name;

        public ConnectionParameters Parameters { get; }

        public ObservableCollection<LoggingMessageWrapper> DetailLogging { get; } = new ObservableCollection<LoggingMessageWrapper>();

        public ObservableCollection<LoggingMessageWrapper> Messages { get; } = new ObservableCollection<LoggingMessageWrapper>();

        public bool IsRunning => _messageChannel.IsRunning;

        public ConnectionState State => _messageChannel.State;

        public string RemoteEndpointDescription => _messageChannel.RemoteEndpointDescription;

        public ConnectionProfile(SynchronizationContext syncContext, ConnectionParameters connParams)
        {
            _syncContext = syncContext;
            this.Parameters = connParams;

            _messageChannel = SetupMessageChannel(connParams, this, this);
        }

        public async Task ChangeParametersAsync(ConnectionParameters newConnParameters)
        {
            var prefWasRunning = false;
            if (_messageChannel.IsRunning)
            {
                await _messageChannel.StopAsync();
                prefWasRunning = true;
            }

            _messageChannel = SetupMessageChannel(newConnParameters, this, this);

            if (prefWasRunning)
            {
                await _messageChannel.StartAsync();
            }
        }

        public async Task SendMessageAsync(string message)
        {
            if (await _messageChannel.SendAsync(new Message(message)))
            {
                var newLoggingMessage = new LoggingMessage(
                    DateTime.UtcNow, LoggingMessageType.Info, "OUT", message, null);

                LogTo(_syncContext, newLoggingMessage, this.DetailLogging);
                LogTo(_syncContext, newLoggingMessage, this.Messages);
            }
        }

        public Task StartAsync()
        {
            return _messageChannel.StartAsync();
        }

        public Task StopAsync()
        {
            return _messageChannel.StopAsync();
        }

        private static MessageChannel SetupMessageChannel(
            ConnectionParameters connParams,
            IMessageReceiveHandler messageReceiveHandler,
            IMessageCommunicatorLogger messageCommunicatorLogger)
        {
            // Create stream handler settings
            ByteStreamHandlerSettings streamHandlerSettings;
            switch (connParams.Mode)
            {
                case ConnectionMode.Active:
                    streamHandlerSettings = new TcpActiveByteSteamHandlerSettings(connParams.Target, connParams.Port);
                    break;

                case ConnectionMode.Passive:
                    streamHandlerSettings = new TcpPassiveByteSteamHandlerSettings(IPAddress.Any, connParams.Port);
                    break;

                default:
                    throw new ArgumentOutOfRangeException($"Unknown connection mode: {connParams.Mode}");
            }

            // Create message recognizer settings
            var messageRecognizerSettings = connParams.RecognizerSettings.CreateLibSettings();

            // Create the message channel
            return new MessageChannel(
                streamHandlerSettings, messageRecognizerSettings,
                messageReceiveHandler,
                messageCommunicatorLogger);
        }

        private static void LogTo(SynchronizationContext syncContext, LoggingMessage logMessage, ObservableCollection<LoggingMessageWrapper> collection)
        {
            syncContext.Post(arg =>
            {
                collection.Insert(0, new LoggingMessageWrapper(logMessage));
                while (collection.Count > 1000)
                {
                    collection.RemoveAt(1000);
                }
            }, null);
        }

        public void OnMessageReceived(Message message)
        {
            try
            {
                var newLoggingMessage = new LoggingMessage(
                    DateTime.UtcNow, LoggingMessageType.Info, "IN", message.ToString(), null);

                LogTo(_syncContext, newLoggingMessage, this.DetailLogging);
                LogTo(_syncContext, newLoggingMessage, this.Messages);

                message.ClearAndReturnToPool();
            }
            finally
            {
                message.ClearAndReturnToPool();
            }
        }

        public void Log(LoggingMessage loggingMessage)
        {
            LogTo(_syncContext, loggingMessage, this.DetailLogging);
        }
    }
}