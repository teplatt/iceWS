/**
* Copyright (c)2011 Tracy Platt (te_platt@yahoo.com)
* 
* Dual licensed under the MIT and GPL licenses. 
**/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Net;

namespace IceWSS
{
    public class WSS
    {
        public class WebSocketConnection
        {
            public int id;
            public Socket socket;
            public Thread thread;
        }

        private const int MAX_BUFFER_SIZE = 1024;
        private String m_AppName;
        private int m_Port;
        private List<WebSocketConnection> m_Connections;
        private Socket m_RootListenerSocket;
        public delegate string[] MessageHandler(string[] messageParameters);
        private MessageHandler mh = null;

        public WSS()
        {
        }

        public WSS(string appName, int port, MessageHandler messageHandlerCallback)
        {
            m_AppName = appName;
            m_Port = port;
            mh = messageHandlerCallback;
        }

        private void Listen()
        {
            int nextConnectionId = 0;
            while (true)
            {
                try
                {
                    Socket socket = m_RootListenerSocket.Accept();
                    WebSocketConnection connection = new WebSocketConnection();
                    connection.socket = socket;

                    connection.thread = new Thread(ProcessConnection);
                    connection.thread.IsBackground = true;
                    connection.id = nextConnectionId;
                    nextConnectionId++;
                    connection.thread.Start(connection);

                    lock (m_Connections) m_Connections.Add(connection);
                }
                catch (SocketException ex)
                {
                    Console.WriteLine("\nIn startListening\n" + ex.Message);
                }
            }
        }

        public void Start()
        {
            m_Connections = new List<WebSocketConnection>();
            m_RootListenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ipLocal = new IPEndPoint(IPAddress.Any, m_Port);
            m_RootListenerSocket.Bind(ipLocal);
            m_RootListenerSocket.Listen((int)SocketOptionName.MaxConnections);

            Thread listenThread = new Thread(Listen);
            listenThread.IsBackground = true;
            listenThread.Start();
        }

        private byte[] formatedMessage(String mess)
        {
            try
            {
                byte[] str = StrToByteArray(mess);
                byte[] val = new byte[mess.Length + 2];
                val[0] = 0;
                val[mess.Length + 1] = 0xff;

                System.Buffer.BlockCopy(str, 0, val, 1, mess.Length);
                return val;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debugger.Log(0, "1", "\nIn formattedMessage\n");
                System.Diagnostics.Debugger.Log(0, "1", ex.Message);
                return null;
            }
        }

        private void handleIncomingMessage(string mess, WebSocketConnection connection)
        {
            char[] sc = {Convert.ToChar(4), Convert.ToChar(3)};
            string[] vals = mess.Split(sc);
            List<string> messParams = new List<string>();
            for (int i = 0; i < vals.Length; i++)
            {
                if (i % 2 == 0)
                {
                    messParams.Add(vals[i]);
                }
            }
            messParams.RemoveAt(messParams.Count - 1);
            string[] returnStrings = mh(messParams.ToArray());
            
            string separator = Convert.ToChar(4).ToString();
            string returnString = "";
            foreach (string s in returnStrings)
            {
                returnString += s;
                returnString += separator;
            }
            connection.socket.Send(formatedMessage(messParams[0] + separator + messParams[1] + separator + returnString));
        }

