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




namespace WindowsFormsApplication1
{

    public partial class Form1 : Form
    {

    
        public Form1()
        {
            InitializeComponent();
            GetAvaliblePorts();
            serialPort1.ReadTimeout = 1000;
            serialPort1.WriteTimeout = 1000;
        }
        void GetAvaliblePorts()
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
                    button2.Enabled = true;
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

        private void button4_Click(object sender, EventArgs e)
        {
            serialPort1.Close();
            progressBar1.Value = 0;
            button1.Enabled = false;
            button2.Enabled = false;
            textBox1.Enabled = false;
            button3.Enabled = true;
            button4.Enabled = false;
            button5.Enabled = false;
            button6.Enabled = false;
            t.IsBackground = false;
            t.Abort();
        }             /*close port*/

        private void textBox1_TextChanged_1(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e) /*Recive*/
        {
            try
            {
                byte[] Responce = new byte[255];
                //textBox2.Text = serialPort1.ReadLine();
                serialPort1.Read(Responce, 0, Responce.Length);
                int i;
                for (i = 0; i <= Responce.Length; i++)
                {
                    if (Responce[i] != 0x00)
                    {
                        textBox2.Text += Convert.ToString(Responce[i]);
                    }
                }
            }
            catch(TimeoutException)
            {
                textBox2.Text = "Timeout Exception";
            }
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

        private void button6_Click(object sender, EventArgs e)
        {
            byte[] Commend = { 0x03, 0x02, 0xAA, 0xFF };
            //serialPort1.WriteLine(textBox1.Text);
            serialPort1.Write(Commend, 0, Commend.Length);
            textBox1.Text = "";
        }
    }
}
