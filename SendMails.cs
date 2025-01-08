using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Empire_BSA_Import
{
    internal class SendMail
    {
        public void SendErrorMail(string error_Message)
        {
            MailMessage msg = new MailMessage();
            msg.To.Add(new MailAddress("", "SomeOne"));
            msg.From = new MailAddress("", "You");
            msg.Subject = "ERROR_" + GetType().Namespace;
            msg.Body = error_Message;
            msg.IsBodyHtml = true;
            Attachment data = new Attachment(LogManager.Instance.GetFullLogPath());
            msg.Attachments.Add(data);

            SmtpClient client = new SmtpClient();
            client.UseDefaultCredentials = false;
            client.Credentials = new System.Net.NetworkCredential("");
            client.Port = 587; // You can use Port 25 if 587 is blocked 
            client.Host = "smtp.office365.com";
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.EnableSsl = true;

            try
            {
                ////Derrell: Block message send here
                client.Send(msg);
                /*Releasing thread to keep writting on the log*/
                msg.Dispose();
                Console.WriteLine("Message Sent Succesfully");

            }
            catch (Exception)
            {
                /*Releasing thread to keep writting on the log*/
                msg.Dispose();
                Console.WriteLine("ERROR");
            }
        }
    }
}
