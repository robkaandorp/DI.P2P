using System;
using System.Collections.Generic;
using System.Text;

namespace DI.P2P
{
    using Akka.Actor;
    using Akka.Event;
    using Akka.IO;

    public class TransportLayer : ReceiveActor
    {
        public class Connected { }

        public class Disconnected { }

        public class SendData
        {
            public SendData(ByteString data)
            {
                this.Data = data;
            }

            public ByteString Data { get; }
        }


        private readonly IActorRef tcpConnection;

        private readonly IActorRef messageLayer;

        private readonly ILoggingAdapter log = Context.GetLogger();

        public TransportLayer(IActorRef tcpConnection, IActorRef messageLayer)
        {
            this.tcpConnection = tcpConnection;
            this.messageLayer = messageLayer;

            this.Receive<Connected>(connected => messageLayer.Tell(new MessageLayer.Connected()));

            this.Receive<Disconnected>(connected => messageLayer.Tell(new MessageLayer.Disconnected()));

            this.Receive<ByteString>(
                data =>
                    {
                        this.currentData = this.currentData.Concat(data);

                        if (!this.receivingMessage)
                        {
                            this.ProcessPreamble();
                        }
                        else
                        {
                            this.ProcessMessageData();
                        }
                    });

            this.Receive<SendData>(sendData => this.ProcessSendData(sendData));
        }

        private bool receivingMessage = false;

        private int currentMessageSize = 0;

        private ByteString currentData = ByteString.Empty;

        private void ProcessPreamble()
        {
            if (this.currentData.Count < 5)
            {
                return;
            }

            if (!(this.currentData[0] == 'P' && this.currentData[1] == '2' && this.currentData[2] == 'P'))
            {
                this.log.Error("Incorrect magic.");
                // TODO how to handle this? Clear the currentData byte string? Try to find the next 'P'?
                this.currentData = ByteString.Empty;
                return;
            }

            byte msbMsgSize = this.currentData[3];
            byte lsbMsgSize = this.currentData[4];
            this.currentMessageSize = msbMsgSize * 256 + lsbMsgSize;

            this.currentData = this.currentData.Slice(5);

            this.receivingMessage = true;
            this.ProcessMessageData();
        }

        private void ProcessMessageData()
        {
            if (this.currentData.Count < this.currentMessageSize)
            {
                return;
            }

            this.messageLayer.Tell(this.currentData.Slice(0, this.currentMessageSize));

            this.currentData = this.currentData.Slice(this.currentMessageSize);
            this.currentMessageSize = 0;
            this.receivingMessage = false;
        }

        private void ProcessSendData(SendData sendData)
        {
            byte msbMsgSize = (byte)(sendData.Data.Count / 256);
            byte lsbMsgSize = (byte)(sendData.Data.Count % 256);

            var data = ByteString.FromString("P2P") // Add magic.
                .Concat(ByteString.FromBytes(new[] { msbMsgSize, lsbMsgSize })) // Add message size.
                .Concat(sendData.Data); // Add the actual message.

            this.tcpConnection.Tell(data);
        }

        public static Props Props(IActorRef tcpConnection, byte transportLayerVersion, IActorRef messageLayer)
        {
            if (transportLayerVersion != Versions.TransportLayer)
            {
                throw new InvalidOperationException($"Invalid transport layer version {transportLayerVersion}. Was expecting {Versions.TransportLayer}.");
            }

            return Akka.Actor.Props.Create(() => new TransportLayer(tcpConnection, messageLayer));
        }
    }
}
