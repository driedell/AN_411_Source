﻿#define FT232H                // Enable only one of these defines depending on your device type
//#define FT2232H
//#define FT4232H

//###################################################################################################################################
// This code is provided as an example only and is not supported or guaranteed by FTDI
// It is the responsibility of the designer of the system incorporating any part of this code to ensure correct
// and safe operation of their overall system. By using this code, you agree that FTDI and its employees accept 
// no responsibility whatsoever for any consequences resulting from the use of this code. 

// Revision History
// Version  Date    Author  Comments
// ======== ======= ======= ==============================
// 1.0      Feb2017 G Brown  Initial release



using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FTD2XX_NET;
using System.Threading;

namespace SensorDemo
{
    public partial class Form1 : Form
    {


        //###################################################################################################################################
        //###################################################################################################################################
        //##################                                      Definitions                                           #####################
        //###################################################################################################################################
        //###################################################################################################################################

        // ###### Driver defines ######
        FTDI.FT_STATUS ftStatus = FTDI.FT_STATUS.FT_OK;

        // ###### I2C Library defines ######
        const byte I2C_Dir_SDAin_SCLin = 0x00;
        const byte I2C_Dir_SDAin_SCLout = 0x01;
        const byte I2C_Dir_SDAout_SCLout = 0x03;
        const byte I2C_Dir_SDAout_SCLin = 0x02;
        const byte I2C_Data_SDAhi_SCLhi = 0x03;
        const byte I2C_Data_SDAlo_SCLhi = 0x01;
        const byte I2C_Data_SDAlo_SCLlo = 0x00;
        const byte I2C_Data_SDAhi_SCLlo = 0x02;
        // MPSSE clocking commands
        const byte MSB_FALLING_EDGE_CLOCK_BYTE_IN = 0x24;
        const byte MSB_RISING_EDGE_CLOCK_BYTE_IN = 0x20;
        const byte MSB_FALLING_EDGE_CLOCK_BYTE_OUT = 0x11;
        const byte MSB_DOWN_EDGE_CLOCK_BIT_IN = 0x26;
        const byte MSB_UP_EDGE_CLOCK_BYTE_IN = 0x20;
        const byte MSB_UP_EDGE_CLOCK_BYTE_OUT = 0x10;
        const byte MSB_RISING_EDGE_CLOCK_BIT_IN = 0x22;
        const byte MSB_FALLING_EDGE_CLOCK_BIT_OUT = 0x13;
        // Clock
        const uint ClockDivisor = 49;      //          = 199;// for 100KHz
        // Sending and receiving
        static uint NumBytesToSend = 0;
        static uint NumBytesToRead = 0;
        uint NumBytesSent = 0;
        static uint NumBytesRead = 0;
        static byte[] MPSSEbuffer = new byte[500];
        static byte[] InputBuffer = new byte[500];   
        static byte[] InputBuffer2 = new byte[500];
        static uint BytesAvailable = 0;
        static bool I2C_Ack = false;
        static byte AppStatus = 0;
        static byte I2C_Status = 0;
        public bool Running = true;
        static bool DeviceOpen = false;
        // GPIO
        static byte GPIO_Low_Dat = 0;
        static byte GPIO_Low_Dir = 0;
        static byte ADbusReadVal = 0;
        static byte ACbusReadVal = 0;
        
        // ###### Proximity sensor defines ######
        static byte Command = 0x00;
        static byte[] ProxData = new byte[500];
        static UInt16 ProxiValue = 0; 
        static double ProxiValueD = 0;
        public const byte VCNL40x0_ADDRESS = 0x13;//0x13 is 7 bit address, 0x26 is 8bit address
        // registers
        public const byte REGISTER_COMMAND = 0x80;
        public const byte REGISTER_ID = 0x81;
        public const byte REGISTER_PROX_RATE = 0x82;
        public const byte REGISTER_PROX_CURRENT = 0x83;
        public const byte REGISTER_AMBI_PARAMETER = 0x84;
        public const byte REGISTER_AMBI_VALUE = 0x85;
        public const byte REGISTER_PROX_VALUE = 0x87;
        public const byte REGISTER_INTERRUPT_CONTROL = 0x89;
        public const byte REGISTER_INTERRUPT_LOW_THRES = 0x8a;
        public const byte REGISTER_INTERRUPT_HIGH_THRES = 0x8c;
        public const byte REGISTER_INTERRUPT_STATUS = 0x8e;
        public const byte REGISTER_PROX_TIMING = 0xf9;
        // Bits in the registers defined above
        public const byte COMMAND_SELFTIMED_MODE_ENABLE = 0x01;
        public const byte COMMAND_PROX_ENABLE = 0x02;
        public const byte COMMAND_AMBI_ENABLE = 0x04;
        public const byte COMMAND_MASK_PROX_DATA_READY = 0x20;
        public const byte PROX_MEASUREMENT_RATE_31 = 0x04;
        public const byte AMBI_PARA_AVERAGE_32 = 0x05; // DEFAULT
        public const byte AMBI_PARA_AUTO_OFFSET_ENABLE = 0x08; // DEFAULT enable
        public const byte AMBI_PARA_MEAS_RATE_2 = 0x10; // DEFAULT
        public const byte INTERRUPT_THRES_SEL_PROX = 0x00;
        public const byte INTERRUPT_THRES_ENABLE = 0x02;
        public const byte INTERRUPT_COUNT_EXCEED_1 = 0x00; // DEFAULT

        // ###### Colour sensor defines ######
        public const byte COLOR_ADDRESS = 0x29;
        public const byte _ENABLE = 0x80;                   //Enablestatusandinterrupts
        public const byte _ATIME = 0x81;                    //RGBCADCtime
        public const byte _CONTROL = 0x8F;                  //Gaincontrolregister
        public const byte _GAIN_x4 = 0x01;
        public const byte _GAIN_x16 = 0x10;
        public const byte _GAIN_x60 = 0x11;
        static byte Global_Red = 0;
        static byte Global_Green = 0;
        static byte Global_Blue = 0;
        uint devcount = 0;


        


        //###################################################################################################################################
        //###################################################################################################################################
        //##################                          Main Application Layer                                            #####################
        //###################################################################################################################################
        //###################################################################################################################################
        
        
        
        
        // Create new instance of the FTDI device class
        FTDI myFtdiDevice = new FTDI();
                
        public Form1()
        {
            InitializeComponent();
        }


        //###################################################################################################################################
        // When the form loads...

        private void Form1_Load(object sender, EventArgs e)
        {
            
            label4.Text = "Closed";
            label6.Text = "0";

            buttonInit.Enabled = true;
            buttonStart.Enabled = false;
            buttonClose.Enabled = true;

            // Print device type to remind user of type that it was compiled for
            // could auto detect based on device info instead of setting defines at top of code
                label8.Text = "FT232H";
        }

        //###################################################################################################################################
        // Code for the INITIALISE button...
        
