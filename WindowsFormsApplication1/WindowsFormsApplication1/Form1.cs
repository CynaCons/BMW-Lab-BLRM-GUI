using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.Threading;
using System.Globalization;




namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {

    
        public Form1()
        {
            InitializeComponent();
            GetAvalaiblePorts();
            serialPort1.ReadTimeout = 1000;
            serialPort1.WriteTimeout = 1000;
			WindowState = FormWindowState.Maximized;
        }
        void GetAvalaiblePorts()
        {
            string[] ports = SerialPort.GetPortNames();
            comboBox_port.Items.AddRange(ports);

        }
        private void Form1_Load(object sender, EventArgs e)
        {
        }
		delegate void Display(string inData);
        private Boolean receiving;

        private Thread receiveingThread;

		private void DisplayText(string inData)
        {
            //textBox_receive.Clear();
			textBox_receive.AppendText (string.Format("{0}{1}", inData, Environment.NewLine));
        }
        private void button1_Click(object sender, EventArgs e)          /*Send Signal Check*/
        {
            byte[] Commend = { 0x01, 0x01, 0x01 };
            //serialPort1.WriteLine(textBox1.Text);
            serialPort1.Write(Commend,0,Commend.Length);
            textBox_send.Text = "";
        }
			
        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)         /*open port*/
        {
            try
            {
                if(comboBox_port.Text == "" || comboBox_baudRate.Text == "")
                {
                    textBox_receive.Text = "Please select port settings";
                }
                else
                {
                    serialPort1.PortName = comboBox_port.Text;
                    serialPort1.BaudRate = Convert.ToInt32(comboBox_baudRate.Text);
                    serialPort1.Open();
                    receiving = true;

                    receiveingThread = new Thread(DoReceive);
                    receiveingThread.IsBackground = true;
                    receiveingThread.Start();
				

                    progressBar_status.Value = 100;
                    button_signalCheck.Enabled = true;
                    textBox_send.Enabled = true;
                    button_openPort.Enabled = false;
                    button_closePort.Enabled = true;
                    button_networkJoin.Enabled = true;
                    button_sendData.Enabled = true;
                }
            }
            catch(UnauthorizedAccessException)
            {
                textBox_receive.Text = "Unauthorized Access";
            }
        }

        private void button4_Click(object sender, EventArgs e)        /*close port*/
        {
            serialPort1.Close();
            progressBar_status.Value = 0;
            button_signalCheck.Enabled = false;
            textBox_send.Enabled = false;
            button_openPort.Enabled = true;
            button_closePort.Enabled = false;
            button_networkJoin.Enabled = false;
            button_sendData.Enabled = false;
            receiveingThread.Abort();
        }    

        private void textBox_send_TextChanged(object sender, EventArgs e)
        {
			serialPort1.Write (textBox_send.Text);
			textBox_send.Text = "";
        }

	

        private void DoReceive()
        {
            try
            {
                Byte[] buffer = new Byte[1024];
                while (receiving)
                {
					if(serialPort1.BytesToRead > 0){
						string inData =  serialPort1.ReadExisting();
						Display d = new Display(DisplayText);
                        this.Invoke(d, new Object[] { inData });
						//textBox_receive.AppendText(string.Format("{0}{1}",inData,Environment.NewLine));
						//Console.WriteLine(inData);

						//textBox_receive.Text += inData;
					}
//                    if (serialPort1.BytesToRead > 0)
//                    {
//                        Int32 length = serialPort1.Read(buffer, 0, buffer.Length);
//                        Array.Resize(ref buffer, length);
////                        Display d = new Display(DisplayText);
////                        this.Invoke(d, new Object[] { buffer });
//						textBox_receive.AppendText(Encoding.ASCII.GetString(buffer));
//                        Array.Resize(ref buffer, 1024);
//                    }
					Thread.Sleep(50);
                }
			}
            catch(InvalidOperationException) /*Avoid Derictly Close The Form without close port*/
            {
                receiveingThread.IsBackground = false;
                receiveingThread.Abort();
            }
        }


        private void button5_Click(object sender, EventArgs e)       /*Network Join*/
        {
            byte[] Commend = { 0x02, 0x01, 0x01 };
            //serialPort1.WriteLine(textBox1.Text);
            serialPort1.Write(Commend, 0, Commend.Length);
            textBox_send.Text = "";
        }

        private void button6_Click(object sender, EventArgs e)       /*Send Data*/
        {
            byte[] Commend = new byte[1024];
            if (radioButton1.Checked) /*read as ASCII*/
            {
                byte[] Data = Encoding.ASCII.GetBytes(textBox_send.Text);
                int lens = 0;
                lens += textBox_send.TextLength;
                Commend[0] = 0x03;
                Commend[1] = Convert.ToByte(lens);
                Array.Resize(ref Commend, lens + 2);
                int i = 2;
                foreach (byte element in Data)
                {
                    //Console.WriteLine("{0} = {1}", element, (char)element); //check data on console
                    Commend[i] = element;
                    i++;
                }
            }
            if (radioButton2.Checked) /*read as HEX*/
            {
                byte[] Data = StringToByteArray(textBox_send.Text);
                int lens = 0;
                lens += Data.Length;
                if (lens <=128) /*lens more than 128 will be failure array*/
                {
                    Commend[0] = 0x03;
                    Commend[1] = Convert.ToByte(lens);
                    Array.Resize(ref Commend, lens + 2);
                    int i = 2;
                    foreach (byte element in Data)
                    {
                        //Console.WriteLine("{0} = {1}", element, (char)element); //check data on console
                        Commend[i] = element;
                        i++;
                    }
                    serialPort1.Write(Commend, 0, Commend.Length);
                }
            }
            //serialPort1.WriteLine(textBox1.Text);
            textBox_send.Text = "";
        }
        public static byte[] StringToByteArray(String hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            try
            {
                for (int i = 0; i < NumberChars; i += 2)
                    bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
                return bytes;
            }
            catch (System.FormatException)
            {
                MessageBox.Show("Wrong Hex format..");
                byte[] FailArray = new byte[200];
                FailArray[199] = 0xFF;
                return FailArray;
            }
            catch (System.ArgumentOutOfRangeException)
            {
                MessageBox.Show("Wrong Hex length..");
                byte[] FailArray = new byte[200];
                FailArray[199] = 0xFF;
                return FailArray;
            }
        }
    }

}
