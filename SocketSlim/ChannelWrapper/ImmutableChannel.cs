﻿using System;
using System.IO;
using System.Net.Sockets;
using SocketSlim.Client;

namespace SocketSlim.ChannelWrapper
{
    /// <summary>
    /// Channel that's bound to a single <see cref="Socket"/>.
    /// </summary>
    public class ImmutableChannel : ISocketChannel
    {
        private readonly ChannelWrapperBase wrapper;

        public ImmutableChannel(Socket channelSocket, SocketAsyncEventArgs receiver, SocketAsyncEventArgs sender, DirectBytesReceivedEventArgs receiverArgs, MemoryStream senderWriter)
        {
            wrapper = new ChannelWrapperBase(channelSocket, receiver, receiverArgs, sender, senderWriter);
            wrapper.BytesReceived += OnBytesReceived;
            wrapper.Closed += OnChannelClosed;
            wrapper.DuplexChannelClosed += OnChannelError;
        }

        /// <summary>
        /// Call this after you've subscribed to all event so that you don't miss first events.
        /// </summary>
        public void Start()
        {
            wrapper.Start();
        }

        protected virtual void OnBytesReceived(object o, BytesReceivedEventArgs e)
        {
            byte[] msg = new byte[e.Size];

            Buffer.BlockCopy(e.Buffer, e.Offset, msg, 0, e.Size);

            RaiseBytesReceived(msg);

            e.Proceed();
        }

        protected virtual void OnChannelClosed(object o, EventArgs e)
        {
            RaiseClosed();
        }

        protected virtual void OnChannelError(object o, ChannelCloseEventArgs e)
        {
            if (e.Exception != null)
            {
                RaiseError(e.Exception);
            }
            else if (e.SocketError != null)
            {
                RaiseError(new SocketErrorException(e.SocketError.Value));
            }

            // when both are null, the socket is closed normally
        }

        public void Send(byte[] bytes)
        {
            wrapper.Send(bytes);
        }

        public event ChannelMessageHandler<ImmutableChannel> BytesReceived;

        protected void RaiseBytesReceived(byte[] message)
        {
            ChannelMessageHandler<ImmutableChannel> handler = BytesReceived;
            if (handler != null)
            {
                handler(this, message);
            }
        }

        public event EventHandler Closed;

        protected virtual void RaiseClosed()
        {
            EventHandler handler = Closed;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        public event EventHandler<ExceptionEventArgs> Error;

        protected void RaiseError(Exception e)
        {
            EventHandler<ExceptionEventArgs> handler = Error;
            if (handler != null)
            {
                handler(this, new ExceptionEventArgs(e));
            }
        }

        public void Close()
        {
            wrapper.Close();
        }
    }
}