        private void button1_Click(object sender, EventArgs e)
        {
            Console.WriteLine("entered button1_click");

            bool DeviceInit = false;
            buttonInit.Enabled = false;
            
            try
            {
                ftStatus = myFtdiDevice.GetNumberOfDevices(ref devcount);
            }
            catch
            {
                label4.Text = "Driver not loaded";
                
                buttonInit.Enabled = false;
                buttonStart.Enabled = false;
                buttonClose.Enabled = true;
            }
            
            // e.g. open a UM232H Module by it's description
            //ftStatus = myFtdiDevice.OpenByDescription("UM232H");  // could replace line below
            ftStatus = myFtdiDevice.OpenByIndex(0);

            // Update the Status text line
            if (ftStatus == FTDI.FT_STATUS.FT_OK)
            {
                DeviceOpen = true;
                label4.Text = "Open";
            }
            else
            {
                DeviceOpen = false;
                label4.Text = "No Device Found";
            }

            Update(); 
            Application.DoEvents();

            // If the device opened successfully, initialise MPSSE and then configure prox and colour sensors over I2C 
            if (DeviceOpen == true)
            {
                DeviceInit = true;
                
                AppStatus = I2C_ConfigureMpsse();
                Console.WriteLine("app status 0: " + AppStatus);
                if (AppStatus != 0)
                {
                    label4.Text = "Failed Init";
                    DeviceInit = false;
                }

                if (DeviceInit == true)
                {
                    //AppStatus = ProximitySensorConfig();

                    AppStatus = I2C_write_GreenPAK(0x08, 0x7A, 0xFF);

                    Console.WriteLine("app status 1: " + AppStatus);

                    if (AppStatus != 0)
                    {
                        label4.Text = "Failed ProxInit";
                        DeviceInit = false;
                    }
                }

                if (DeviceInit == true)
                {
                    // allow user to start or exit
                    buttonInit.Enabled = false;
                    buttonStart.Enabled = true;
                    buttonClose.Enabled = true;

                    label4.Text = "Ready";
                }
                else
                {
                    label4.Text = "Failed Init";
                    myFtdiDevice.Close();
                    // allow re-init or exit
                    buttonInit.Enabled = true;
                    buttonStart.Enabled = false;
                    buttonClose.Enabled = true;
                }
                
                Update();
                Application.DoEvents();
            }
            else
            {
                // allow re-init or exit
                buttonInit.Enabled = true;
                buttonStart.Enabled = false;
                buttonClose.Enabled = true;


            }

            Console.WriteLine("exited button1_click");
        }
        
        //###################################################################################################################################
        // Code for Start button...
        
        private void button2_Click(object sender, EventArgs e)
        {
            Console.WriteLine("entered button2_click");

            if (I2C_write_GreenPAK(0x08, 0x7A, 0x00) > 0)
            {
                Console.WriteLine("nack");
            }

            I2C_read_GreenPAK(0x08, 0x7A);

            Console.WriteLine("exited button2_click");
        }

        //###################################################################################################################################
        // Code for the STOP button
        
        private void button3_Click(object sender, EventArgs e)
        {

            Running = false;
            Console.WriteLine("entered button3_click");

            if (I2C_write_GreenPAK(0x08, 0x7A, 0xFF) > 0)
            {
                Console.WriteLine("nack");
            }

            I2C_read_GreenPAK(0x08, 0x7A);

            Console.WriteLine("exited button3_click");

        }

        //###################################################################################################################################
        //###################################################################################################################################
        //##################                          GreenPAK                                                          #####################
        //###################################################################################################################################
        //###################################################################################################################################

        public byte I2C_write_GreenPAK(byte slave_address, byte register_address, byte data)
        {
            Console.WriteLine("entered I2C write greenpak");
            AppStatus = I2C_SetStart();                                                     // I2C START

            AppStatus += I2C_SendDeviceAddrAndCheckACK((byte)(slave_address), false);       // I2C ADDRESS (for write)
            if (I2C_Ack != true) { I2C_SetStop(); return 1; }                               // if GreenPAK NAKs then send stop and return

            AppStatus += I2C_SendByteAndCheckACK((byte)(register_address));                 // SEND REGISTER ID
            if (I2C_Ack != true) { I2C_SetStop(); return 1; }                               // if GreenPAK NAKs then send stop and return

            AppStatus += I2C_SendByteAndCheckACK((byte)(data));                             // SEND VALUE TO WRITE
            if (I2C_Ack != true) { I2C_SetStop(); return 1; }                               // if GreenPAK NAKs then send stop and return

            AppStatus += I2C_SetStop();                                                     // I2C STOP

            Console.WriteLine("app status bravo:" + AppStatus);
            return AppStatus;
        }

        public byte I2C_read_GreenPAK(byte slave_address, byte register_address)
        {
            Console.WriteLine("entered I2C read greenpak");
            AppStatus = I2C_SetStart();                                                     // I2C START

            AppStatus += I2C_SendDeviceAddrAndCheckACK((byte)(slave_address), false);       // I2C ADDRESS (for write)
            if (I2C_Ack != true) { I2C_SetStop(); return 1; }                               // if GreenPAK NAKs then send stop and return

            AppStatus += I2C_SendByteAndCheckACK((byte)(register_address));                 // SEND REGISTER ID
            if (I2C_Ack != true) { I2C_SetStop(); return 1; }                               // if GreenPAK NAKs then send stop and return

            AppStatus = I2C_SetStart();                                                     // REPEAT START

            AppStatus += I2C_SendDeviceAddrAndCheckACK((byte)(slave_address), true);        // I2C ADDRESS (for read)
            if (I2C_Ack != true) { I2C_SetStop(); return 1; }                               // if GreenPAK NAKs then send stop and return

            AppStatus += I2C_ReadByte(false);                                               // I2C READ (send Nak)
            Console.WriteLine(InputBuffer2[0]);
            //Command = InputBuffer2[0];                                                      // Get the byte read
            
            AppStatus += I2C_SetStop();                                                     // I2C STOP

            Console.WriteLine("app status bravo:" + AppStatus);
            return AppStatus;

        }


        //###################################################################################################################################
        //###################################################################################################################################
        //##################                             I2C Layer                                                      #####################
        //###################################################################################################################################
        //###################################################################################################################################

