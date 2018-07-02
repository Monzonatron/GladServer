using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Net.Sockets;
using MySql.Data.MySqlClient;
using MySql.Data;

namespace GladServer
{
    class ServerHeart
    {


        GameClient myClient;

        MySqlConnection DatabaseConnection;
        private List<TcpClient> loginClients;

        //Sockets
        private TcpListener inLoginSocket;

        //Threads for Sockets
        private Thread loginThread;
        private Thread ReadCommands;

        private uint LoginClientCount = 0;



        /// <summary>
        /// Runs the server from life to death. Errors should be logged in 
        /// an errors.txt file (not yet defined).
        /// </summary>
        public void RunServer()
        {
            StartSockets();

            //Run the Login Port
            loginThread = new Thread(StartLoginThread);
            loginThread.Start();

            ReadCommands = new Thread(InputIOThread);
            ReadCommands.Start();
 
            while (true)
            {
                Thread.Sleep(10000);
            }

            CleanupServer();
        }

        /// <summary>
        /// Opens and begins the server sockets. Use once per server life.
        /// </summary>
        public void StartSockets()
        {
            inLoginSocket = new TcpListener(xVariables.SOCKET_LOGIN);

            ConnectToDatabase();

            inLoginSocket.Start();

            Console.WriteLine("Server Started!\n");
        }






        public void InputIOThread()
        {
            while (true)
            {
                string prompt = Console.ReadLine();
                prompt = prompt.ToLower();

                switch (prompt)
                {
                    case "hello":
                        Console.WriteLine("Console: Hello!");
                        break;
                    case "addglads1":
                        AddGladsToDatabase(1);
                        break;
                    case "addglads10":
                        AddGladsToDatabase(10);
                        break;
                    case "addglads100":
                        AddGladsToDatabase(100);
                        break;
                    case "addglads1000":
                        AddGladsToDatabase(1000);
                        break;
                    case "addglads10000":
                        AddGladsToDatabase(10000);
                        break;
                    default:
                        Console.WriteLine("Unrecognized Command.");
                        break;
                }
            }
        }

        static public void AddGladsToDatabase(int count)
        {
            
        }








        ////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////

        //These are the main Juicy threads thta handle everything for our game

        public void StartLoginThread()
        {
            inLoginSocket.BeginAcceptTcpClient(DoConnection, inLoginSocket);
        }



        private void DoConnection(IAsyncResult result)
        {
            //Restart the TCP listener
            StartLoginThread();
            /////////////////////////


            //Grab the Client Listener
            TcpClient client = ((TcpListener)result.AsyncState).EndAcceptTcpClient(result);
            if (!client.Connected) return;


            //Get the client action
            String message = ReadClientMessage(client);

            //See if the client needs to register. If they are registering, this returns true. Send successful or not and disconnect the client.
            String registerresult = "";
           if ( DoClientRegister(message, ref registerresult) )
           {
               SendClientMessage(client, registerresult);
               client.Close();
               return;
           }

           String loginresult = "";
           String username = "";

            //If this is not a register, the client must be loggin in to play. Anything else will result in disconnect.
            if ( DoClientLogin(message, ref loginresult, ref username) )
            {
                if ( loginresult == "0000" )
                {
                    StreamWriter writer = new StreamWriter(client.GetStream());
                    StreamReader reader = new StreamReader(client.GetStream());
                    SendClientMessage(client, writer, loginresult);
                    
                    myClient = new GameClient(client, writer, reader, username);
                    
                }

                else
                {
                    SendClientMessage(client, loginresult);
                    client.Close();
                }
            }
            else
            {
                SendClientMessage(client, loginresult);
                client.Close();
            }
        }

        ////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////

        public Boolean DoClientLogin(String a_message, ref String a_result, ref String a_username)
        {
            String username = "";
            String password = "";

            if (a_message.Contains("ACTION=login") && !a_message.Contains("ACTION=game"))
            {
                String[] net_params = a_message.Split('&');
                foreach (string s in net_params)
                {
                    String[] commands = s.Split('=');
                    switch (commands[0])
                    {
                        case "USERNAME":
                            username = commands[1];
                            break;
                        case "PASSWORD":
                            password = commands[1];
                            break;
                        default:
                            break;
                    }
                }

                String code = DoMYSQLLogin(username, password);

                if (code == "0000")
                {
                    a_result = "0000";
                    a_username = username;
                    return true;
                }
                else
                {
                    //Wrong password dumas
                    a_result = code;
                    return false;
                }

                //Try querying from MYSQL Database.


            }

            else
            {
                a_result = "0005";
                return false;
            }
        }

        public Boolean DoClientRegister(String a_message, ref String a_result)
        {
            string username = "";
            string password = "";
            string email = "";
            if (a_message.Contains("ACTION=register") && !a_message.Contains("ACTION=game"))
            {
                String[] net_params = a_message.Split('&');
                foreach (string s in net_params)
                {
                    String[] commands = s.Split('=');
                    switch (commands[0])
                    {
                        case "USERNAME":
                            username = commands[1];
                            break;
                        case "PASSWORD":
                            password = commands[1];
                            break;
                        case "EMAIL":
                            email = commands[1];
                            break;
                    }
                }
                //Make sure registration parameters dont exist



                //Try doing a registration next
                if (username != "" && password != "" && email != "")
                {
                    try
                    {
                        ConnectToDatabase();
                        String SqlCommand = "INSERT INTO users (UserName, Password, Email) " +
                            "VALUES (@username,@password,@email);";
                        MySqlCommand register = new MySqlCommand(SqlCommand, DatabaseConnection);
                        register.Parameters.AddWithValue("@username", username);
                        register.Parameters.AddWithValue("@password", password);
                        register.Parameters.AddWithValue("@email", email);

                        //Execute Database Command
                        MySqlDataReader reader = register.ExecuteReader();
                        Console.WriteLine("User " + username + " has registered.");
                        CloseDatabase();
                        a_result = "0000";
                        return true;
                    }
                    catch(Exception e)
                    {
                        a_result = "0004";
                        Console.WriteLine(e);
                        return true;
                    }
                }
                else
                {
                    //Wrong kind of login credentials
                    a_result = "0003";
                    return true;
                }
            }
            //This is not registration 
            else return false;
        }


