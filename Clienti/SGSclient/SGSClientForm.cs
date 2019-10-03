using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.IO;


namespace SGSclient
{
    enum Command
    {
        Login,      
        Logout,     
        Message,    
        List,       
        Null        
    }

    public partial class Klienti : Form
    {
        public Socket clientSocketa; 
        public string strEmri;      
        public EndPoint epServer;   

        byte []byteData = new byte[1024];

        public Klienti()
        {
            InitializeComponent();
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            try
            {		
               
                Data msgToSend = new Data();
             
                msgToSend.strName = strEmri;
                Console.WriteLine(txtMessage.Text);
                msgToSend.strMessage = myRSA.Encrypt(txtMessage.Text);
                msgToSend.cmdCommand = Command.Message;

                
                byte[] byteData = msgToSend.ToByte();
                
                clientSocketa.BeginSendTo (byteData, 0, byteData.Length, SocketFlags.None, epServer, new AsyncCallback(OnSend), null);

                txtMessage.Text = null;
            }
            catch (Exception)
            {
                MessageBox.Show("Mesazhi nuk u dergua");
            }  
        }
        
        private void OnSend(IAsyncResult ar)
        {
            try
            {
                clientSocketa.EndSend(ar);
            }
            catch (ObjectDisposedException)
            { }
            catch (Exception ex)
            {
                MessageBox.Show("Ka ndodhur nje gabim:" + ex);
            }
        }

        private void OnReceive(IAsyncResult ar)
        {            
            try
            {                
                clientSocketa.EndReceive(ar);

                Data msgReceived = new Data(byteData);



                if (msgReceived.strMessage != null && msgReceived.cmdCommand != Command.List)
                    txtChatBox.Text += msgReceived.strMessage + "\r\n";

                byteData = new byte[1024];                

                //Start listening to receive more data from the user
                clientSocketa.BeginReceiveFrom(byteData, 0, byteData.Length, SocketFlags.None, ref epServer,
                                           new AsyncCallback(OnReceive), null);
            }
            catch (ObjectDisposedException)
            { }
            catch (Exception ex)
            {
                MessageBox.Show("Ka ndodhur nje gabim:"+ex);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
	    CheckForIllegalCrossThreadCalls = false;

            this.Text = "Klienti: " + strEmri;
            
            //The user has logged into the system so we now request the server to send
            //the names of all users who are in the chat room
            Data msgToSend = new Data ();
            msgToSend.cmdCommand = Command.List;
            msgToSend.strName = strEmri;
            msgToSend.strMessage = null;

            byteData = msgToSend.ToByte();

            clientSocketa.BeginSendTo(byteData, 0, byteData.Length, SocketFlags.None, epServer, 
                new AsyncCallback(OnSend), null);

            byteData = new byte[1024];
            //Start listening to the data asynchronously
            clientSocketa.BeginReceiveFrom (byteData,
                                       0, byteData.Length,
                                       SocketFlags.None,
                                       ref epServer,
                                       new AsyncCallback(OnReceive),
                                       null);
        }

        private void txtMessage_TextChanged(object sender, EventArgs e)
        {
            if (txtMessage.Text.Length == 0)
                btnSend.Enabled = false;
            else
                btnSend.Enabled = true;
        }

      

        

        private void txtMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btnSend_Click(sender, null);
            }
        }
    }
    
    class Data
    {
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

class myRSA
{

    public static string Encrypt(string plain)
    {
        byte[] bytesIn = Encoding.UTF8.GetBytes(plain);
        RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
        StreamReader StRe = new StreamReader("D:\\PjesaVetemPublike.xml");
        string VetemPublikeXML = StRe.ReadToEnd();
        rsa.FromXmlString(VetemPublikeXML);
        StRe.Close();
        bytesIn = rsa.Encrypt(bytesIn, true);

        return Convert.ToBase64String(bytesIn);
    }

}