        public byte I2C_ConfigureMpsse()
        {

            byte ADbusVal = 0;
            byte ADbusDir = 0;
            NumBytesToSend = 0;

            /***** Initial device configuration *****/

            ftStatus = FTDI.FT_STATUS.FT_OK;
            ftStatus |= myFtdiDevice.SetTimeouts(5000, 5000);
            ftStatus |= myFtdiDevice.SetLatency(16);
            ftStatus |= myFtdiDevice.SetFlowControl(FTDI.FT_FLOW_CONTROL.FT_FLOW_RTS_CTS, 0x00,0x00);
            ftStatus |= myFtdiDevice.SetBitMode(0x00, 0x00);
            ftStatus |= myFtdiDevice.SetBitMode(0x00, 0x02);         // MPSSE mode        

            Console.WriteLine(0);

            if (ftStatus != FTDI.FT_STATUS.FT_OK)
                return 1; // error();

            Console.WriteLine(1);

            /***** Flush the buffer *****/
            I2C_Status = FlushBuffer();

            /***** Synchronize the MPSSE interface by sending bad command 0xAA *****/
            NumBytesToSend = 0;
            MPSSEbuffer[NumBytesToSend++] = 0xAA;
            I2C_Status = Send_Data(NumBytesToSend);
            if (I2C_Status != 0) return 1; // error();

            Console.WriteLine(2);

            NumBytesToRead = 2;
            I2C_Status = Receive_Data(2);
            if (I2C_Status !=0) return 1; //error();

            Console.WriteLine(3);

            if ((InputBuffer2[0] == 0xFA) && (InputBuffer2[1] == 0xAA))
            {
                //Console.WriteLine("Bad Command Echo successful");
            }
            else
            {
                return 1;            //error();
            }

            Console.WriteLine(4);

            /***** Synchronize the MPSSE interface by sending bad command 0xAB *****/
            NumBytesToSend = 0;
            MPSSEbuffer[NumBytesToSend++] = 0xAB;
            I2C_Status = Send_Data(NumBytesToSend);
            if (I2C_Status != 0) return 1; // error();

            Console.WriteLine(5);

            NumBytesToRead = 2;
            I2C_Status = Receive_Data(2);
            if (I2C_Status !=0) return 1; //error();

            Console.WriteLine(6);

            if ((InputBuffer2[0] == 0xFA) && (InputBuffer2[1] == 0xAB))
            {
                //Console.WriteLine("Bad Command Echo successful");
            }
            else
            {
                return 1;            //error();
            }

            Console.WriteLine(7);

            NumBytesToSend = 0;
            MPSSEbuffer[NumBytesToSend++] = 0x8A; 	// Disable clock divide by 5 for 60Mhz master clock
            MPSSEbuffer[NumBytesToSend++] = 0x97;	// Turn off adaptive clocking
            MPSSEbuffer[NumBytesToSend++] = 0x8C; 	// Enable 3 phase data clock, used by I2C to allow data on both clock edges
            // The SK clock frequency can be worked out by below algorithm with divide by 5 set as off
            // SK frequency  = 60MHz /((1 +  [(1 +0xValueH*256) OR 0xValueL])*2)
            MPSSEbuffer[NumBytesToSend++] = 0x86; 	//Command to set clock divisor
            MPSSEbuffer[NumBytesToSend++] = (byte)(ClockDivisor & 0x00FF);	//Set 0xValueL of clock divisor
            MPSSEbuffer[NumBytesToSend++] = (byte)((ClockDivisor >> 8) & 0x00FF);	//Set 0xValueH of clock divisor
            MPSSEbuffer[NumBytesToSend++] = 0x85; 			// loopback off

#if (FT232H)
            MPSSEbuffer[NumBytesToSend++] = 0x9E;       //Enable the FT232H's drive-zero mode with the following enable mask...
            MPSSEbuffer[NumBytesToSend++] = 0x07;       // ... Low byte (ADx) enables - bits 0, 1 and 2 and ... 
            MPSSEbuffer[NumBytesToSend++] = 0x00;       //...High byte (ACx) enables - all off

            ADbusVal = (byte)(0x00 | I2C_Data_SDAhi_SCLhi | (GPIO_Low_Dat & 0xF8)); // SDA and SCL both output high (open drain)
            ADbusDir = (byte)(0x00 | I2C_Dir_SDAout_SCLout | (GPIO_Low_Dir & 0xF8));
#else
            ADbusVal = (byte)(0x00 | I2C_Data_SDAlo_SCLlo | (GPIO_Low_Dat & 0xF8));  	// SDA and SCL set low but as input to mimic open drain
            ADbusDir = (byte)(0x00 | I2C_Dir_SDAin_SCLin | (GPIO_Low_Dir & 0xF8));	//

#endif


            MPSSEbuffer[NumBytesToSend++] = 0x80; 	//Command to set directions of lower 8 pins and force value on bits set as output 
            MPSSEbuffer[NumBytesToSend++] = (byte)(ADbusVal);
            MPSSEbuffer[NumBytesToSend++] = (byte)(ADbusDir);

            Console.WriteLine(8);

            I2C_Status = Send_Data(NumBytesToSend);

            Console.WriteLine("I2C status: " + I2C_Status);

            if (I2C_Status != 0)
            {
                return 1;            //error();
            }
            else
            {
                return 0;
            }
        }

        //###################################################################################################################################
        // Reads a byte over I2C 

        public byte I2C_ReadByte(bool ACK)
        {
            byte ADbusVal = 0;
            byte ADbusDir = 0;
            NumBytesToSend = 0;
            
#if (FT232H)
            // Clock in one data byte
            MPSSEbuffer[NumBytesToSend++] = MSB_RISING_EDGE_CLOCK_BYTE_IN;      // Clock data byte in
            MPSSEbuffer[NumBytesToSend++] = 0x00;
            MPSSEbuffer[NumBytesToSend++] = 0x00;                               // Data length of 0x0000 means 1 byte data to clock in

            // clock out one bit as ack/nak bit
            MPSSEbuffer[NumBytesToSend++] = MSB_FALLING_EDGE_CLOCK_BIT_OUT;     // Clock data bit out
            MPSSEbuffer[NumBytesToSend++] = 0x00;                               // Length of 0 means 1 bit
            if (ACK == true)
                MPSSEbuffer[NumBytesToSend++] = 0x00;                           // Data bit to send is a '0'
            else
                MPSSEbuffer[NumBytesToSend++] = 0xFF;                           // Data bit to send is a '1'

            // I2C lines back to idle state 
            ADbusVal = (byte)(0x00 | I2C_Data_SDAhi_SCLlo | (GPIO_Low_Dat & 0xF8));
            ADbusDir = (byte)(0x00 | I2C_Dir_SDAout_SCLout | (GPIO_Low_Dir & 0xF8));
            MPSSEbuffer[NumBytesToSend++] = 0x80;                               //       ' Command - set low byte
            MPSSEbuffer[NumBytesToSend++] = ADbusVal;                            //      ' Set the values
            MPSSEbuffer[NumBytesToSend++] = ADbusDir;                             //     ' Set the directions
#else          
            // Ensure line is definitely an input since FT2232H and FT4232H don't have open drain
            ADbusVal = (byte)(0x00 | I2C_Data_SDAlo_SCLlo | (GPIO_Low_Dat & 0xF8));
            ADbusDir = (byte)(0x00 | I2C_Dir_SDAin_SCLout | (GPIO_Low_Dir & 0xF8)); // make data input
            MPSSEbuffer[NumBytesToSend++] = 0x80;                                   // command - set low byte
            MPSSEbuffer[NumBytesToSend++] = ADbusVal;                               // Set the values
            MPSSEbuffer[NumBytesToSend++] = ADbusDir;                               // Set the directions
            // Clock one byte of data in from the sensor
            MPSSEbuffer[NumBytesToSend++] = MSB_RISING_EDGE_CLOCK_BYTE_IN;      // Clock data byte in
            MPSSEbuffer[NumBytesToSend++] = 0x00;
            MPSSEbuffer[NumBytesToSend++] = 0x00;                               // Data length of 0x0000 means 1 byte data to clock in
            
            // Change direction back to output and clock out one bit. If ACK is true, we send bit as 0 as an acknowledge
            ADbusVal = (byte)(0x00 | I2C_Data_SDAlo_SCLlo | (GPIO_Low_Dat & 0xF8));
            ADbusDir = (byte)(0x00 | I2C_Dir_SDAout_SCLout | (GPIO_Low_Dir & 0xF8));    // back to output
            MPSSEbuffer[NumBytesToSend++] = 0x80;                               // Command - set low byte
            MPSSEbuffer[NumBytesToSend++] = ADbusVal;                           // set the values
            MPSSEbuffer[NumBytesToSend++] = ADbusDir;                           // set the directions

            MPSSEbuffer[NumBytesToSend++] = MSB_FALLING_EDGE_CLOCK_BIT_OUT;    // Clock data bit out
            MPSSEbuffer[NumBytesToSend++] = 0x00;                              // Length of 0 means 1 bit
            if (ACK == true)
            {
                MPSSEbuffer[NumBytesToSend++] = 0x00;                          // Data bit to send is a '0'
            }
            else
            {
                MPSSEbuffer[NumBytesToSend++] = 0xFF;                          // Data bit to send is a '1'
            }

            // Put line states back to idle with SDA open drain high (set to input) 
            ADbusVal = (byte)(0x00 | I2C_Data_SDAlo_SCLlo | (GPIO_Low_Dat & 0xF8));
            ADbusDir = (byte)(0x00 | I2C_Dir_SDAin_SCLout | (GPIO_Low_Dir & 0xF8));//make data input
            MPSSEbuffer[NumBytesToSend++] = 0x80;                               //       ' Command - set low byte
            MPSSEbuffer[NumBytesToSend++] = ADbusVal;                            //      ' Set the values
            MPSSEbuffer[NumBytesToSend++] = ADbusDir;                             //     ' Set the directions

            
#endif
            // This command then tells the MPSSE to send any results gathered back immediately
            MPSSEbuffer[NumBytesToSend++] = 0x87;                                  //    ' Send answer back immediate command

            // send commands to chip
            I2C_Status = Send_Data(NumBytesToSend);
            if (I2C_Status != 0)
            {
                return 1;
            }

            // get the byte which has been read from the driver's receive buffer
            I2C_Status = Receive_Data(1);
            if (I2C_Status != 0)
            {
                return 1;
            }
            
            // InputBuffer2[0] now contains the results

            return 0;
        }
         
