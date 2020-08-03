﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MessageCommunicator.Util;

namespace MessageCommunicator
{
    public class EndSymbolsMessageRecognizer : MessageRecognizer
    {
        private Encoding _encoding;
        private string _endSymbols;
        private StringBuffer _receiveStringBuffer;

        public EndSymbolsMessageRecognizer(Encoding encoding, string endSymbols)
        {
            _encoding = encoding;
            _endSymbols = endSymbols;
            _receiveStringBuffer = new StringBuffer(1024);
        }

        /// <inheritdoc />
        protected override Task<bool> SendInternalAsync(ByteStreamHandler byteStreamHandler, ReadOnlySpan<char> rawMessage)
        {
            var sendBuffer = StringBuffer.Acquire(rawMessage.Length + _endSymbols.Length);
            byte[]? bytes = null;
            try
            {
                sendBuffer.Append(rawMessage);
                sendBuffer.Append(_endSymbols, 0, _endSymbols.Length);
                sendBuffer.GetInternalData(out var buffer, out var currentCount);

                var sendMessageByteLength = _encoding.GetByteCount(buffer, 0, currentCount);
                bytes = ByteArrayPool.Take(sendMessageByteLength);

                _encoding.GetBytes(buffer, 0, currentCount, bytes, 0);
                StringBuffer.Release(sendBuffer);
                sendBuffer = null;

                return byteStreamHandler.SendAsync(
                    new ReadOnlyMemory<byte>(bytes, 0, sendMessageByteLength));
            }
            finally
            {
                if (bytes != null)
                {
                    ByteArrayPool.Return(bytes);
                }
                if (sendBuffer != null)
                {
                    StringBuffer.Release(sendBuffer);
                }
            }
        }

        public override void OnReceivedBytes(bool isNewConnection, ReadOnlySpan<byte> receivedSegment)
        {
            // Clear receive buffer on new connections
            if (isNewConnection) { _receiveStringBuffer.Clear(); }

            if (receivedSegment.Length <= 0) { return; }
            _receiveStringBuffer.Append(receivedSegment, _encoding);

            bool endSymbolsMatch;
            do
            {
                endSymbolsMatch = false;

                var receiveBufferCount = _receiveStringBuffer.Count;
                for (var indexReceiveBuffer = 0; indexReceiveBuffer < receiveBufferCount; indexReceiveBuffer++)
                {
                    if (_receiveStringBuffer[indexReceiveBuffer] != _endSymbols[0]){ continue; }

                    endSymbolsMatch = true;
                    var endSymbolIndex = indexReceiveBuffer;

                    // Check rest of endsymbol collection
                    for (var indexEndSymbol = 1; indexEndSymbol < _endSymbols.Length; indexEndSymbol++)
                    {
                        var actReceiveBufferIndex = indexReceiveBuffer + indexEndSymbol;
                        if ((actReceiveBufferIndex >= _receiveStringBuffer.Count) ||
                            (_receiveStringBuffer[actReceiveBufferIndex] != _endSymbols[indexEndSymbol]))
                        {
                            endSymbolsMatch = false;
                            endSymbolIndex = -1;
                            break;
                        }
                    }

                    // Raise found message
                    if (endSymbolsMatch)
                    {
                        // Cut out received message
                        var receiveHandler = this.ReceiveHandler;
                        if (receiveHandler != null)
                        {
                            var recognizedMessage = MessagePool.Rent(endSymbolIndex);
                            recognizedMessage.RawMessage.Append(_receiveStringBuffer.GetPart(0, endSymbolIndex));
                            receiveHandler.OnMessageReceived(recognizedMessage);
                        }

                        // Remove the message with endsymbols from receive buffer
                        _receiveStringBuffer.RemoveFromStart(endSymbolIndex + _endSymbols.Length);
                        break;
                    }
                }

            } while (endSymbolsMatch);
        }
    }
}
