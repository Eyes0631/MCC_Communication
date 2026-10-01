using System;
using System.IO;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using PaeLibGeneral;
using System.Diagnostics;
using System.Linq;

namespace MCC_Communication
{
    public class MCC_BMP_Host
    {
        private int m_serverPort = 5555;
        private string m_serverIP = "127.0.0.1";
        private TcpListener m_listener;
        private bool m_isRunning = false;

        private string UpdateMsg = string.Empty;
        private bool m_Refresh = false;
        public bool bRefresh
        {
            get { return m_Refresh; }
        }

        public string UpdateText()
        {
            string sRet = string.Empty;
            if (m_Refresh)
            {
                sRet = UpdateMsg;
                UpdateMsg = string.Empty;
                m_Refresh = false;
            }
            return sRet;
        }

        private void AddMsg(string str)
        {
            UpdateMsg += str + "\r\n";
            m_Refresh = true;
        }

        public MCC_BMP_Host()
        {
            ReadSetup();
        }

        private bool ReadSetup()
        {
            bool bRet = false;
            string _fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "System", "Setup.ini");
            if (File.Exists(_fullPath))
            {
                using (TINIFile ini = new TINIFile(_fullPath))
                {
                    m_serverPort = ini.ReadInteger("MCCLink", "ServerPort", 5555);
                    m_serverIP = ini.ReadString("MCCLink", "MainAddress", "127.0.0.1");
                    bRet = true;
                }
            }
            return bRet;
        }

        //public void Start()
        public async Task Start()
        {
            m_listener = new TcpListener(IPAddress.Parse(m_serverIP), m_serverPort);
            m_listener.Start();
            m_isRunning = true;
            Console.WriteLine("Server started on " + m_serverIP + ":" + m_serverPort);
            AddMsg("Server started on " + m_serverIP + ":" + m_serverPort);

            while (m_isRunning)
            {
                try
                {
                    //TcpClient client = m_listener.AcceptTcpClient();
                    TcpClient client = await m_listener.AcceptTcpClientAsync();
                    Thread clientThread = new Thread(HandleClient);
                    clientThread.Start(client);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                    AddMsg("Error: " + ex.Message);
                }
            }
        }

        public void Stop()
        {
            m_isRunning = false;
            m_listener.Stop();
        }

        private void HandleClient(object obj)
        {
            TcpClient client = (TcpClient)obj;
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            int bytesRead;
            // 設置接收超時為 5 秒（5000 毫秒）
            client.ReceiveTimeout = 5000;
            try
            {
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    string request = Encoding.Default.GetString(buffer, 0, bytesRead);
                    Console.WriteLine("Received: " + request);
                    AddMsg("Received: " + request);

                    // 處理請求並生成回應
                    string response = ProcessRequest(request);
                    byte[] responseBytes = Encoding.Default.GetBytes(response);

                    // 發送回應
                    stream.Write(responseBytes, 0, responseBytes.Length);
                    Console.WriteLine("Sent: " + response);
                    AddMsg("Sent: " + response);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                AddMsg("Error: " + ex.Message);
            }
            finally
            {
                client.Close();
            }
        }
        
        private string ProcessRequest(string request)
        {
            int msglength = 0;
            int dfmessagelength = 62;
            byte[] byteArr = null;
            byteArr = System.Text.Encoding.Default.GetBytes(request);
            MCC_BibMap m_BibMap = new MCC_BibMap();
            string sType = request.Substring(8, 4).Trim();
            PackType Type = (PackType)Enum.Parse(typeof(PackType), sType);
            string mode = request.Substring(62, 20).Trim();
            
            m_BibMap.PackFormat(mode, Type);
            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Reset();
                sw.Start();
                while (true)
                {
                    if (request.Length >= dfmessagelength)
                    {
                        msglength = int.Parse(request.Substring(0, 8));
                        if (request.Length >= dfmessagelength + msglength)
                        {
                            break;
                        }
                    }
                    if (sw.Elapsed.TotalSeconds > 3)
                    {
                        //JLogger.LogDebug("ServerProtocol", "(ReceiveFormHost_TotalSeconds Timout)", true);
                        AddMsg("(ReceiveFormHost_TotalSeconds Timout)");
                        break;
                    }

                }
            }
            catch (Exception ex)
            {
                JLogger.LogDebug("ServerProtocol", ex.Message, true);
                AddMsg("ERROR: " + ex.Message);
            }
            Array.Copy(byteArr, 0, m_BibMap.bytedata1, 0, m_BibMap.bytedata1.Length);
            //AddMsg("Receive:" + m_Socket.ReceiveData);
            //byteArr = System.Text.Encoding.Default.GetBytes("EEXT  E    568      568Mf2220231214181540123456789012         953960      QA-272-0                     0                      f22                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                ");
            int ret = m_BibMap.PackAnalysis(mode, PackType.DFMessage, m_BibMap.bytedata1);
            PackType mytype; 
            if (ret == 1)
            {
                switch (msglength)
                {
                    case 20:
                        mytype = PackType.ACKMessage;
                        break;
                    case 280:
                        mytype = PackType.NCKMessage;
                        //mytype = Type;
                        break;
                    default:
                        mytype = Type;
                        break;
                }
                m_BibMap.bytedata2 = new byte[msglength];
                Array.Copy(byteArr, m_BibMap.bytedata1.Length, m_BibMap.bytedata2, 0, msglength);
                //ret = m_BibMap.PackAnalysis(mode, mytype, m_BibMap.bytedata2);
                ret = m_BibMap.PackResult(mode, mytype);
                if (ret == 1)
                {
                    switch (mytype)
                    {
                        case PackType.ISB:
                        case PackType.EJB:
                        case PackType.STG:
                            PackType type = PackType.ACKMessage;
                            if (mode != "Insert" && mode != "Load" && mode != "Unload" &&
                                mode != "Exchange" && mode != "Remove" && mode != "Swap")
                            {
                                type = PackType.NCKMessage;
                            }
                            m_BibMap.PackFormat(mode, type);
                            byte[] Data = m_BibMap.bytedata1.Concat(m_BibMap.bytedata2).ToArray();
                            return System.Text.Encoding.Default.GetString(Data);
                    }
                }
            }
            return "NULL";
        }
    }
}