        //###################################################################################################################################
        // Sends I2C address followed by reading 2 bytes
        
        public byte I2C_Read2BytesWithAddr(byte Address)
        {
            byte ADbusVal = 0;
            byte ADbusDir = 0;
            NumBytesToSend = 0;
            
            // ------------------------------------ Address ------------------------------------
            
            Address <<= 1;
            Address |= 0x01;

#if (FT232H)
            MPSSEbuffer[NumBytesToSend++] = MSB_FALLING_EDGE_CLOCK_BYTE_OUT;        // clock data byte out
            MPSSEbuffer[NumBytesToSend++] = 0x00;                                   // 
            MPSSEbuffer[NumBytesToSend++] = 0x00;                                   //  Data length of 0x0000 means 1 byte data to clock in
            MPSSEbuffer[NumBytesToSend++] = Address;// DataSend[0];          //  Byte to send

            // Put line back to idle (data released, clock pulled low)
            ADbusVal = (byte)(0x00 | I2C_Data_SDAhi_SCLlo | (GPIO_Low_Dat & 0xF8));
            ADbusDir = (byte)(0x00 | I2C_Dir_SDAout_SCLout | (GPIO_Low_Dir & 0xF8));// make data input
            MPSSEbuffer[NumBytesToSend++] = 0x80;                                   // Command - set low byte
            MPSSEbuffer[NumBytesToSend++] = ADbusVal;                               // Set the values
            MPSSEbuffer[NumBytesToSend++] = ADbusDir;                               // Set the directions

            // CLOCK IN ACK
            MPSSEbuffer[NumBytesToSend++] = MSB_RISING_EDGE_CLOCK_BIT_IN;           // clock data bits in
            MPSSEbuffer[NumBytesToSend++] = 0x00;                                   // Length of 0 means 1 bit
#else

            // Set directions of clock and data to output in preparation for clocking out a byte
            ADbusVal = (byte)(0x00 | I2C_Data_SDAlo_SCLlo | (GPIO_Low_Dat & 0xF8));
            ADbusDir = (byte)(0x00 | I2C_Dir_SDAout_SCLout | (GPIO_Low_Dir & 0xF8));// back to output
            MPSSEbuffer[NumBytesToSend++] = 0x80;                                   // Command - set low byte
            MPSSEbuffer[NumBytesToSend++] = ADbusVal;                               // Set the values
            MPSSEbuffer[NumBytesToSend++] = ADbusDir;                               // Set the directions
            // clock out one byte
            MPSSEbuffer[NumBytesToSend++] = MSB_FALLING_EDGE_CLOCK_BYTE_OUT;        // clock data byte out
            MPSSEbuffer[NumBytesToSend++] = 0x00;                                   // 
            MPSSEbuffer[NumBytesToSend++] = 0x00;                                   // Data length of 0x0000 means 1 byte data to clock in
            MPSSEbuffer[NumBytesToSend++] = Address;                         // Byte to send

            // Put line back to idle (data released, clock pulled low) so that sensor can drive data line
            ADbusVal = (byte)(0x00 | I2C_Data_SDAlo_SCLlo | (GPIO_Low_Dat & 0xF8));
            ADbusDir = (byte)(0x00 | I2C_Dir_SDAin_SCLout | (GPIO_Low_Dir & 0xF8)); // make data input
            MPSSEbuffer[NumBytesToSend++] = 0x80;                                   // Command - set low byte
            MPSSEbuffer[NumBytesToSend++] = ADbusVal;                               // Set the values
            MPSSEbuffer[NumBytesToSend++] = ADbusDir;                               // Set the directions

            // CLOCK IN ACK
            MPSSEbuffer[NumBytesToSend++] = MSB_RISING_EDGE_CLOCK_BIT_IN;           // clock data byte in
            MPSSEbuffer[NumBytesToSend++] = 0x00;                                   // Length of 0 means 1 bit

#endif

            // ------------------------------------ Clock in 1st byte and ACK ------------------------------------

#if (FT232H)
            MPSSEbuffer[NumBytesToSend++] = MSB_RISING_EDGE_CLOCK_BYTE_IN;      // Clock data byte in
            MPSSEbuffer[NumBytesToSend++] = 0x00;
            MPSSEbuffer[NumBytesToSend++] = 0x00;                               // Data length of 0x0000 means 1 byte data to clock in

            MPSSEbuffer[NumBytesToSend++] = MSB_FALLING_EDGE_CLOCK_BIT_OUT;    // Clock data bit out
            MPSSEbuffer[NumBytesToSend++] = 0x00;                              // Length of 0 means 1 bit
            MPSSEbuffer[NumBytesToSend++] = 0x00;                              // Sending 0 here as ACK

            ADbusVal = (byte)(0x00 | I2C_Data_SDAhi_SCLlo | (GPIO_Low_Dat & 0xF8));
            ADbusDir = (byte)(0x00 | I2C_Dir_SDAout_SCLout | (GPIO_Low_Dir & 0xF8));

            MPSSEbuffer[NumBytesToSend++] = 0x80;                               //       ' Command - set low byte
            MPSSEbuffer[NumBytesToSend++] = ADbusVal;                            //      ' Set the values
            MPSSEbuffer[NumBytesToSend++] = ADbusDir;                             //     ' Set the directions
#else          

            MPSSEbuffer[NumBytesToSend++] = MSB_RISING_EDGE_CLOCK_BYTE_IN;      // Clock data byte in
            MPSSEbuffer[NumBytesToSend++] = 0x00;
            MPSSEbuffer[NumBytesToSend++] = 0x00;                               // Data length of 0x0000 means 1 byte data to clock in
            
            // Send a 0 bit as an acknowledge
            ADbusVal = (byte)(0x00 | I2C_Data_SDAlo_SCLlo | (GPIO_Low_Dat & 0xF8));
            ADbusDir = (byte)(0x00 | I2C_Dir_SDAout_SCLout | (GPIO_Low_Dir & 0xF8));//back to output
            MPSSEbuffer[NumBytesToSend++] = 0x80;                               //       ' Command - set low byte
            MPSSEbuffer[NumBytesToSend++] = ADbusVal;                            //      ' Set the values
            MPSSEbuffer[NumBytesToSend++] = ADbusDir;                             //     ' Set the directions

            MPSSEbuffer[NumBytesToSend++] = MSB_FALLING_EDGE_CLOCK_BIT_OUT;    // Clock data bit out
            MPSSEbuffer[NumBytesToSend++] = 0x00;                              // Length of 0 means 1 bit
            MPSSEbuffer[NumBytesToSend++] = 0x00;                              // Sending 0 here as ACK
            
            ADbusVal = (byte)(0x00 | I2C_Data_SDAlo_SCLlo | (GPIO_Low_Dat & 0xF8));
            ADbusDir = (byte)(0x00 | I2C_Dir_SDAin_SCLout | (GPIO_Low_Dir & 0xF8));//make data input
            
            MPSSEbuffer[NumBytesToSend++] = 0x80;                               //       ' Command - set low byte
            MPSSEbuffer[NumBytesToSend++] = ADbusVal;                            //      ' Set the values
            MPSSEbuffer[NumBytesToSend++] = ADbusDir;                             //     ' Set the directions

            
#endif

            // ------------------------------------ Clock in 2nd byte and NAK ------------------------------------

#if (FT232H)
            MPSSEbuffer[NumBytesToSend++] = MSB_RISING_EDGE_CLOCK_BYTE_IN;      // Clock data byte in
            MPSSEbuffer[NumBytesToSend++] = 0x00;
            MPSSEbuffer[NumBytesToSend++] = 0x00;                               // Data length of 0x0000 means 1 byte data to clock in

            MPSSEbuffer[NumBytesToSend++] = MSB_FALLING_EDGE_CLOCK_BIT_OUT;    // Clock data bit out
            MPSSEbuffer[NumBytesToSend++] = 0x00;                              // Length of 0 means 1 bit
            MPSSEbuffer[NumBytesToSend++] = 0xFF;                              // Sending 1 here as NAK

            ADbusVal = (byte)(0x00 | I2C_Data_SDAhi_SCLlo | (GPIO_Low_Dat & 0xF8));
            ADbusDir = (byte)(0x00 | I2C_Dir_SDAout_SCLout | (GPIO_Low_Dir & 0xF8));

            MPSSEbuffer[NumBytesToSend++] = 0x80;                               //       ' Command - set low byte
            MPSSEbuffer[NumBytesToSend++] = ADbusVal;                            //      ' Set the values
            MPSSEbuffer[NumBytesToSend++] = ADbusDir;                             //     ' Set the directions
#else
            MPSSEbuffer[NumBytesToSend++] = MSB_RISING_EDGE_CLOCK_BYTE_IN;      // Clock data byte in
            MPSSEbuffer[NumBytesToSend++] = 0x00;
            MPSSEbuffer[NumBytesToSend++] = 0x00;                               // Data length of 0x0000 means 1 byte data to clock in
            
            // Send a 1 bit as a Nack
            ADbusVal = (byte)(0x00 | I2C_Data_SDAlo_SCLlo | (GPIO_Low_Dat & 0xF8));
            ADbusDir = (byte)(0x00 | I2C_Dir_SDAout_SCLout | (GPIO_Low_Dir & 0xF8));//back to output
            MPSSEbuffer[NumBytesToSend++] = 0x80;                               //       ' Command - set low byte
            MPSSEbuffer[NumBytesToSend++] = ADbusVal;                            //      ' Set the values
            MPSSEbuffer[NumBytesToSend++] = ADbusDir;                             //     ' Set the directions

            MPSSEbuffer[NumBytesToSend++] = MSB_FALLING_EDGE_CLOCK_BIT_OUT;    // Clock data bit out
            MPSSEbuffer[NumBytesToSend++] = 0x00;                              // Length of 0 means 1 bit
            MPSSEbuffer[NumBytesToSend++] = 0xFF;                              // Sending 1 here as NAK
                                   
            ADbusVal = (byte)(0x00 | I2C_Data_SDAlo_SCLlo | (GPIO_Low_Dat & 0xF8));
            ADbusDir = (byte)(0x00 | I2C_Dir_SDAin_SCLout | (GPIO_Low_Dir & 0xF8));//make data input
            
            MPSSEbuffer[NumBytesToSend++] = 0x80;                               //       ' Command - set low byte
            MPSSEbuffer[NumBytesToSend++] = ADbusVal;                            //      ' Set the values
            MPSSEbuffer[NumBytesToSend++] = ADbusDir;                             //     ' Set the directions
                        
#endif
            // This command then tells the MPSSE to send any results gathered back immediately
            MPSSEbuffer[NumBytesToSend++] = 0x87;                                //  ' Send answer back immediate command

            // Send off the commands
            I2C_Status = Send_Data(NumBytesToSend);
            if (I2C_Status != 0)
            {
                return 1;
            }

            // Read back the ack from the address phase and the 2 bytes read
            I2C_Status = Receive_Data(3);
            if (I2C_Status != 0)
            {
                return 1;
            }
            
            // Check if address phase was acked
            if ((InputBuffer2[0] & 0x01) == 0)
            {
                I2C_Ack = true;
            }
            else
            {
                I2C_Ack = false;
            }
            
            // Get the two data bytes to put back to the calling function - InputBuffer2[0..1] now contains the results
            InputBuffer2[0] = InputBuffer2[1];
            InputBuffer2[1] = InputBuffer2[2];            
                        
            return 0;
            
        }
        