        private void ProcessConnection(object state)
        {
            WebSocketConnection connection = (WebSocketConnection)state;
            try
            {
                byte[] buffer = new byte[MAX_BUFFER_SIZE];

                //first handle the websocket handshake
                int bytesRead = connection.socket.Receive(buffer);
                string startCheck = "GET /" + m_AppName + " HTTP/1.1";
                System.Text.UTF8Encoding enc = new System.Text.UTF8Encoding();
                string inString = enc.GetString(buffer);
                if (inString.StartsWith(startCheck))
                {
                    byte[] code = new byte[8];
                    for (int i = 0; i < 8; i++)
                    {
                        code[i] = buffer[i + bytesRead - 8];
                    }
                    SendHandshakeResponse(inString, code, connection.socket);
                    //done with the handshake - now just need to handle messages as they come
                    while (true)
                    {
                        bytesRead = connection.socket.Receive(buffer);
                        if (bytesRead > 0)
                        {
                            inString = enc.GetString(buffer);
                            String mess = inString.Substring(1, bytesRead - 2);  //strip the 0x00 and 0xff off the message
                            char[] sp = { Convert.ToChar(0), Convert.ToChar(255) };
                            string[] messes = mess.Split(sp);

                            foreach (string m in messes)
                            {
                                string mm = m;
                                byte[] t = StrToByteArray(m);
                                if (t.Length != m.Length)
                                {
                                    mm = m.Substring(0, m.Length - 1);
                                }

                                handleIncomingMessage(mm, connection);
                            }
                        }
                        else if (bytesRead == 0) 
                            return;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex);
            }
            finally
            {
                connection.socket.Close();
                lock (m_Connections) m_Connections.Remove(connection);
            }
        }

        public int GetIntFromSecret(string secret)
        {
            Int64 num1;
            string num = "";
            int numSpaces = 0;
            for (int j = 0; j < secret.Length; j++)
            {
                if (secret[j] == ' ')
                    numSpaces++;
                if (secret[j] >= '0' && secret[j] <= '9')
                {
                    num += secret[j];
                }
            }
            Int64Converter t = new Int64Converter();
            num1 = Int64.Parse(num) / numSpaces;
            return (int)num1;
        }

        private byte[] HashString(byte[] data)
        {
            System.Security.Cryptography.MD5CryptoServiceProvider x = new System.Security.Cryptography.MD5CryptoServiceProvider();
            data = x.ComputeHash(data);
            return data;
        }

        public static byte[] StrToByteArray(string str)
        {
            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
            return encoding.GetBytes(str);
        }

        public void SendHandshakeResponse(String message, byte[] code, Socket sock)
        {
            String host = "", origin = "", secret1, secret2;
            String[] values = Regex.Split(message, "\r\n");
            byte[] response = new byte[1024];
            byte[] hash = new byte[16];
            int num1 = 0, num2 = 0;

            try
            {
                for (int i = 0; i < values.Length; i++)
                {
                    if (values[i].StartsWith("Sec-WebSocket-Key1: "))
                    {
                        secret1 = values[i].Substring(20);
                        num1 = GetIntFromSecret(secret1);
                    }
                    if (values[i].StartsWith("Sec-WebSocket-Key2: "))
                    {
                        secret2 = values[i].Substring(20);
                        num2 = GetIntFromSecret(secret2);
                    }
                    if (values[i].StartsWith("Host:"))
                    {
                        host = values[i].Substring(6);
                    }
                    if (values[i].StartsWith("Origin:"))
                    {
                        origin = values[i].Substring(8);
                    }
                }
                hash[0] = (byte)(num1 >> 24);
                hash[1] = (byte)((num1 >> 16) % 256);
                hash[2] = (byte)((num1 >> 8) % 256);
                hash[3] = (byte)(num1 % 256);

                hash[4] = (byte)(num2 >> 24);
                hash[5] = (byte)((num2 >> 16) % 256);
                hash[6] = (byte)((num2 >> 8) % 256);
                hash[7] = (byte)(num2 % 256);


                for (int i = 0; i < 8; i++)
                {
                    hash[i + 8] = code[i];
                }
                hash = HashString(hash);

                int totalLen = 0;
                System.Buffer.BlockCopy(StrToByteArray("HTTP/1.1 101 WebSocket Protocol Handshake\r\nUpgrade: WebSocket\r\nConnection: Upgrade\r\nSec-WebSocket-Origin: "), 0, response, 0, StrToByteArray("HTTP/1.1 101 WebSocket Protocol Handshake\r\nUpgrade: WebSocket\r\nConnection: Upgrade\r\nSec-WebSocket-Origin: ").Length);
                totalLen += StrToByteArray("HTTP/1.1 101 WebSocket Protocol Handshake\r\nUpgrade: WebSocket\r\nConnection: Upgrade\r\nSec-WebSocket-Origin: ").Length;
                byte[] t = StrToByteArray(origin);
                int len = t.Length;
                System.Buffer.BlockCopy(t, 0, response, totalLen, len);
                totalLen += len;
                System.Buffer.BlockCopy(StrToByteArray("\r\nSec-WebSocket-Location: ws://"), 0, response, totalLen, StrToByteArray("\r\nSec-WebSocket-Location: ws://").Length);
                totalLen += StrToByteArray("\r\nSec-WebSocket-Location: ws://").Length;
                System.Buffer.BlockCopy(StrToByteArray(host), 0, response, totalLen, StrToByteArray(host).Length);
                totalLen += StrToByteArray(host).Length;
                String echo = "/" + m_AppName + "\r\n\r\n";
                System.Buffer.BlockCopy(StrToByteArray(echo), 0, response, totalLen, StrToByteArray(echo).Length);
                totalLen += StrToByteArray(echo).Length;
                System.Buffer.BlockCopy(hash, 0, response, totalLen, hash.Length);
                totalLen += hash.Length;
                response[totalLen] = 0;

                sock.Send(response, totalLen, SocketFlags.None);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debugger.Log(0, "1", "\nError handling handshake\n");
                System.Diagnostics.Debugger.Log(0, "1", ex.Message);
            }
        }
    }
}
