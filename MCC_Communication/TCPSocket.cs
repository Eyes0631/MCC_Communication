using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using PaeLibGeneral;
using System.Diagnostics;
using System.Threading;


namespace MCC_Communication
{
    public enum ConnectState
    {
        ConnectFail,
        ConnectTimeOut,
        ConfigIsNull,
        ReceiveSuccessful
    }
    public class SocketHelper
    {
        private Socket _socket;
        private SocketAsyncEventArgs _receiveArgs;

        public SocketHelper(Socket socket)
        {
            _socket = socket;
            _receiveArgs = new SocketAsyncEventArgs();
            _receiveArgs.Completed += ReceiveCompleted;
            _receiveArgs.SetBuffer(new byte[2048], 0, 2048);
        }

        public Task<int> ReceiveAsync()
        {
            return Task.Factory.FromAsync(
            _socket.BeginReceive(_receiveArgs.Buffer, 0, _receiveArgs.Buffer.Length, SocketFlags.None, null, null),
            ar => _socket.EndReceive(ar));
        }

        private void ReceiveCompleted(object sender, SocketAsyncEventArgs e)
        {
            // ... 處理接收完成的事件 ...
        }
    }
    public class TCPSocket
    {
        private Socket m_ClientSocket = null;
        private SocketConfig m_SocketConfig = null;
        private string m_ReceiveData = string.Empty;
        private string m_ListenData = string.Empty;

        public TCPSocket(SocketConfig config)
        {
            m_SocketConfig = config;
        }

