using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Threading;
using System.Net;
using System.Net.Sockets;

using System.Net.WebSockets;

namespace myLocalGatewayApplication
{

    public partial class Form1 : Form
    {
        delegate void SetChartCallback(string text);
        WebClient client = null;
        int tx_count = 0;
        int rx_count = 0;
        int crx_count = 0;
        string storedAuthToken = "-1";
        int dataFormat = 0;
        string sensorIP = "";
        string sensorPort = "";
        string gatewayIP = "";
        string thecloudIP = "";
        string thecloudPort = "";
        string authserverIP = "";
        string authserverloginPort = "";


        //IPAddress localAddr = IPAddress.Parse("192.168.137.1");

        IPAddress localAddr = IPAddress.Parse("10.100.93.221");

        public Form1()
        {
            InitializeComponent();
            chart1.ChartAreas[0].AxisY.Maximum = 120;
            chart1.ChartAreas[0].AxisY.Minimum = 0;
        }

        private void AddTempPoint(string text)
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if (this.chart1.InvokeRequired)
            {
                SetChartCallback d = new SetChartCallback(AddTempPoint);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                if (text.Length > 0)
                {
                    this.chart1.Series[0].Points.Add(double.Parse(text));
                }
            }
        }

        private void enableNetworkSettings()
        {
            groupBox2.Enabled = true;
            groupBox3.Enabled = true;
            groupBox4.Enabled = true;
            groupBox6.Enabled = true;
        }

        private void disableNetworkSettings()
        {
            groupBox2.Enabled = false;
            groupBox3.Enabled = false;
            groupBox4.Enabled = false;
            groupBox6.Enabled = false;
        }

