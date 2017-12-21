﻿using System;
using System.Collections.Generic;
using System.Text;

namespace DI.P2P
{
    using System.IO;

    using Akka.Actor;
    using Akka.Event;
    using Akka.IO;

    using DI.P2P.Messages;

    using ProtoBuf;

    /// <summary>
    /// Handles en/decryption and (de)serialization of incoming and outgoing data.
    /// </summary>
    public class MessageLayer : ReceiveActor
    {
        public class ConnectTransportLayer
        {
            public ConnectTransportLayer(IActorRef transportLayer)
            {
                this.TransportLayer = transportLayer;
            }

            public IActorRef TransportLayer { get; }
        }


        private readonly IActorRef protocolHandler;

        private IActorRef transportLayer;

        private readonly ILoggingAdapter log = Context.GetLogger();

        public MessageLayer(IActorRef protocolHandler)
        {
            this.protocolHandler = protocolHandler;

            this.Receive<ConnectTransportLayer>(connectTransportLayer => this.transportLayer = connectTransportLayer.TransportLayer);

            this.Receive<ByteString>(rawMsg => this.ProcessRawIncomingMessage(rawMsg));

            this.Receive<Message>(message => this.SendMessage(message));

            protocolHandler.Tell(new ProtocolHandler.ConnectMessageLayer(this.Self));
        }

        private void SendMessage(Message message)
        {
            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(stream, message);

                var data = ByteString.CopyFrom(new []{ (byte)message.GetMessageType() });
                data = data.Concat(ByteString.FromBytes(stream.ToArray()));

                this.transportLayer.Tell(new TransportLayer.SendData(data));
            }
        }

        private readonly Dictionary<byte, Type> messageTypeMap =
            new Dictionary<byte, Type>
            {
                { (byte)AnnounceMessage.MessageType, typeof(AnnounceMessage) }
            };

        private void ProcessRawIncomingMessage(ByteString rawMsg)
        {
            var msgType = rawMsg[0];
            var data = rawMsg.Slice(1).ToArray();

            if (!this.messageTypeMap.ContainsKey(msgType))
            {
                // Type not found.
                this.log.Error($"Type {msgType} not found.");
                // TODO notify sender of error?
                return;
            }

            var type = this.messageTypeMap[msgType];

            using (var stream = new MemoryStream(data))
            {
                var msg = Serializer.Deserialize(type, stream);
                this.protocolHandler.Tell(msg);
            }
        }

        public static Props Props(IActorRef protocolHandler, byte messageLayerVersion = 0)
        {
            return Akka.Actor.Props.Create(() => new MessageLayer(protocolHandler));
        }
    }
}