        private void StartListening()
        {
            Thread listeningThread = new Thread(ListenForMessages);
            listeningThread.IsBackground = true;
            listeningThread.Start();
        }
        //public bool bListening = false;
        public ManualResetEvent WaitingResultEvent = new ManualResetEvent(false);
        private void ListenForMessages()
        {
            if (m_ClientSocket == null || !m_ClientSocket.Connected)
            {
                Console.WriteLine("Client socket is not connected.");
                //bListening = false;
                return;
            }

            byte[] buffer = new byte[2048];
            try
            {
                m_ClientSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), buffer);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error starting receive: " + ex.Message);
                //bListening = false;
            }
        }

        private  void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                byte[] buffer = (byte[])ar.AsyncState;
                int bytesRead = m_ClientSocket.EndReceive(ar);
                if (bytesRead > 0)
                {
                    string message = Encoding.Default.GetString(buffer, 0, bytesRead);
                    m_ListenData += message;
                    m_ReceiveData += message;
                    Console.WriteLine("Received message from host: " + message);
                    WaitingResultEvent.Set();
                    // 在這裡處理接收到的訊息

                    // 繼續接收數據
                    m_ClientSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), buffer);
                }
                else
                {
                    // 如果沒有接收到數據，重新開始接收
                    m_ClientSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), buffer);
                }
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.TimedOut)
                {
                    Console.WriteLine("Receive timed out.");
                }
                else
                {
                    Console.WriteLine("Socket error: " + ex.Message);
                }

                // 在發生 SocketException 後重新開始接收數據
                Connect();
                byte[] buffer = new byte[2048];
                m_ClientSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), buffer);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);

                // 在發生其他異常後重新開始接收數據
                //Connect();
                Connect();
                byte[] buffer = new byte[2048];
                m_ClientSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), buffer);
            }
        }
        //private void ListenForMessages()
        //{
        //    while (true)
        //    {
        //        if (this.IsConnected)
        //        {
        //            if (bListening)
        //            {
        //                byte[] btmp = new byte[2048];
        //                int dfmessagelength = 62;
        //                int msglength = 0;
        //                m_ReceiveData = string.Empty;

        //                int len = m_ClientSocket.Receive(btmp);

        //                m_ListenData += System.Text.Encoding.Default.GetString(btmp, 0, len);
        //                if (m_ListenData.Length >= dfmessagelength)
        //                {
        //                    msglength = int.Parse(m_ListenData.Substring(0, 8));
        //                    if (m_ListenData.Length >= dfmessagelength + msglength)
        //                    {
        //                        break;
        //                    }
        //                }
        //                //try
        //                //{
        //                //    Stopwatch sw = new Stopwatch();
        //                //    sw.Reset();
        //                //    sw.Start();
        //                //    m_ClientSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 10000);
        //                //    while (true)
        //                //    {
        //                //        int len = m_ClientSocket.Receive(btmp);

        //                //        m_ListenData += System.Text.Encoding.Default.GetString(btmp, 0, len);
        //                //        if (m_ListenData.Length >= dfmessagelength)
        //                //        {
        //                //            msglength = int.Parse(m_ListenData.Substring(0, 8));
        //                //            if (m_ListenData.Length >= dfmessagelength + msglength)
        //                //            {
        //                //                break;
        //                //            }
        //                //        }
        //                //        if (sw.Elapsed.TotalSeconds > 3)
        //                //        {
        //                //            break;
        //                //        }

        //                //    }
        //                //}
        //                //catch (SocketException ex)
        //                //{
        //                //    Console.WriteLine("Error receive: " + ex.Message);
        //                //}
        //            }
        //        }
        //        Thread.Sleep(100);
        //    }
        //}

        public string ListenData
        {
            get { return m_ListenData; }
            set { m_ListenData = value; }
        }

        public string ReceiveData
        {
            get { return m_ReceiveData; }
        }

        public bool IsConnected
        {
            get { return m_ClientSocket.Connected; }
        }

        public bool Connect()
        {
            try
            {
                if (this.m_SocketConfig == null)
                {
                    return false;
                }
                IPAddress ip = IPAddress.Parse(m_SocketConfig.IPaddress);
                IPEndPoint server = new IPEndPoint(ip, m_SocketConfig.Port);
                m_ClientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                m_ClientSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                m_ClientSocket.Connect(server);

                StartListening();
            }
            catch (Exception ex)
            {
                JLogger.LogDebug("ServerProtocol", "TCPSocket Connect function error = " + ex.Message, true);
                return false;
            }
            return this.IsConnected;
        }
        public bool Disconnect()
        {
            try
            {
                if (this.m_SocketConfig == null)
                {
                    return false;
                }
                if (m_ClientSocket != null)
                {
                    m_ClientSocket.Close();
                    m_ClientSocket.Dispose();
                }
            }
            catch (Exception ex)
            {
                JLogger.LogDebug("ServerProtocol", "TCPSocket Disconnect function error = " + ex.Message, true);
                return false;
            }
            return true;
        }


        public ConnectState SendToHost(byte[] sendData, int receiveLength)
        {
            if (this.m_SocketConfig == null)
            {
                return ConnectState.ConfigIsNull;
            }
            if (!this.IsConnected)
            {
                return ConnectState.ConnectFail;
            }

            while (true)
            {
                SocketError error = new SocketError();
                WaitingResultEvent.Reset();
                m_ClientSocket.Send(sendData, 0, sendData.Length, SocketFlags.None, out error);
                if (error == SocketError.Success)
                {
                    break;
                }

            }
            return ConnectState.ConfigIsNull;
            //return ReceiveFormHost(receiveLength);
        }

        private ConnectState ReceiveFormHost(int receiveLength)
        {
            byte[] btmp = new byte[2048];
            int dfmessagelength = 62;
            int msglength = 0;
            m_ReceiveData = string.Empty;
            if (!this.IsConnected)
            {
                return ConnectState.ConnectFail;
            }


            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Reset();
                sw.Start();
                //JLogger.LogDebug("ServerProtocol", "(ReceiveFormHost_SetSocketOption Start)", true);
                m_ClientSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 10000);
                //JLogger.LogDebug("ServerProtocol", "(ReceiveFormHost_SetSocketOption End)", true);
                while (true)
                {
                    int len = 0;
                    if (!string.IsNullOrEmpty(m_ListenData))
                    {
                        len = m_ListenData.Length;
                        m_ReceiveData = m_ListenData;
                    }
                    else
                    {
                        len = m_ClientSocket.Receive(btmp);
                        m_ReceiveData += System.Text.Encoding.Default.GetString(btmp, 0, len);
                    }
                    
                    if (m_ReceiveData.Length >= dfmessagelength)
                    {
                        msglength = int.Parse(m_ReceiveData.Substring(0, 8));
                        if (m_ReceiveData.Length >= dfmessagelength + msglength)
                        {
                            break;
                        }
                    }
                    if (sw.Elapsed.TotalSeconds > 3)
                    {
                        JLogger.LogDebug("ServerProtocol", "(ReceiveFormHost_TotalSeconds Timout)", true);
                        return ConnectState.ConnectTimeOut;//timeout
                    }

                }
            }
            //catch (Exception ex)
            //{
            //    JLogger.LogDebug("ServerProtocol", "(ReceiveFormHost_Exception " + ex.ToString() + ")", true);
            //    JLogger.LogDebug("ServerProtocol", "(ReceiveFormHost_m_ReceiveData = " + m_ReceiveData + ")", true);
            //    JLogger.LogDebug("ServerProtocol", "(ReceiveFormHost_m_dfmessagelength = " + dfmessagelength.ToString() + ")", true);
            //    JLogger.LogDebug("ServerProtocol", "(ReceiveFormHost_m_msglength = " + msglength.ToString() + ")", true);
            //    return ConnectState.ConnectTimeOut;//timeout
            //}
            catch (SocketException ex)
            {
                Console.WriteLine("Error receive: " + ex.Message);
                return ConnectState.ConnectTimeOut;
            }
            finally
            {
                //bListening = true;
            }
            return ConnectState.ReceiveSuccessful;
        }
    }
    public class SocketConfig
    {
        private string m_ipAddress = null;
        private int m_port = 0;
        public SocketConfig(string ipAddress, int port)
        {
            m_ipAddress = ipAddress;
            m_port = port;
        }
        public string IPaddress
        {
            get { return m_ipAddress; }
            set
            {
                m_ipAddress = value;
            }
        }
        public int Port
        {
            get { return m_port; }
            set
            {
                m_port = value;
            }
        }
    }
}
