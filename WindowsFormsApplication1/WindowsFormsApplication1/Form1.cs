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
		private static Boolean keepReceiving;

		private static Boolean keepAnalysing;

		private Thread receivingThread;

		private Thread stringAnalysingThread;

		delegate void Delegate(string inData);

		delegate void UpdateDelegate(string newValue);

		private static Object myMutex = new Object();

		private static string synchronizedBuffer = String.Empty;
    
        public Form1()
        {
            InitializeComponent();
            GetAvalaiblePorts();
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
      

		private void DisplayText(string inData)
        {
			textBox_receive.AppendText(inData);
        }


		private void UpdateTimerPeriod (string newValue){
			label_timerPeriodValue.Text = newValue;
		}


		private void UpdateAutoModeStatus (string newValue){
			label_automodeStatusValue.Text = newValue;
		}

		private void UpdateNetworkStatus(string newStatus){
			label_networkJoinValue.Text = newStatus;
		}

		private void UpdateSignalStatus(string newStatus){
			label_rfSignalCheckValue.Text = newStatus;
		}


        private void button_signalCheck_Click(object sender, EventArgs e)          /*Send Signal Check*/
        {
            byte[] Commend = { 0x01, 0x01, 0x01 };
            serialPort1.Write(Commend,0,Commend.Length);
            textBox_send.Text = "";
        }
			
        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button_openPort_Click(object sender, EventArgs e)         /*open port*/
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

                    receivingThread = new Thread(receivingLoop);
                    receivingThread.IsBackground = true;
                    receivingThread.Start();
					keepReceiving = true;

					stringAnalysingThread = new Thread(stringAnalysingLoop);
					stringAnalysingThread.IsBackground = true;
					stringAnalysingThread.Start();
					keepAnalysing = true;
				

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

        private void button_closePort_Click(object sender, EventArgs e)        /*close port*/
        {
            serialPort1.Close();
			keepReceiving = false;
			keepAnalysing = false;


            progressBar_status.Value = 0;
            button_signalCheck.Enabled = false;
            textBox_send.Enabled = false;
            button_openPort.Enabled = true;
            button_closePort.Enabled = false;
            button_networkJoin.Enabled = false;
            button_sendData.Enabled = false;
        }    


        private void textBox_send_TextChanged(object sender, EventArgs e)
        {
			serialPort1.Write (textBox_send.Text);
			textBox_send.Text = "";
        }

	

        private void receivingLoop()
        {
            try
            {
				while (keepReceiving)
                {
					if(serialPort1.BytesToRead > 0){
						string inData =  serialPort1.ReadExisting();
						Delegate d = new Delegate(DisplayText);
						this.Invoke(d, inData);
						lock(myMutex){
							synchronizedBuffer += inData;
						}
					}
					Thread.Sleep(25);
                }
			}
            catch(InvalidOperationException) /*Avoid Derictly Close The Form without close port*/
            {
                receivingThread.IsBackground = false;
                receivingThread.Abort();
            }
        }


		private string[] referenceStrings = {
			"Current timer period value :",
			"AutoMode Started !",
			"AutoMode is ON",
			"AutoMode is OFF",
			"AutoMode Stopped",
			"RF signal check answer was received",
			"Network Join Failed",
			"Network Join Succes"
		};


			
		private void stringAnalysingLoop(){
			Delegate d;
			string tmp;
			while (keepAnalysing) {
				lock (myMutex) {
					for(Int32 i = 0; i< referenceStrings.Length; i+=1) {
						string currentReferenceString = referenceStrings[i];
						if (synchronizedBuffer.Contains (currentReferenceString)) {
							Int32 subStringIndex = synchronizedBuffer.IndexOf (currentReferenceString);
							subStringIndex += currentReferenceString.Length;
							switch (i) {
							case 0: //Current timer period value :
								d = new Delegate (UpdateTimerPeriod);
								tmp = String.Format ("{0} seconds", synchronizedBuffer.Substring (subStringIndex));
								this.Invoke (d, tmp);
								break;
							case 1: //AutoMode Started !
								d = new Delegate (UpdateAutoModeStatus);
								tmp = "ON";
								this.Invoke (d, tmp);
								break;
							case 2: //AutoMode ON
								d = new Delegate (UpdateAutoModeStatus);
								tmp = "OFF";
								this.Invoke (d, tmp);
								break;
							case 3: //AutoMode OFF
								d = new Delegate (UpdateAutoModeStatus);
								tmp = "OFF";
								this.Invoke (d, tmp);
								break;
							case 4: //AutoMode Stopped
								d = new Delegate (UpdateAutoModeStatus);
								tmp = "OFF";
								this.Invoke (d, tmp);
								break;	
							case 5: //RF signal check answer was received
								d = new Delegate (UpdateSignalStatus);
								tmp = String.Format ("{0} seconds", synchronizedBuffer.Substring (subStringIndex));
								this.Invoke (d, tmp);
								break;
							case 6: //Network Join Failed
								d = new Delegate (UpdateNetworkStatus);
								tmp = "Disconnected";
								this.Invoke (d, tmp);
								break;
							case 7: //Network Join Succes
								d = new Delegate (UpdateNetworkStatus);
								tmp = "Connected";
								this.Invoke (d, tmp);
								break;
							}
							break;
						}
					}
					synchronizedBuffer = "";
				}
				Thread.Sleep (50);
			}
		}


        private void button_networkJoin_Click(object sender, EventArgs e)       /*Network Join*/
        {
            byte[] Commend = { 0x02, 0x01, 0x01 };
            //serialPort1.WriteLine(textBox1.Text);
            serialPort1.Write(Commend, 0, Commend.Length);
            textBox_send.Text = "";
        }

        private void button_sendData_Click(object sender, EventArgs e)       /*Send Data*/
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
