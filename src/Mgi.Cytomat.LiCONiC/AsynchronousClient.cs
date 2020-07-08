using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Mgi.Cytomat.LiCONiC
{
    internal class AsynchronousClient
    {
        private Socket client;
        // The port number for the remote device.
        //private const int port = 3336;
        // ManualResetEvent instances signal completion.
        private ManualResetEvent connectDone = new ManualResetEvent(false);
        private ManualResetEvent sendDone = new ManualResetEvent(false);
        private ManualResetEvent receiveDone = new ManualResetEvent(false);
        // The response from a remote StoreX Server.
        private String response = String.Empty;
        public Socket getSocket()
        {
            return client;
        }
        public void StartClient(string STXIPAddress, int STXport)
        {
            // Connect to a remote device.
            try
            {
                // Establish the remote endpoint for the socket.
                IPHostEntry ipHostInfo = Dns.GetHostEntry(STXIPAddress);
                IPAddress ipAddress = ipHostInfo.AddressList[0];
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, STXport);
                // Create a TCP/IP socket.
                client = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);
                // Connect to the remote endpoint.
                client.BeginConnect(remoteEP,
                new AsyncCallback(ConnectCallback), client);
                connectDone.WaitOne();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        public void StopClient()
        {
            // Release the socket.
            client.Shutdown(SocketShutdown.Both);
            client.Close();
        }
        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket client = (Socket)ar.AsyncState;
                // Complete the connection.
                client.EndConnect(ar);
                Console.WriteLine("Socket connected to {0}",
                client.RemoteEndPoint.ToString());
                // Signal that the connection has been made.
                connectDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        private void Receive(Socket client)
        {
            try
            {
                // Create the state object.
                StateObject state = new StateObject
                {
                    workSocket = client
                };
                // Begin receiving the data from the remote device.
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        private void ReceiveCallback(IAsyncResult ar)
        {
            if (!client.Connected) return;
            try
            {
                StateObject state = (StateObject)ar.AsyncState;
                Socket client = state.workSocket;
                int bytesRead = client.EndReceive(ar);
                if (bytesRead > 0)
                {
                    // There might be more data, so store the data received so far.
                    state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0,
                    bytesRead));
                    // Get the rest of the data.
                    client.BeginReceive(state.buffer, 0, StateObject.BufferSize,
                    0, new AsyncCallback(ReceiveCallback), state);
                    response = Encoding.ASCII.GetString(state.buffer, 0, bytesRead);
                    Console.WriteLine("Received from STX: " + response);
                    receiveDone.Set();
                }
                else
                {
                    // All the data has arrived; put it in response.
                    if (state.sb.Length > 1)
                    {
                        response = state.sb.ToString();
                    }
                    // Signal that all bytes have been received.
                    receiveDone.Set();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        public string Send(string data)
        {
            response = "";
            // Convert the string data to byte data using ASCII encoding.
            byte[] byteData = Encoding.ASCII.GetBytes(data);
            // Begin sending the data to the remote device.
            client.BeginSend(byteData, 0, byteData.Length, 0,
            new AsyncCallback(SendCallback), client);
            Console.WriteLine("Sent STRING to STX server: " + data);
            Receive(client);
            receiveDone = new ManualResetEvent(false);
            receiveDone.WaitOne();
            return response;
        }
        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket client = (Socket)ar.AsyncState;
                // Complete sending the data to the remote device.
                int bytesSent = client.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to server.", bytesSent);
                // Signal that all bytes have been sent.
                sendDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    } // End of Socket's client class
}