        //###################################################################################################################################

        public byte I2C_SendDeviceAddrAndCheckACK(byte Address, bool Read)
        {


            byte ADbusVal = 0;
            byte ADbusDir = 0;
            NumBytesToSend = 0;

            Address <<= 1;
            if (Read == true)
                Address |= 0x01;

#if (FT232H)
            MPSSEbuffer[NumBytesToSend++] = MSB_FALLING_EDGE_CLOCK_BYTE_OUT;        // clock data byte out
            MPSSEbuffer[NumBytesToSend++] = 0x00;                                   // 
            MPSSEbuffer[NumBytesToSend++] = 0x00;                                   //  Data length of 0x0000 means 1 byte data to clock in
            MPSSEbuffer[NumBytesToSend++] = Address;           //  Byte to send

            // Put line back to idle (data released, clock pulled low)
            ADbusVal = (byte)(0x00 | I2C_Data_SDAhi_SCLlo | (GPIO_Low_Dat & 0xF8));
            ADbusDir = (byte)(0x00 | I2C_Dir_SDAout_SCLout | (GPIO_Low_Dir & 0xF8));// make data input
            MPSSEbuffer[NumBytesToSend++] = 0x80;                                   // Command - set low byte
            MPSSEbuffer[NumBytesToSend++] = ADbusVal;                               // Set the values
            MPSSEbuffer[NumBytesToSend++] = ADbusDir;                               // Set the directions

            // CLOCK IN ACK
            MPSSEbuffer[NumBytesToSend++] = MSB_RISING_EDGE_CLOCK_BIT_IN;           // clock data bits in
            MPSSEbuffer[NumBytesToSend++] = 0x00;                                   // Length of 0 means 1 bit
#else

            // Set directions of clock and data to output in preparation for clocking out a byte
            ADbusVal = (byte)(0x00 | I2C_Data_SDAlo_SCLlo | (GPIO_Low_Dat & 0xF8));
            ADbusDir = (byte)(0x00 | I2C_Dir_SDAout_SCLout | (GPIO_Low_Dir & 0xF8));// back to output
            MPSSEbuffer[NumBytesToSend++] = 0x80;                                   // Command - set low byte
            MPSSEbuffer[NumBytesToSend++] = ADbusVal;                               // Set the values
            MPSSEbuffer[NumBytesToSend++] = ADbusDir;                               // Set the directions
            // clock out one byte
            MPSSEbuffer[NumBytesToSend++] = MSB_FALLING_EDGE_CLOCK_BYTE_OUT;        // clock data byte out
            MPSSEbuffer[NumBytesToSend++] = 0x00;                                   // 
            MPSSEbuffer[NumBytesToSend++] = 0x00;                                   // Data length of 0x0000 means 1 byte data to clock in
            MPSSEbuffer[NumBytesToSend++] = Address;                         // Byte to send

            // Put line back to idle (data released, clock pulled low) so that sensor can drive data line
            ADbusVal = (byte)(0x00 | I2C_Data_SDAlo_SCLlo | (GPIO_Low_Dat & 0xF8));
            ADbusDir = (byte)(0x00 | I2C_Dir_SDAin_SCLout | (GPIO_Low_Dir & 0xF8)); // make data input
            MPSSEbuffer[NumBytesToSend++] = 0x80;                                   // Command - set low byte
            MPSSEbuffer[NumBytesToSend++] = ADbusVal;                               // Set the values
            MPSSEbuffer[NumBytesToSend++] = ADbusDir;                               // Set the directions

            // CLOCK IN ACK
            MPSSEbuffer[NumBytesToSend++] = MSB_RISING_EDGE_CLOCK_BIT_IN;           // clock data byte in
            MPSSEbuffer[NumBytesToSend++] = 0x00;                                   // Length of 0 means 1 bit

#endif
            // This command then tells the MPSSE to send any results gathered (in this case the ack bit) back immediately
            MPSSEbuffer[NumBytesToSend++] = 0x87;                                //  ' Send answer back immediate command

            // send commands to chip
            I2C_Status = Send_Data(NumBytesToSend);
            if (I2C_Status != 0)
            {
                return 1;
            }

            // read back byte containing ack
            I2C_Status = Receive_Data(1);
            if (I2C_Status != 0)
            {
                return 1;            // can also check NumBytesRead
            }

            // if ack bit is 0 then sensor acked the transfer, otherwise it nak'd the transfer
            if ((InputBuffer2[0] & 0x01) == 0)
            {
                I2C_Ack = true;
            }
            else
            {
                I2C_Ack = false;
            }

            return 0;

        }

