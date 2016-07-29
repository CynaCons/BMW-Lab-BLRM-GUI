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
using System.Diagnostics;




namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
		private static Boolean keepReceiving;

		private static Boolean keepAnalysing;

		private static Boolean keepUpdatingProgressBar;

		private static Boolean automodeStatus;

		private Thread updatingProgressBarThread;

		private Thread receivingThread;

		private Thread stringAnalysingThread;

		delegate void Delegate(string inData);

		delegate void UpdateDelegate(string newValue);

		private static Object myMutex = new Object();

		private static string synchronizedBuffer = String.Empty;

		private Int32 timerPeriodValue = 0;

		private Stopwatch stopwatch = Stopwatch.StartNew();

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

		/**
		 *  DELEGATES FUNCTIONS
		 */
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

		private void UpdateLastData(string newData){
			label_lastDataValue.Text = newData;
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
				
					updatingProgressBarThread = new Thread(updatingProgressBarLoop);
					updatingProgressBarThread.IsBackground = true;
					updatingProgressBarThread.Start();
					keepUpdatingProgressBar = true;

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
			keepUpdatingProgressBar = false;

		
            progressBar_status.Value = 0;
            button_signalCheck.Enabled = false;
            textBox_send.Enabled = false;
            button_openPort.Enabled = true;
            button_closePort.Enabled = false;
            button_networkJoin.Enabled = false;
            button_sendData.Enabled = false;
        }    


		/**
		 * Clean send textBox after each character
		 */
        private void textBox_send_TextChanged(object sender, EventArgs e)
        {
			serialPort1.Write (textBox_send.Text);
			textBox_send.Text = "";
        }
			

		/**
		 * Character reception via UART
		 */
        private void receivingLoop()
        {
            try
            {
				while (keepReceiving)
                {
					if(serialPort1.BytesToRead > 0){
						string inData =  serialPort1.ReadExisting().Replace("\0",String.Empty);
						Delegate d = new Delegate(DisplayText);
						this.Invoke(d, inData);
						lock(myMutex){
							synchronizedBuffer += inData;
						}
					}
					Thread.Sleep(50);
                }
			}
            catch(InvalidOperationException) /*Avoid Derictly Close The Form without close port*/
            {
                receivingThread.IsBackground = false;
                receivingThread.Abort();
            }
        }


		/**
		 * Reference strings. When one is received from the UART, some action will be done
		 */
		private string[] referenceStrings = {
			"Current timer period value :",
			"AutoMode Started !",
			"AutoMode is ON",
			"AutoMode is OFF",
			"AutoMode Stopped",
			"RSSI :",
			"Network Join failed",
			"Network Join succes",
			"New timer period value :",
			"Data Transfer succes",
			"The following data was just sent :",
			"Data Transfer failed"
		};
			

		/**
		 * Received characters are analysed by a special thread.
		 * Input buffer is compared to reference strings and actions are performed when a string is recognized
		 */
		private void stringAnalysingLoop(){
			Delegate d;
			string tmp;
			string rxBuffer;
			while (keepAnalysing) {
				lock (myMutex) {
					rxBuffer = (string)synchronizedBuffer.Clone ();
					synchronizedBuffer = "";
				}
				for(Int32 i = 0; i< referenceStrings.Length; i+=1) {
					string currentReferenceString = referenceStrings[i];
					if (rxBuffer.Contains (currentReferenceString)) {
						tmp = "";
						Int32 subStringIndex = rxBuffer.IndexOf (currentReferenceString);
						subStringIndex += currentReferenceString.Length;
						switch (i) {
						case 0: //Current timer period value :
							d = new Delegate (UpdateTimerPeriod);
							timerPeriodValue = Int32.Parse (rxBuffer.Substring (subStringIndex));
							Stopwatch.StartNew ();
							tmp = timerPeriodValue.ToString ();
							tmp += " seconds";
							this.Invoke (d, tmp);
							break;
						case 1: //AutoMode Started !
							d = new Delegate (UpdateAutoModeStatus);
							tmp = "ON";
							this.Invoke (d, tmp);
							automodeStatus = true;
							break;
						case 2: //AutoMode ON
							d = new Delegate (UpdateAutoModeStatus);
							Console.WriteLine ("Recognized automode on");
							tmp = "ON";
							this.Invoke (d, tmp);
							automodeStatus = true;
							stopwatch.Reset ();

							break;
						case 3: //AutoMode OFF
							d = new Delegate (UpdateAutoModeStatus);
							tmp = "OFF";
							this.Invoke (d, tmp);
							automodeStatus = false;
							break;
						case 4: //AutoMode Stopped
							d = new Delegate (UpdateAutoModeStatus);
							tmp = "OFF";
							this.Invoke (d, tmp);
							automodeStatus = false;
							break;	
						case 5: //RF signal check answer was received
							d = new Delegate (UpdateSignalStatus);
							tmp = String.Format ("RSSI: {0}", rxBuffer.Substring (subStringIndex));
							this.Invoke (d, tmp);
							break;
						case 11:
						case 6: //Network Join Failed
							d = new Delegate (UpdateNetworkStatus);
							tmp = "Disconnected";
							this.Invoke (d, tmp);
							break;
						case 7: //Network Join Succes
							//TODO DO REAL SWITCH CASE
							d = new Delegate (UpdateNetworkStatus);
							tmp = "Connected";
							this.Invoke (d, tmp);
							break;
						case 8: //New timer period value :
							d = new Delegate (UpdateTimerPeriod);
							tmp = rxBuffer.Substring (subStringIndex).Split (new string[]{ System.Environment.NewLine }, StringSplitOptions.None).ElementAt (0);
							timerPeriodValue = Int32.Parse (tmp);
							tmp += " seconds";
							this.Invoke (d, tmp);
							break;
						case 9: //Data Transfer succes
							d = new Delegate (UpdateNetworkStatus);
							tmp = "Connected";
							this.Invoke (d, tmp);
							break;
						case 10: //The following data was just sent
							d = new Delegate (UpdateLastData);
							tmp = rxBuffer.Substring (subStringIndex);
							this.Invoke (d, tmp);
							break;
						}
						rxBuffer = rxBuffer.Substring (subStringIndex);
					}
				}
				Thread.Sleep (50);
			}
		}


		/**
		 * Update progress bar to indicate when the next data tranfer will occur
		 */
		private void updatingProgressBarLoop(){
			while (keepUpdatingProgressBar) {
				if (automodeStatus == true) {
					if (stopwatch.IsRunning == false) {
						stopwatch.Start ();
					}
					int timerBarValue = (int)(Math.Round ((stopwatch.Elapsed.TotalSeconds / timerPeriodValue) * 100));
					if (timerPeriodValue != 0 && timerBarValue < 100) {
						progressBar_timerPeriod.Value = timerBarValue; 
					}
				} else {
					progressBar_timerPeriod.Value = 0;
						if(stopwatch.IsRunning == true){
						stopwatch.Stop();
					}
				}
				Thread.Sleep (250);
			}
		}
    }
}
