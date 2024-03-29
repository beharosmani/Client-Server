using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.IO;

namespace Server
{
    //The commands for interaction between the server and the client
    enum Command
    {
        Login,      //Log into the server
        Logout,     //Logout of the server
        Message,    //Send a text message to all the chat clients
        List,       //Get a list of users in the chat room from the server
        Null        //No command
    }

    public partial class SGSserverForm : Form
    {
        //The ClientInfo structure holds the required information about every
        //client connected to the server
        struct gg ClientInfo
        {
            public EndPoint endpoint;   //Socket of the client
            public string strName;      //Name by which the user logged into the chat room
        }

        //The collection of all clients logged into the room (an array of type ClientInfo)
        ArrayList clientList;

        //The main socket on which the server listens to the clients
        Socket serverSocket;

        byte[] byteData = new byte[1024];

        public SGSserverForm()
        {
            clientList = new ArrayList();
            InitializeComponent();
        }

    private void Form1_Load(object sender, EventArgs e)
    {            
        try
        {
	    CheckForIllegalCrossThreadCalls = false;

            //We are using UDP sockets
            serverSocket = new Socket(AddressFamily.InterNetwork, 
                SocketType.Dgram, ProtocolType.Udp);

            //Assign the any IP of the machine and listen on port number 1000
            IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Any, 1000);

            //Bind this address to the server
            serverSocket.Bind(ipEndPoint);
            
            IPEndPoint ipeSender = new IPEndPoint(IPAddress.Any, 0);
            //The epSender identifies the incoming clients
            EndPoint epSender = (EndPoint) ipeSender;

            //Start receiving data
            serverSocket.BeginReceiveFrom (byteData, 0, byteData.Length, 
                SocketFlags.None, ref epSender, new AsyncCallback(OnReceive), epSender);                
        }
        catch (Exception ex) 
        { 
            MessageBox.Show(ex.Message, "SGSServerUDP", 
                MessageBoxButtons.OK, MessageBoxIcon.Error); 
        }            
    }

        private void OnReceive(IAsyncResult ar)
        {
            try
            {
                IPEndPoint ipeSender = new IPEndPoint(IPAddress.Any, 0);
                EndPoint epSender = (EndPoint)ipeSender;

                serverSocket.EndReceiveFrom (ar, ref epSender);
                
                //Transform the array of bytes received from the user into an
                //intelligent form of object Data
                Data msgReceived = new Data(byteData);

                //We will send this object in response the users request
                Data msgToSend = new Data();

                byte [] message;
                //If the message is to login, logout, or simple text message
                //then when send to others the type of the message remains the same
                msgToSend.cmdCommand = msgReceived.cmdCommand;
                msgToSend.strName = msgReceived.strName;
               
                switch (msgReceived.cmdCommand)
                {
                    case Command.Login:
                        
                        //When a user logs in to the server then we add her to our
                        //list of clients

                        ClientInfo clientInfo = new ClientInfo();
                        clientInfo.endpoint = epSender;      
                        clientInfo.strName = msgReceived.strName;                        

                        clientList.Add(clientInfo);

                        break;

                    case Command.Logout:                    
                        
                        //When a user wants to log out of the server then we search for her 
                        //in the list of clients and close the corresponding connection

                        int nIndex = 0;
                        foreach (ClientInfo client in clientList)
                        {
                            if (client.endpoint == epSender)
                            {
                                clientList.RemoveAt(nIndex);
                                break;
                            }
                            ++nIndex;
                        }

                        break;

                    case Command.Message:
                        //myRsa enk = new myRsa();
                        //Console.WriteLine(enk.Dekripto(msgReceived.strMessage));
                        //Set the text of the message that we will broadcast to all users
                        Console.WriteLine(msgReceived.strMessage);
                        msgToSend.strMessage = msgReceived.strName + ": " + myRsa.Decrypt(msgReceived.strMessage);
                        break;

                   
                        
                }

                if (msgToSend.cmdCommand != Command.List)   //List messages are not broadcasted
                {
                    txtLog.Text += msgToSend.strMessage + "\r\n";
                   
                    message = msgToSend.ToByte();

                  
                    foreach (ClientInfo clientInfo in clientList)
                    {
                        if (clientInfo.endpoint != epSender ||
                            msgToSend.cmdCommand != Command.Login)
                        {
                            //Send the message to all users
                            serverSocket.BeginSendTo(message, 0, message.Length, SocketFlags.None, clientInfo.endpoint,
                                new AsyncCallback(OnSend), clientInfo.endpoint);
                        }
                    }
                }

                //If the user is logging out then we need not listen from her
                if (msgReceived.cmdCommand != Command.Logout)
                {
                    //Start listening to the message send by the user
                    serverSocket.BeginReceiveFrom (byteData, 0, byteData.Length, SocketFlags.None, ref epSender, 
                        new AsyncCallback(OnReceive), epSender);
                }
            }
            catch (Exception ex)
            { 
                MessageBox.Show(ex.Message, "SGSServerUDP", MessageBoxButtons.OK, MessageBoxIcon.Error); 
            }
        }