        private void setNetworkSetting()
        {
            sensorIP = textBox5.Text;
            sensorPort = textBox8.Text;
            gatewayIP = textBox10.Text;
            thecloudIP = textBox7.Text;
            thecloudPort = textBox9.Text;
            authserverIP = textBox6.Text;
            authserverloginPort = textBox11.Text;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            disableNetworkSettings();
            setNetworkSetting();
            timer1.Start();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            bool lab8 = false;
            bool lab9_JSON = false;
            bool lab9_JWT = false;

            string apidataformat = "/api/temp";
            switch(dataFormat)
            {
                case 0:
                    apidataformat = "/api/temp";
                    break;
                case 1:
                    apidataformat = "/api/temp/json";
                    break;
                case 2:
                    apidataformat = "/api/temp/jwt";
                    break;
            }
            
            // Create web client simulating IE6.
            client = new WebClient();
            client.Headers["User-Agent"] =
                "Mozilla/4.0 (Compatible; Windows NT 5.1; MSIE 6.0)";

            // Download data.
            byte[] arr = { };
            try
            {
                arr = client.DownloadData("http://" + sensorIP + ":" + sensorPort + apidataformat);

                // Write values.
                string resp = Encoding.UTF8.GetString(arr);
                string originalResp = resp;
                string[] parameters = { };

                Console.WriteLine(resp);

                if (resp.Contains("{")) // JSON format
                {
                    Console.WriteLine("Received JSON format: " + resp + " from device.");
                    resp = resp.Replace("{", string.Empty);
                    resp = resp.Replace("}", string.Empty);
                    resp = resp.Replace("\"", string.Empty);
                    parameters = resp.Split(',');
                    lab9_JSON = true;
                }
                else if(resp.IndexOf(".") == resp.LastIndexOf(".")) // Lab 8 format
                {
                    Console.WriteLine("Received lab 8 format: " + resp + " from device.");
                    parameters = resp.Split(',');
                    lab8 = true;
                }
                else // JWT format
                {
                    Console.WriteLine("Received JSON Web Token: " + resp + " from device.");
                    textBox4.Text = resp;
                    string[] jwtparams = resp.Split('.');
                    string s = jwtparams[1];
                    s = s.Replace('-', '+'); // 62nd char of encoding
                    s = s.Replace('_', '/'); // 63rd char of encoding
                    switch (s.Length % 4) // Pad with trailing '='s
                    {
                        case 0: break; // No pad chars in this case
                        case 2: s += "=="; break; // Two pad chars
                        case 3: s += "="; break; // One pad char
                        default:
                            throw new System.Exception(
                     "Illegal base64url string!");
                    }
                    byte[] ds = Convert.FromBase64String(s);
                    string decodedString = Encoding.UTF8.GetString(ds);
                    decodedString = decodedString.Replace("{", string.Empty);
                    decodedString = decodedString.Replace("}", string.Empty);
                    decodedString = decodedString.Replace("\"", string.Empty);
                    parameters = decodedString.Split(',');
                    lab9_JWT = true;
                }

                if (lab8 || lab9_JSON || lab9_JWT)
                {
                    string authtoken = "";
                    string tempID = "";
                    string temp = "";
                    string status = "";
                    string username = "";
                    string password = "";
                    foreach (string parameter in parameters)
                    {
                        string[] keypair = parameter.Split(':');
                        if (keypair[0].Equals("authtoken"))
                        {
                            authtoken = keypair[1];
                        }
                        else if (keypair[0].Equals("tempID"))
                        {
                            tempID = keypair[1];
                        }
                        else if (keypair[0].Equals("temp"))
                        {
                            temp = keypair[1];
                        }
                        else if (keypair[0].Equals("status"))
                        {
                            status = keypair[1];
                        }
                        else if (keypair[0].Equals("username"))
                        {
                            username = keypair[1];
                        }
                        else if (keypair[0].Equals("password"))
                        {
                            password = keypair[1];
                        }
                    }
                    if(lab8)
                    {
                        rx_count++;
                        rx.Text = "Rx from device: " + rx_count.ToString();
                        this.AddTempPoint(temp);
                        label1.Text = "Heat: " + status;

                        if (status.Equals("ON"))
                        {
                            label1.BackColor = Color.Red;
                            label1.ForeColor = Color.Black;
                        }
                        else
                        {
                            label1.BackColor = Color.LightBlue;
                            label1.ForeColor = Color.White;
                        }

                        client.Headers["User-Agent"] = "Mozilla/4.0 (Compatible; Windows NT 5.1; MSIE 6.0)";
                        byte[] arr2 = { };
                        try
                        { 
                            arr2 = client.DownloadData("http://"+ thecloudIP + ":"+ thecloudPort + "/thecloud/sensor?authtoken=-1&tempID=" + tempID + "&temp=" + temp + "&status=" + status);
                        }
                        catch (WebException ee)
                        {
                            Console.WriteLine(ee.ToString());
                            textBox2.Text = "WebException: {0}" + ee.ToString();
                            timer1.Stop();
                            enableNetworkSettings();
                            return;
                        }
                        tx_count++;
                        tx.Text = "Attempted to send to TheCloud!: " + tx_count.ToString();

                        string thecloudresp = Encoding.UTF8.GetString(arr2);
                        textBox1.Text = thecloudresp;
                                              
                        if(thecloudresp.CompareTo("OK")==0)
                        {
                            crx_count++;
                            crx.Text = "TheCloud! Received: " + crx_count.ToString();
                        }
                    }
                    else if(lab9_JSON || lab9_JWT)
                    {
                        if (storedAuthToken.CompareTo("-1") == 0)
                        {
                            // Get authToken
                            try
                            {
                                Int32 authserverport = int.Parse(authserverloginPort);
                                TcpClient loginclient = new TcpClient(authserverIP, authserverport);
                                string message = username + "," + password + "," + temp;
                                Byte[] data = System.Text.Encoding.ASCII.GetBytes(message);
                                NetworkStream stream = loginclient.GetStream();
                                stream.Write(data, 0, data.Length);
                                Console.WriteLine("Sent: {0} to authentication server", message);
                                data = new Byte[256];
                                String responseData = String.Empty;
                                Int32 bytes = stream.Read(data, 0, data.Length);
                                responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                                textBox3.Text = responseData;
                                if (responseData.CompareTo("INVALID CREDENTIALS")==0)
                                {
                                    Console.WriteLine("FROM AUTH SERVER: INVALID CREDENTIALS");
                                    authserv.Text = "Could not verify sensor credentials. Check username and password. Is token invalid?";
                                }
                                else
                                {
                                    authserv.Text = "Login Success! Token received: " + responseData;
                                    Console.WriteLine("Received authToken: {0}", responseData);
                                }
                                stream.Close();
                                loginclient.Close();

                                client.Headers["User-Agent"] = "Mozilla/4.0 (Compatible; Windows NT 5.1; MSIE 6.0)";
                                byte[] arr2 = { };
                                try
                                {
                                    arr2 = client.DownloadData("http://" + sensorIP + ":" + sensorPort + "/api/temp/postToken?atoken=" + responseData);
                                }
                                catch (WebException ee)
                                {
                                    Console.WriteLine(ee.ToString());
                                    textBox2.Text = "WebException: {0}" + ee.ToString();
                                    timer1.Stop();
                                    enableNetworkSettings();
                                    return;
                                }
                                string thedevicetokensentresp = Encoding.UTF8.GetString(arr2);
                                if(thedevicetokensentresp.CompareTo("OK")==0)
                                {
                                    storedAuthToken = responseData;
                                    Console.WriteLine("Sensor Received authToken: {0}", storedAuthToken);
                                }

                            }
                            catch (ArgumentNullException ee)
                            {
                                Console.WriteLine("ArgumentNullException: {0}", ee);
                                textBox2.Text = "ArgumentNullException: {0}" + ee.ToString();
                                timer1.Stop();
                                enableNetworkSettings();
                                return;
                            }
                            catch (SocketException ee)
                            {
                                Console.WriteLine("SocketException: {0}", ee);
                                textBox2.Text = "SocketException: {0}" + ee.ToString();
                                timer1.Stop();
                                enableNetworkSettings();
                                return;
                            }
                        }
                        else if (authtoken.CompareTo(storedAuthToken) == 0)
                        {
                            authserv.Text = "Sensor: " + tempID + ", is registered and using authtoken: " + storedAuthToken;
                            rx_count++;
                            rx.Text = "Rx from device: " + rx_count.ToString();
                            this.AddTempPoint(temp);

                            label1.Text = "Heat: " + status;

                            if (status.Equals("ON"))
                            {
                                label1.BackColor = Color.Red;
                                label1.ForeColor = Color.Black;
                            }
                            else
                            {
                                label1.BackColor = Color.LightBlue;
                                label1.ForeColor = Color.White;
                            }

                            
                            client.Headers["User-Agent"] = "Mozilla/4.0 (Compatible; Windows NT 5.1; MSIE 6.0)";
                            byte[] arr2 = { };
                            try
                            {
                                arr2 = client.DownloadData("http://"+ thecloudIP + ":"+ thecloudPort + "/thecloud/sensor?message=" + originalResp);
                            }
                            catch(WebException ee)
                            {
                                Console.WriteLine(ee.ToString());
                                textBox2.Text = "WebException: {0}" + ee.ToString();
                                timer1.Stop();
                                enableNetworkSettings();
                                return;
                            }
                            
                            tx_count++;
                            tx.Text = "Attempted to send to TheCloud!: " + tx_count.ToString();

                            Console.WriteLine(Encoding.UTF8.GetString(arr2));
                            textBox1.Text = Encoding.UTF8.GetString(arr2);

                            string thecloudresp = Encoding.UTF8.GetString(arr2);
                            textBox1.Text = thecloudresp;

                            if (thecloudresp.CompareTo("OK") == 0)
                            {
                                crx_count++;
                                crx.Text = "TheCloud! Received: " + crx_count.ToString();
                            }
                        }
                        else
                        {
                            Console.WriteLine("DEVICE IS NOT REGISTERED.");
                            authserv.Text = "Sensor is not regisered!";
                        }
                    }
                    else
                    {
                        Console.WriteLine("Why are we here?");
                    }
                }
                else
                {
                    Console.WriteLine("UNKNOWN FORMAT RECEIVED: " + resp);
                }
            }
            catch (WebException ee)
            {
                Console.WriteLine(ee.ToString());
                textBox2.Text = "WebException: {0}" + ee.ToString();
                timer1.Stop();
                enableNetworkSettings();
                return;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            timer1.Stop();
            enableNetworkSettings();
            client.Dispose();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            // Create web client simulating IE6.
            using (WebClient client = new WebClient())
            {
                client.Headers["User-Agent"] =
                    "Mozilla/4.0 (Compatible; Windows NT 5.1; MSIE 6.0)";

                // Download data.
                byte[] arr = { };
                try
                {
                    arr = client.DownloadData("http://" + sensorIP + ":" + sensorPort + "/api/temp/resetToken");
                }
                catch (WebException ee)
                {
                    Console.WriteLine(ee.ToString());
                    textBox2.Text = "WebException: {0}" + ee.ToString();
                    timer1.Stop();
                    enableNetworkSettings();
                    return;
                }

            // Write values.
            string resp = Encoding.UTF8.GetString(arr);
                Console.WriteLine(resp);
                if (resp.Equals("OK"))
                {
                    storedAuthToken = "-1";
                    authserv.Text = "Token is reset.";
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            textBox1.Text = "";
            textBox2.Text = "";
            textBox3.Text = "";
            textBox4.Text = "";
        }

        private void lab8radio_Click(object sender, EventArgs e)
        {
            dataFormat = 0;
        }

        private void jsonradio_Click(object sender, EventArgs e)
        {
            dataFormat = 1;
        }

        private void jwtradio_Click(object sender, EventArgs e)
        {
            dataFormat = 2;
        }

        private void textBox9_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox5_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