        //###################################################################################################################################
        // Writes one byte to the I2C bus

        public byte I2C_SendByteAndCheckACK(byte DataByteToSend)
        {
            byte ADbusVal = 0;
            byte ADbusDir = 0;
            NumBytesToSend = 0;

#if (FT232H)
            MPSSEbuffer[NumBytesToSend++] = MSB_FALLING_EDGE_CLOCK_BYTE_OUT;        // clock data byte out
            MPSSEbuffer[NumBytesToSend++] = 0x00;                                   // 
            MPSSEbuffer[NumBytesToSend++] = 0x00;                                   //  Data length of 0x0000 means 1 byte data to clock in
            MPSSEbuffer[NumBytesToSend++] = DataByteToSend;// DataSend[0];          //  Byte to send

            // Put line back to idle (data released, clock pulled low)
            ADbusVal = (byte)(0x00 | I2C_Data_SDAhi_SCLlo | (GPIO_Low_Dat & 0xF8));
            ADbusDir = (byte)(0x00 | I2C_Dir_SDAout_SCLout | (GPIO_Low_Dir & 0xF8));// make data input
            MPSSEbuffer[NumBytesToSend++] = 0x80;                                   // Command - set low byte
            MPSSEbuffer[NumBytesToSend++] = ADbusVal;                               // Set the values
            MPSSEbuffer[NumBytesToSend++] = ADbusDir;                               // Set the directions

            // CLOCK IN ACK
            MPSSEbuffer[NumBytesToSend++] = MSB_RISING_EDGE_CLOCK_BIT_IN;           // clock data bits in
            MPSSEbuffer[NumBytesToSend++] = 0x00;                                   // Length of 0 means 1 bit
#else

            // Set directions of clock and data to output in preparation for clocking out a byte
            ADbusVal = (byte)(0x00 | I2C_Data_SDAlo_SCLlo | (GPIO_Low_Dat & 0xF8));
            ADbusDir = (byte)(0x00 | I2C_Dir_SDAout_SCLout | (GPIO_Low_Dir & 0xF8));// back to output
            MPSSEbuffer[NumBytesToSend++] = 0x80;                                   // Command - set low byte
            MPSSEbuffer[NumBytesToSend++] = ADbusVal;                               // Set the values
            MPSSEbuffer[NumBytesToSend++] = ADbusDir;                               // Set the directions
            // clock out one byte
            MPSSEbuffer[NumBytesToSend++] = MSB_FALLING_EDGE_CLOCK_BYTE_OUT;        // clock data byte out
            MPSSEbuffer[NumBytesToSend++] = 0x00;                                   // 
            MPSSEbuffer[NumBytesToSend++] = 0x00;                                   // Data length of 0x0000 means 1 byte data to clock in
            MPSSEbuffer[NumBytesToSend++] = DataByteToSend;                         // Byte to send

            // Put line back to idle (data released, clock pulled low) so that sensor can drive data line
            ADbusVal = (byte)(0x00 | I2C_Data_SDAlo_SCLlo | (GPIO_Low_Dat & 0xF8));
            ADbusDir = (byte)(0x00 | I2C_Dir_SDAin_SCLout | (GPIO_Low_Dir & 0xF8)); // make data input
            MPSSEbuffer[NumBytesToSend++] = 0x80;                                   // Command - set low byte
            MPSSEbuffer[NumBytesToSend++] = ADbusVal;                               // Set the values
            MPSSEbuffer[NumBytesToSend++] = ADbusDir;                               // Set the directions

            // CLOCK IN ACK
            MPSSEbuffer[NumBytesToSend++] = MSB_RISING_EDGE_CLOCK_BIT_IN;           // clock data byte in
            MPSSEbuffer[NumBytesToSend++] = 0x00;                                   // Length of 0 means 1 bit

#endif
            // This command then tells the MPSSE to send any results gathered (in this case the ack bit) back immediately
            MPSSEbuffer[NumBytesToSend++] = 0x87;                                //  ' Send answer back immediate command

            // send commands to chip
            I2C_Status = Send_Data(NumBytesToSend);
            if (I2C_Status != 0)
            {
                return 1;
            }

            // read back byte containing ack
            I2C_Status = Receive_Data(1);
            if (I2C_Status != 0)
            {
                return 1;            // can also check NumBytesRead
            }
       
            // if ack bit is 0 then sensor acked the transfer, otherwise it nak'd the transfer
            if ((InputBuffer2[0] & 0x01) == 0)
            {
                I2C_Ack = true;
            }
            else
            {
                I2C_Ack = false;
            }

            return 0;
                               
        }
        
        //###################################################################################################################################
        // Sets I2C Start condition