        public String DoMYSQLLogin(String username, String password)
        {
            ConnectToDatabase();
            if (username != "" && password != "")
            {

                String SqlCommand = "SELECT password from users WHERE username = @username";
                MySqlCommand login = new MySqlCommand(SqlCommand, DatabaseConnection);
                login.Parameters.AddWithValue("@username", username);


                //Execute Database Command
                MySqlDataReader reader = login.ExecuteReader();

                //Check to see if we actually have a username
                if (reader.HasRows)
                {
                    reader.Read();
                    if (reader.GetString(0) == password)
                    {
                        Console.WriteLine("User " + username + " has entered correct credentials and is logging in. ");
                        return "0000";
                    }
                    //Wrong password!
                    else return "0008";
                }
                //No such username!
                else return "0007";
            }
            else
            {
                CloseDatabase();
                return "0006";
            }
        }

        /// <summary>
        /// returns true if the client version is incorrect.
        /// </summary>
        /// <param name="message">unmodified client message</param>
        /// <returns></returns>
        public bool CheckForIncorrectClientVer(String message)
        {
            try
            {
                String[] array = message.Split('&');
                if (array[0].Contains("CLIENTVER"))
                {
                    String[] clientVerArray = array[0].Split('=');
                    if (clientVerArray[1] == xVariables.CLIENT_VERSION.ToString())
                    {
                        return false;
                    }
                    else return true;
                }
                else
                {
                    return true;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return true;
            }
            return false;
        }

        public String ReadClientMessage(TcpClient a_client)
        {
            String message;
            //Message Buffer from the client apparently
            byte[] bytesFrom = new byte[10025];
            //Represents the actual data deserialized from bytebuffer
            string dataFromClient = null;

            try
            {
                //Gets the message from the client apparently
                NetworkStream networkStream = a_client.GetStream();
                //Required for reading the stream, these two go together
                networkStream.Read(bytesFrom, 0, (int)a_client.ReceiveBufferSize);

                //Convert to string and trim empty bytes
                dataFromClient = System.Text.Encoding.ASCII.GetString(bytesFrom);
                dataFromClient = dataFromClient.TrimEnd('\0');


                //clean up the incoming stream buffer
                networkStream.Flush();
                message = dataFromClient;

                dataFromClient = null;
                bytesFrom = new byte[10025];

                message = message.Trim();
                message = message.Trim('\n');
                message = message.Trim('\r');

            }

            //Handle exceptions that may occur for some reason
            catch (Exception ex)
            {
                Console.WriteLine(" >> " + ex.ToString());
                message = "ERR:Client has disconnected.";
                a_client.Close();
            }


            return message;
        }

        public void SendClientMessage(TcpClient a_client, StreamWriter a_writer, String a_message)
        {
            a_writer.WriteLine(a_message);
            a_writer.Flush();
        }

        public void SendClientMessage(TcpClient a_client, String a_message)
        {
            Byte[] sendBytes = null;
            NetworkStream netStream = a_client.GetStream();
            sendBytes = Encoding.ASCII.GetBytes(a_message);
            netStream.Write(sendBytes, 0, sendBytes.Length);
            netStream.Flush();
        }

        /// <summary>
        /// Pauses the server for a specified amount of time.
        /// </summary>
        /// <param name="a_seconds">Total Milliseconds to pause</param>
        public void DebugOutputTimer(int a_mseconds)
        {
            int intervalTime = 5000;

            for (int timer = a_mseconds; timer >= 0; timer -= intervalTime)
            {
                Console.WriteLine("5-Second inteveral Console Test.");
                System.Threading.Thread.Sleep(intervalTime);
            }
        }

        public void DebugOutputTimer(int a_mseconds, string a_extra)
        {
            int intervalTime = 5000;

            for (int timer = a_mseconds; timer >= 0; timer -= intervalTime)
            {
                Console.WriteLine("5-Second inteveral Console Test from " + a_extra);
                System.Threading.Thread.Sleep(intervalTime);
            }
        }

        /// <summary>
        /// Connects to the database. Closes application if error.
        /// </summary>
        public void ConnectToDatabase()
        {
            try
            {
                DatabaseConnection = new MySqlConnection(xVariables.MYSQL_LOGIN);
                DatabaseConnection.Open();
            }
            catch (Exception e)
            {
                CleanupServer();
                Console.WriteLine("Error connecting to database.");
                Console.WriteLine(e);
                Environment.Exit(5);
            }
            Console.WriteLine("Connected to database!");

        }
        public void CloseDatabase()
        {
            DatabaseConnection.Close();
        }

        /// <summary>
        /// Closes sockets, clears lists and otherwise clears memory.
        /// </summary>
        public void CleanupServer()
        {
            inLoginSocket.Stop();
        }


    }
}
