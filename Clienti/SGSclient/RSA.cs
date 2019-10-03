using System;

public class myRSA
{
    public myRSA() { }

     private void Enkripto(string teksti)
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();

            StreamReader StRe = new StreamReader("D:\\PjesaVetemPublike.xml");
            string VetemPublikeXML = StRe.ReadToEnd();
            rsa.FromXmlString(VetemPublikeXML);
            StRe.Close();

            UTF8Encoding enk = new UTF8Encoding();
            data = enk.GetBytes(teksti);

            data = rsa.Encrypt(data, false);

            txtTekstiKriptuar.Text = Encoding.UTF8.GetString(data).ToString();

            txtTekstiKriptuar.Enabled = true;

            btnKripto.Enabled = false;
            btnDekripto.Enabled = true;
            dekriptoToolStripMenuItem.Enabled = true;
        }
	
}