        public byte I2C_SetStart()
        {
            byte Count = 0;
            byte ADbusVal = 0;
            byte ADbusDir = 0;
            NumBytesToSend = 0;


#if (FT232H)
            // SDA high, SCL high
            ADbusVal = (byte)(0x00 | I2C_Data_SDAhi_SCLhi | (GPIO_Low_Dat & 0xF8));
            ADbusDir = (byte)(0x00 | I2C_Dir_SDAout_SCLout | (GPIO_Low_Dir & 0xF8));    // on FT232H lines always output

            for (Count = 0; Count < 6; Count++)
            {
                MPSSEbuffer[NumBytesToSend++] = 0x80;	    // ADbus GPIO command
                MPSSEbuffer[NumBytesToSend++] = ADbusVal;   // Set data value
                MPSSEbuffer[NumBytesToSend++] = ADbusDir;	// Set direction
            }

            // SDA lo, SCL high
            ADbusVal = (byte)(0x00 | I2C_Data_SDAlo_SCLhi | (GPIO_Low_Dat & 0xF8));

            for (Count = 0; Count < 6; Count++)	// Repeat commands to ensure the minimum period of the start setup time ie 600ns is achieved
            {
                MPSSEbuffer[NumBytesToSend++] = 0x80;	    // ADbus GPIO command
                MPSSEbuffer[NumBytesToSend++] = ADbusVal;   // Set data value
                MPSSEbuffer[NumBytesToSend++] = ADbusDir;	// Set direction
            }

            // SDA lo, SCL lo
            ADbusVal = (byte)(0x00 | I2C_Data_SDAlo_SCLlo | (GPIO_Low_Dat & 0xF8));

            for (Count = 0; Count < 6; Count++)	// Repeat commands to ensure the minimum period of the start setup time ie 600ns is achieved
            {
                MPSSEbuffer[NumBytesToSend++] = 0x80;	    // ADbus GPIO command
                MPSSEbuffer[NumBytesToSend++] = ADbusVal;   // Set data value
                MPSSEbuffer[NumBytesToSend++] = ADbusDir;	// Set direction
            }

            // Release SDA
            ADbusVal = (byte)(0x00 | I2C_Data_SDAhi_SCLlo | (GPIO_Low_Dat & 0xF8));

            MPSSEbuffer[NumBytesToSend++] = 0x80;	    // ADbus GPIO command
            MPSSEbuffer[NumBytesToSend++] = ADbusVal;   // Set data value
            MPSSEbuffer[NumBytesToSend++] = ADbusDir;	// Set direction


# else

            // Both SDA and SCL high (setting to input simulates open drain high)
            ADbusVal = (byte)(0x00 | I2C_Data_SDAlo_SCLlo | (GPIO_Low_Dat & 0xF8));
            ADbusDir = (byte)(0x00 | I2C_Dir_SDAin_SCLin | (GPIO_Low_Dir & 0xF8));

            for (Count = 0; Count < 6; Count++)
            {
                MPSSEbuffer[NumBytesToSend++] = 0x80;	    // ADbus GPIO command
                MPSSEbuffer[NumBytesToSend++] = ADbusVal;   // Set data value
                MPSSEbuffer[NumBytesToSend++] = ADbusDir;	// Set direction
            }

            // SDA low, SCL high (setting to input simulates open drain high)
            ADbusVal = (byte)(0x00 | I2C_Data_SDAlo_SCLlo | (GPIO_Low_Dat & 0xF8));
            ADbusDir = (byte)(0x00 | I2C_Dir_SDAout_SCLin | (GPIO_Low_Dir & 0xF8));

            for (Count = 0; Count < 6; Count++)	// Repeat commands to ensure the minimum period of the start setup time
            {
                MPSSEbuffer[NumBytesToSend++] = 0x80;	    // ADbus GPIO command
                MPSSEbuffer[NumBytesToSend++] = ADbusVal;   // Set data value
                MPSSEbuffer[NumBytesToSend++] = ADbusDir;	// Set direction
            }

            // SDA low, SCL low
            ADbusVal = (byte)(0x00 | I2C_Data_SDAlo_SCLlo | (GPIO_Low_Dat & 0xF8));//
            ADbusDir = (byte)(0x00 | I2C_Dir_SDAout_SCLout | (GPIO_Low_Dir & 0xF8));//as above

            for (Count = 0; Count < 6; Count++)	// Repeat commands to ensure the minimum period of the start setup time
            {
                MPSSEbuffer[NumBytesToSend++] = 0x80;	    // ADbus GPIO command
                MPSSEbuffer[NumBytesToSend++] = ADbusVal;   // Set data value
                MPSSEbuffer[NumBytesToSend++] = ADbusDir;	// Set direction
            }

            // Release SDA (setting to input simulates open drain high)
            ADbusVal = (byte)(0x00 | I2C_Data_SDAlo_SCLlo | (GPIO_Low_Dat & 0xF8));//
            ADbusDir = (byte)(0x00 | I2C_Dir_SDAin_SCLout | (GPIO_Low_Dir & 0xF8));//as above

            MPSSEbuffer[NumBytesToSend++] = 0x80;	    // ADbus GPIO command
            MPSSEbuffer[NumBytesToSend++] = ADbusVal;   // Set data value
            MPSSEbuffer[NumBytesToSend++] = ADbusDir;	// Set direction



# endif
            I2C_Status = Send_Data(NumBytesToSend);
            if (I2C_Status != 0)
                return 1;
            else
                return 0;

        }
        
        //###################################################################################################################################
        // Sets I2C Stop condition

        public byte I2C_SetStop()
        {
            byte Count = 0;
            byte ADbusVal = 0;
            byte ADbusDir = 0;
            NumBytesToSend = 0;

#if (FT232H)
            // SDA low, SCL low
            ADbusVal = (byte)(0x00 | I2C_Data_SDAlo_SCLlo | (GPIO_Low_Dat & 0xF8));
            ADbusDir = (byte)(0x00 | I2C_Dir_SDAout_SCLout | (GPIO_Low_Dir & 0xF8));    // on FT232H lines always output

            for (Count = 0; Count < 6; Count++)
            {
                MPSSEbuffer[NumBytesToSend++] = 0x80;	    // ADbus GPIO command
                MPSSEbuffer[NumBytesToSend++] = ADbusVal;   // Set data value
                MPSSEbuffer[NumBytesToSend++] = ADbusDir;	// Set direction
            }
           
            // SDA low, SCL high
            ADbusVal = (byte)(0x00 | I2C_Data_SDAlo_SCLhi | (GPIO_Low_Dat & 0xF8));
            ADbusDir = (byte)(0x00 | I2C_Dir_SDAout_SCLout | (GPIO_Low_Dir & 0xF8));    // on FT232H lines always output

            for (Count = 0; Count < 6; Count++)
            {
                MPSSEbuffer[NumBytesToSend++] = 0x80;	    // ADbus GPIO command
                MPSSEbuffer[NumBytesToSend++] = ADbusVal;   // Set data value
                MPSSEbuffer[NumBytesToSend++] = ADbusDir;	// Set direction
            }

            // SDA high, SCL high
            ADbusVal = (byte)(0x00 | I2C_Data_SDAhi_SCLhi | (GPIO_Low_Dat & 0xF8));
            ADbusDir = (byte)(0x00 | I2C_Dir_SDAout_SCLout | (GPIO_Low_Dir & 0xF8));        // on FT232H lines always output

            for (Count = 0; Count < 6; Count++)	
            {
                MPSSEbuffer[NumBytesToSend++] = 0x80;	    // ADbus GPIO command
                MPSSEbuffer[NumBytesToSend++] = ADbusVal;   // Set data value
                MPSSEbuffer[NumBytesToSend++] = ADbusDir;	// Set direction
            }
           
# else

            // SDA low, SCL low
            ADbusVal = (byte)(0x00 | I2C_Data_SDAlo_SCLlo | (GPIO_Low_Dat & 0xF8));
            ADbusDir = (byte)(0x00 | I2C_Dir_SDAout_SCLout | (GPIO_Low_Dir & 0xF8));

            for (Count = 0; Count < 6; Count++)
            {
                MPSSEbuffer[NumBytesToSend++] = 0x80;	    // ADbus GPIO command
                MPSSEbuffer[NumBytesToSend++] = ADbusVal;   // Set data value
                MPSSEbuffer[NumBytesToSend++] = ADbusDir;	// Set direction
            }


            // SDA low, SCL high (note: setting to input simulates open drain high)
            ADbusVal = (byte)(0x00 | I2C_Data_SDAlo_SCLlo | (GPIO_Low_Dat & 0xF8));
            ADbusDir = (byte)(0x00 | I2C_Dir_SDAout_SCLin | (GPIO_Low_Dir & 0xF8));

            for (Count = 0; Count < 6; Count++)
            {
                MPSSEbuffer[NumBytesToSend++] = 0x80;	    // ADbus GPIO command
                MPSSEbuffer[NumBytesToSend++] = ADbusVal;   // Set data value
                MPSSEbuffer[NumBytesToSend++] = ADbusDir;	// Set direction
            }

            // SDA high, SCL high (note: setting to input simulates open drain high)
            ADbusVal = (byte)(0x00 | I2C_Data_SDAlo_SCLlo | (GPIO_Low_Dat & 0xF8));
            ADbusDir = (byte)(0x00 | I2C_Dir_SDAin_SCLin | (GPIO_Low_Dir & 0xF8));

            for (Count = 0; Count < 6; Count++)	// Repeat commands to hold states for longer time
            {
                MPSSEbuffer[NumBytesToSend++] = 0x80;	    // ADbus GPIO command
                MPSSEbuffer[NumBytesToSend++] = ADbusVal;   // Set data value
                MPSSEbuffer[NumBytesToSend++] = ADbusDir;	// Set direction
            }
#endif
           // send the buffer of commands to the chip 
           I2C_Status = Send_Data(NumBytesToSend);
            if (I2C_Status != 0)
                return 1;
            else
                return 0;

        }
        
