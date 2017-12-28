namespace DI.P2P.Connection
{
    using System;
    using System.Linq;
    using System.Security.Cryptography;

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

        public class SetAesKeyIn
        {
            public byte[] Key { get; }

            public byte[] Iv { get; }

            public SetAesKeyIn(byte[] key, byte[] iv)
            {
                this.Key = key;
                this.Iv = iv;
            }
        }

        public class SetAesKeyOut
        {
            public byte[] Iv { get; }

            public byte[] Key { get; }

            public SetAesKeyOut(byte[] key, byte[] iv)
            {
                this.Iv = iv;
                this.Key = key;
            }
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

            this.Receive<SetAesKeyIn>(setAesKeyIn => this.ProcessSetAesKeyIn(setAesKeyIn));

            this.Receive<SetAesKeyOut>(setAesKeyOut => this.ProcessSetAesKeyOut(setAesKeyOut));
        }

        private bool receivingMessage = false;

        private int currentMessageSize = 0;

        private ByteString currentData = ByteString.Empty;

        private Aes aesIn;

        private Aes aesOut;

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

            var message = this.currentData.Slice(0, this.currentMessageSize);

            if (this.aesIn != null)
            {
                //message.CopyTo(this.aesIn.IV, 0, this.aesIn.IV.Length);
                this.aesIn.IV = message.Slice(0, this.aesIn.IV.Length).ToArray();
                this.log.Debug($"IV in: {string.Join("", this.aesIn.IV.Select(b => b.ToString("X2")))}");
                message = message.Slice(this.aesIn.IV.Length);

                using (var decryptor = this.aesIn.CreateDecryptor())
                {
                    message = ByteString.FromBytes(decryptor.TransformFinalBlock(message.ToArray(), 0, message.Count));
                }
            }

            this.messageLayer.Tell(message);

            this.currentData = this.currentData.Slice(this.currentMessageSize);
            this.currentMessageSize = 0;
            this.receivingMessage = false;
        }

        private void ProcessSendData(SendData sendData)
        {
            var data = sendData.Data;

            if (this.aesOut != null)
            {
                this.aesOut.GenerateIV();
                this.log.Debug($"IV out: {string.Join("", this.aesOut.IV.Select(b => b.ToString("X2")))}");

                using (var encryptor = this.aesOut.CreateEncryptor())
                {
                    data = ByteString.CopyFrom(this.aesOut.IV) +
                        ByteString.FromBytes(encryptor.TransformFinalBlock(data.ToArray(), 0, data.Count));
                }
            }

            byte msbMsgSize = (byte)(data.Count / 256);
            byte lsbMsgSize = (byte)(data.Count % 256);

            var dataWithHeader = ByteString.FromString("P2P") // Add magic.
                .Concat(ByteString.FromBytes(new[] { msbMsgSize, lsbMsgSize })) // Add message size.
                .Concat(data); // Add the actual message.

            this.tcpConnection.Tell(dataWithHeader);
        }

        private void ProcessSetAesKeyIn(SetAesKeyIn setAesKeyIn)
        {
            this.aesIn = Aes.Create();
            this.aesIn.Key = setAesKeyIn.Key;
        }

        private void ProcessSetAesKeyOut(SetAesKeyOut setAesKeyOut)
        {
            this.aesOut = Aes.Create();
            this.aesOut.Key = setAesKeyOut.Key;
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
