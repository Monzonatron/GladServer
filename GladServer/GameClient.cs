using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Sockets;
using System.IO;
using MySql.Data.MySqlClient;

namespace GladServer
{
    class GameClient 
    {
        MySqlConnection DatabaseConnection;
        TcpClient m_client;
        StreamWriter m_outstream;
        StreamReader m_instream;
        String m_username;
        Thread m_clientReadThread;

        Queue<String> IncomingMessages = new Queue<String>();

        public GameClient(TcpClient a_client, StreamWriter a_writer, StreamReader a_reader, String a_username)
        {
            m_client = a_client;
            m_instream = a_reader;
            m_outstream = a_writer;
            m_username = a_username;

            RunLoop();
        }

        public void RunLoop()
        {
            m_clientReadThread = new Thread(ReadFromClient);
            m_clientReadThread.Start();

            while (true)
            {
                if ( !m_client.Connected )
                {
                    Console.WriteLine(m_username + " has disconnected.");
                    m_client.Close();
                    return;
                }
                if ( DoWeNeedToReply() )
                {
                    String response = ConstructResponse();
                    SendtoClient(response);
                }
                else 
                {
                    System.Threading.Thread.Sleep(40);
                }
            }
        }
        
        private Boolean DoWeNeedToReply()
        {
            if (IncomingMessages.Count == 0) return false;
            else return true;
        }

        private void SendtoClient(String a_message)
        {
            m_outstream.WriteLineAsync(a_message);
            Console.WriteLine("Sent message to user " + m_username + " : " + a_message);
            m_outstream.Flush();
        }

        

        private void ReadFromClient()
        {
            while (true)
            {
                String clientmsg = "";
                if (m_instream.Peek() == -1)
                {
                    System.Threading.Thread.Sleep(40);
                }
                else
                {
                    clientmsg = m_instream.ReadLine();
                    Console.WriteLine("Recieved message from user " + m_username + " : " + clientmsg);
                    
                    clientmsg = clientmsg.Trim();
                    clientmsg = clientmsg.Trim('\n');
                    clientmsg = clientmsg.Trim('\r');
                    clientmsg = clientmsg.Trim('\n');
                    clientmsg = clientmsg.Trim('\r');
                    IncomingMessages.Enqueue(clientmsg);
                }
            }
        }

        public String ConstructResponse()
        {
            String Response = "";

            String query = IncomingMessages.Dequeue();

            String [] queries = query.Split('&');
            foreach ( string command in queries )
            {
                String[] paramvalue = command.Split('=');

                if ( paramvalue[0] == "GC")
                {
                    switch(paramvalue[1])
                    {
                        case "GETGLADIATORS":
                            break;
                        case "GETMONEYCOUNT":
                            Response += "&MONEYCOUNT=" + GetMoneyCount();
                            break;
                       
                        default:
                            break;
                    }
                }

            }

            return Response;
        }

        String GetMoneyCount()
        {
            String money = "";
            ConnectToDatabase();

            String sqlCommand = "SELECT moneycount FROM users WHERE username = \"" + m_username +"\"";
            MySqlCommand command = new MySqlCommand(sqlCommand, DatabaseConnection);
            MySqlDataReader reader = command.ExecuteReader();
            reader.Read();
            money = reader.GetString(0);

            CloseDatabase();
            return money;
        }




        public void ConnectToDatabase()
        {
            try
            {
                DatabaseConnection = new MySqlConnection(xVariables.MYSQL_LOGIN);
                DatabaseConnection.Open();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error connecting to database.");
                Console.WriteLine(e);
                Environment.Exit(5);
            }

        }
        public void CloseDatabase()
        {
            DatabaseConnection.Close();
        }


    }
}