       //###################################################################################################################################
       // Sets GPIO values on low byte and puts I2C lines (bits 0, 1, 2) to idle outwith transaction state
       
        public byte I2C_SetLineStatesIdle()
       {
           byte ADbusVal = 0;
           byte ADbusDir = 0;
           NumBytesToSend = 0;
            
#if (FT232H)
           // '######## Combine the I2C line state for bits 2..0 with the GPIO for bits 7..3 ########
           ADbusVal = (byte)(0x00 | I2C_Data_SDAhi_SCLhi | (GPIO_Low_Dat & 0xF8));
           ADbusDir = (byte)(0x00 | I2C_Dir_SDAout_SCLout | (GPIO_Low_Dir & 0xF8));    // FT232H always output due to open drain capability    
           
# else
           ADbusVal = (byte)(0x00 | I2C_Data_SDAlo_SCLlo | (GPIO_Low_Dat & 0xF8));
           ADbusDir = (byte)(0x00 | I2C_Dir_SDAin_SCLin | (GPIO_Low_Dir & 0xF8));       // FT2232H/FT4232H use input to mimic open drain
# endif
            
           MPSSEbuffer[NumBytesToSend++] = 0x80;	    // ADbus GPIO command
           MPSSEbuffer[NumBytesToSend++] = ADbusVal;   // Set data value
           MPSSEbuffer[NumBytesToSend++] = ADbusDir;	// Set direction

           I2C_Status = Send_Data(NumBytesToSend);
           if (I2C_Status != 0)
               return 1;
           else
               return 0;
       }

       //###################################################################################################################################
       // Gets GPIO values from low byte

       public byte I2C_GetGPIOValuesLow()
       {
           NumBytesToSend = 0;

           MPSSEbuffer[NumBytesToSend++] = 0x81;	    // ADbus GPIO command for reading low byte
           MPSSEbuffer[NumBytesToSend++] = 0x87;        // Send answer back immediate command
                      
           I2C_Status = Send_Data(NumBytesToSend);
           if (I2C_Status != 0)
               return 1;

           I2C_Status = Receive_Data(1);
           if (I2C_Status != 0)
           {
               return 1;
           }

           ADbusReadVal = (byte)(InputBuffer2[0] & 0xF8); // mask the returned value to show only 5 GPIO lines (bits 0/1/2 are I2C)
               
           return 0;
       }
       
       //###################################################################################################################################
       // Sets GPIO values on high byte

       public byte I2C_SetGPIOValuesHigh(byte ACbusDir, byte ACbusVal)
       {
           NumBytesToSend = 0;
           
#if (FT4232H)

           return 1;
           
# else
           MPSSEbuffer[NumBytesToSend++] = 0x82;	    // ACbus GPIO command
           MPSSEbuffer[NumBytesToSend++] = ACbusVal;   // Set data value
           MPSSEbuffer[NumBytesToSend++] = ACbusDir;	// Set direction
                      
           I2C_Status = Send_Data(NumBytesToSend);
           if (I2C_Status != 0)
               return 1;
           else
               return 0;

# endif
       }

        //###################################################################################################################################
        // Gets GPIO values from high byte

       public byte I2C_GetGPIOValuesHigh()
       {
            NumBytesToSend = 0;

#if (FT4232H)
                return 1;       // no high byte on FT4232H
# else

           MPSSEbuffer[NumBytesToSend++] = 0x83;	        // ACbus read GPIO command
           MPSSEbuffer[NumBytesToSend++] = 0x87;            // Send answer back immediate command

           I2C_Status = Send_Data(NumBytesToSend);
           if (I2C_Status != 0)
               return 1;

           I2C_Status = Receive_Data(1);
           if (I2C_Status != 0)
               return 1;

           ACbusReadVal = (byte)(InputBuffer2[0]);      // Return via global variable for calling function to read

           return 0;
# endif
       }


        //###################################################################################################################################
        //###################################################################################################################################
        //##################                                          D2xx Layer                                        #####################
        //###################################################################################################################################
        //###################################################################################################################################
                

        // Read a specified number of bytes from the driver receive buffer

        private byte Receive_Data(uint BytesToRead)
        {
            uint NumBytesInQueue = 0;
            uint QueueTimeOut = 0;
            uint Buffer1Index = 0;
            uint Buffer2Index = 0;
            uint TotalBytesRead = 0;
            bool QueueTimeoutFlag = false;
            uint NumBytesRxd = 0;

            // Keep looping until all requested bytes are received or we've tried 5000 times (value can be chosen as required)
            while ((TotalBytesRead < BytesToRead) && (QueueTimeoutFlag == false))
            {
                ftStatus = myFtdiDevice.GetRxBytesAvailable(ref NumBytesInQueue);       // Check bytes available

                if ((NumBytesInQueue > 0) && (ftStatus == FTDI.FT_STATUS.FT_OK))
                {
                    ftStatus = myFtdiDevice.Read(InputBuffer, NumBytesInQueue, ref NumBytesRxd);  // if any available read them

                    if ((NumBytesInQueue == NumBytesRxd) && (ftStatus == FTDI.FT_STATUS.FT_OK))
                    {
                        Buffer1Index = 0;

                        while (Buffer1Index < NumBytesRxd)
                        {
                            InputBuffer2[Buffer2Index] = InputBuffer[Buffer1Index];     // copy into main overall application buffer
                            Buffer1Index++;
                            Buffer2Index++;
                        }
                        TotalBytesRead = TotalBytesRead + NumBytesRxd;                  // Keep track of total
                    }
                    else
                        return 1;

                    QueueTimeOut++;
                    if (QueueTimeOut == 5000)
                        QueueTimeoutFlag = true;
                    else
                        Thread.Sleep(0);                                                // Avoids running Queue status checks back to back
                }
            }
            // returning globals NumBytesRead and the buffer InputBuffer2
            NumBytesRead = TotalBytesRead;

            if (QueueTimeoutFlag == true)
                return 1;
            else
                return 0;
        }
        
        //###################################################################################################################################
        // Write a buffer of data and check that it got sent without error

        private byte Send_Data(uint BytesToSend)
        {

            NumBytesToSend = BytesToSend;

            // Send data. This will return once all sent or if times out
            ftStatus = myFtdiDevice.Write(MPSSEbuffer, NumBytesToSend, ref NumBytesSent);

            // Ensure that call completed OK and that all bytes sent as requested
            if ((NumBytesSent != NumBytesToSend) || (ftStatus != FTDI.FT_STATUS.FT_OK))
                return 1;   // error   calling function can check NumBytesSent to see how many got sent
            else
                return 0;   // success
        }
        
        //###################################################################################################################################
        // Flush drivers receive buffer - Get queue status and read everything available and discard data

        private byte FlushBuffer()
        {
            ftStatus = myFtdiDevice.GetRxBytesAvailable(ref BytesAvailable);	 // Get the number of bytes in the receive buffer
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
                return 1;
            
            if(BytesAvailable > 0)
            {
                ftStatus = myFtdiDevice.Read(InputBuffer, BytesAvailable, ref NumBytesRead);  	//Read out the data from receive buffer
                if (ftStatus != FTDI.FT_STATUS.FT_OK)
                    return 1;       // error
                else
                    return 0;       // all bytes successfully read
            }
            else
            {
                return 0;           // there were no bytes to read
            }
        }

    }
}