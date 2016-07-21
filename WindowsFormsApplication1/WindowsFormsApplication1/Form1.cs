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
        }
        void GetAvalaiblePorts()
        {
            string[] ports = SerialPort.GetPortNames();
            comboBox1.Items.AddRange(ports);

        }
        private void Form1_Load(object sender, EventArgs e)
        {
        }
        delegate void Display(byte[] buffer);
        private Boolean receiving;
        private Thread t;
        private void DisplayText(byte[] buffer)
        {
            textBox2.Clear();
            textBox2.Text += string.Format("{0}{1}", Encoding.ASCII.GetString(buffer), Environment.NewLine);
        }
        private void button1_Click(object sender, EventArgs e)          /*Send Signal Check*/
        {
            byte[] Commend = { 0x01, 0x01, 0x01 };
            //serialPort1.WriteLine(textBox1.Text);
            serialPort1.Write(Commend,0,Commend.Length);
            textBox1.Text = "";
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

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
                if(comboBox1.Text == "" || comboBox2.Text == "")
                {
                    textBox2.Text = "Please select port settings";
                }
                else
                {
                    serialPort1.PortName = comboBox1.Text;
                    serialPort1.BaudRate = Convert.ToInt32(comboBox2.Text);
                    serialPort1.Open();
                    receiving = true;
                    t = new Thread(DoReceive);
                    t.IsBackground = true;
                    t.Start();
                    progressBar1.Value = 100;
                    button1.Enabled = true;
                    textBox1.Enabled = true;
                    button3.Enabled = false;
                    button4.Enabled = true;
                    button5.Enabled = true;
                    button6.Enabled = true;
                }
            }
            catch(UnauthorizedAccessException)
            {
                textBox2.Text = "Unauthorized Access";
            }
        }

        private void button4_Click(object sender, EventArgs e)        /*close port*/
        {
            serialPort1.Close();
            progressBar1.Value = 0;
            button1.Enabled = false;
            textBox1.Enabled = false;
            button3.Enabled = true;
            button4.Enabled = false;
            button5.Enabled = false;
            button6.Enabled = false;
            t.Abort();
        }    

        private void textBox1_TextChanged_1(object sender, EventArgs e)
        {

        }

        
        private void DoReceive()
        {
            try
            {
                Byte[] buffer = new Byte[1024];
                while (receiving)
                {
                    if (serialPort1.BytesToRead > 0)
                    {
                        Int32 length = serialPort1.Read(buffer, 0, buffer.Length);
                        Array.Resize(ref buffer, length);
                        Display d = new Display(DisplayText);
                        this.Invoke(d, new Object[] { buffer });
                        Array.Resize(ref buffer, 1024);
                    }
                    Thread.Sleep(16);
                }
            }
            catch(InvalidOperationException) /*Avoid Derictly Close The Form without close port*/
            {
                t.IsBackground = false;
                t.Abort();
            }
        }

        private void button5_Click(object sender, EventArgs e)       /*Network Join*/
        {
            byte[] Commend = { 0x02, 0x01, 0x01 };
            //serialPort1.WriteLine(textBox1.Text);
            serialPort1.Write(Commend, 0, Commend.Length);
            textBox1.Text = "";
        }

        private void button6_Click(object sender, EventArgs e)       /*Send Data*/
        {
            byte[] Commend = new byte[1024];
            if (radioButton1.Checked) /*read as ASCII*/
            {
                byte[] Data = Encoding.ASCII.GetBytes(textBox1.Text);
                int lens = 0;
                lens += textBox1.TextLength;
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
                byte[] Data = StringToByteArray(textBox1.Text);
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
            textBox1.Text = "";
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
