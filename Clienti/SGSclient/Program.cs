using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SGSclient
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);            

            LoginForm loginForm = new LoginForm();

            Application.Run(loginForm);
            if (loginForm.DialogResult == DialogResult.OK)
            {
                Klienti sgsClientForm = new Klienti();
                sgsClientForm.clientSocketa = loginForm.clientSocket;
                sgsClientForm.strEmri = loginForm.strName;
                sgsClientForm.epServer = loginForm.epServer;

                sgsClientForm.ShowDialog();
            }

        }
    }
}