        public void OnSend(IAsyncResult ar)
        {
            try
            {                
                serverSocket.EndSend(ar);
            }
            catch (Exception ex)
            { 
                MessageBox.Show(ex.Message, "SGSServerUDP", MessageBoxButtons.OK, MessageBoxIcon.Error); 
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            
        }

        private void button3_Click(object sender, EventArgs e)
        {
            myRsa.GjeneroRSAParametrat();
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            
            StreamReader StRe = new StreamReader("D:\\PjesaPublikeDhePrivate.xml");
            string PublikeDhePrivate = StRe.ReadToEnd();
            MessageBox.Show(PublikeDhePrivate, "Celesat e perdorur per enkriptim dhe dekriptim");
            StRe.Close();
        }
    }

    //The data structure by which the server and the client interact with 
    //each other
    class Data
    {
        //Default constructor
        public Data()
        {
            this.cmdCommand = Command.Null;
            this.strMessage = null;
            this.strName = null;
        }

        //Converts the bytes into an object of type Data
        public Data(byte[] data)
        {
            //The first four bytes are for the Command
            this.cmdCommand = (Command)BitConverter.ToInt32(data, 0);

            //The next four store the length of the name
            int nameLen = BitConverter.ToInt32(data, 4);

            //The next four store the length of the message
            int msgLen = BitConverter.ToInt32(data, 8);

            //This check makes sure that strName has been passed in the array of bytes
            if (nameLen > 0)
                this.strName = Encoding.UTF8.GetString(data, 12, nameLen);
            else
                this.strName = null;

            //This checks for a null message field
            if (msgLen > 0)
                this.strMessage = Encoding.UTF8.GetString(data, 12 + nameLen, msgLen);
            else
                this.strMessage = null;
        }

        //Converts the Data structure into an array of bytes
        public byte[] ToByte()
        {
            List<byte> result = new List<byte>();

            //First four are for the Command
            result.AddRange(BitConverter.GetBytes((int)cmdCommand));

            //Add the length of the name
            if (strName != null)
                result.AddRange(BitConverter.GetBytes(strName.Length));
            else
                result.AddRange(BitConverter.GetBytes(0));

            //Length of the message
            if (strMessage != null)
                result.AddRange(BitConverter.GetBytes(strMessage.Length));
            else
                result.AddRange(BitConverter.GetBytes(0));

            //Add the name
            if (strName != null)
                result.AddRange(Encoding.UTF8.GetBytes(strName));

            //And, lastly we add the message text to our array of bytes
            if (strMessage != null)
                result.AddRange(Encoding.UTF8.GetBytes(strMessage));

            return result.ToArray();
        }

        public string strName;      //Name by which the client logs into the room
        public string strMessage;   //Message text
        public Command cmdCommand;  //Command type (login, logout, send message, etcetera)
    }
}

class myRsa{

    public static void GjeneroRSAParametrat()
    {
        RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
        StreamWriter StWr = new StreamWriter("D:\\PjesaPublikeDhePrivate.xml");
        string PublikeDhePrivateXML = rsa.ToXmlString(true);
        StWr.Write(PublikeDhePrivateXML);
        Console.WriteLine(PublikeDhePrivateXML);
        StWr.Close();

        StWr = new StreamWriter("D:\\PjesaVetemPublike.xml");
        string VetemPublikeXML = rsa.ToXmlString(false);
        StWr.Write(VetemPublikeXML);
        StWr.Close();
    
    }

    public static string Decrypt(string cipher)
    {
        byte[] bytesIn = Convert.FromBase64String(cipher);

        RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
        StreamReader StRe = new StreamReader("D:\\PjesaPublikeDhePrivate.xml");
        string PublikeDhePrivate = StRe.ReadToEnd();
        rsa.FromXmlString(PublikeDhePrivate);
        StRe.Close();
        bytesIn = rsa.Decrypt(bytesIn, true);

        return Encoding.UTF8.GetString(bytesIn);
    }

    public static string Encrypt(string plain)
    {
        byte[] bytesIn = Encoding.UTF8.GetBytes(plain);
        RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
        StreamReader StRe = new StreamReader("D:\\PjesaPublikeDhePrivate.xml");
        string PublikeDhePrivate = StRe.ReadToEnd();
        rsa.FromXmlString(PublikeDhePrivate);
        StRe.Close();
        bytesIn = rsa.Encrypt(bytesIn, true);

        return Convert.ToBase64String(bytesIn);
    }

 }
 
