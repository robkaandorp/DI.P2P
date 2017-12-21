using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DI.P2P.Entities
{
    public class Node
    {
        public Guid Id { get; set; }
        public string[] Adresses { get; set; }
        public int Port { get; set; }
        public int Connections { get; set; }

        public override bool Equals(object obj)
        {
            var n2 = obj as Node;

            if (n2 == null)
                return false;

            return Id.Equals(n2.Id) &&
                Adresses.Equals(n2.Adresses) &&
                Port.Equals(n2.Port) &&
                Connections.Equals(n2.Connections);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode() ^ Adresses.GetHashCode() ^ Port.GetHashCode() ^ Connections.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format($"Node(Id = {this.Id}, {string.Join("|", this.Adresses)}:{this.Port}) [{this.Connections} conn.]");
        }
    }
}
