﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.DirectX.DirectInput;

namespace UDB_FlyByWire
{
    class JoystickHandler
    {
        const string PacketHeader = "FbW";
        const int PacketLength = 13;
        public int CenterX = 0;
        public int CenterY = 0;
        public bool InvertX = false;
        public bool InvertY = false;
        public bool InvertRudder = false;
        public bool InvertThrottle = false;

        public class FbW_Data
        {
            public string m_header;
            public int m_aileron;
            public int m_elevator;
            public int m_rudder;
            public int m_throttle;
            public int m_mode;
            public bool m_hasChanged;

            public FbW_Data()
            {
                m_header = PacketHeader;
                m_aileron = 0;
                m_elevator = 0;
                m_rudder = 0;
                m_throttle = 0;
                m_mode = 0;
                m_hasChanged = true;
            }

            public FbW_Data(int aileron, int elevator, int rudder, int throttle, int mode)
            {
                m_header = PacketHeader;
                m_aileron = aileron;
                m_elevator = elevator;
                m_rudder = rudder;
                m_throttle = throttle;
                m_mode = mode;
                m_hasChanged = true;
            }

        }


        public FbW_Data ConvertToPercent(MainForm.PlaneAttributes planeState)
        {
            FbW_Data dataPercent = new FbW_Data();

            const int maxValue = 65535;
            // values are 0 to 65535

            if (InvertThrottle)
                planeState.throttle = maxValue - planeState.throttle; // z ==  throttle on my joystick, and inverted

            // offset to +-/ 32k
            planeState.aileron -= CenterX;
            planeState.elevator -= CenterY;

            // translate to +/- 100%
            planeState.aileron = (planeState.aileron * 100) / (maxValue / 2);
            planeState.elevator = (planeState.elevator * 100) / (maxValue / 2);
            planeState.rudder = (planeState.rudder * 100) / (maxValue) / 2;
            planeState.throttle = (planeState.throttle * 100) / (maxValue);

            if (InvertX)
                planeState.aileron = -planeState.aileron;
            if (InvertY)
                planeState.elevator = -planeState.elevator;
            if (InvertRudder) // rudder
                planeState.rudder = -planeState.rudder;

            // we don't really need to clip here
            //x = MainForm.Clip(x, -100, 100);
            //y = MainForm.Clip(y, -100, 100);
            //rudder = MainForm.Clip(rudder, -100, 100);
            //throttle = MainForm.Clip(throttle, 0, 100);


            dataPercent.m_aileron = planeState.aileron;
            dataPercent.m_elevator = planeState.elevator;
            dataPercent.m_throttle = planeState.throttle;
            dataPercent.m_rudder = planeState.rudder;

            dataPercent.m_mode = 0;

            return dataPercent;
        }
  
        static public FbW_Data ConvertToPWM(FbW_Data dataPercent, int modeIndex)
        {
            // UDB PWM values are 2200 to 3800
            // center is 3000 with +/- 800

            FbW_Data dataPWM = new FbW_Data();

            dataPWM.m_aileron = (ushort)((dataPercent.m_aileron * 8) + 3000);
            dataPWM.m_elevator = (ushort)((dataPercent.m_elevator * 8) + 3000);
            dataPWM.m_throttle = (ushort)((dataPercent.m_throttle * 8) + 3000);
            dataPWM.m_rudder = (ushort)((dataPercent.m_rudder * 8) + 3000);

            switch (modeIndex)
            {
                default:
                case 0: dataPWM.m_mode = 2400; break; // Manual
                case 1: dataPWM.m_mode = 3000; break; // Stabilized
                case 2: dataPWM.m_mode = 3600; break; // WayPoint
            }
            return dataPWM;
        }

        public void ParseRxPacket(byte[] packet)
        {
            // we dont care about Rx data
        }

        static public byte[] CreateTxPacket(FbW_Data PWMdata)
        {
            byte[] packet = new byte[PacketLength];
            int i = 0;

            packet[i++] = (byte)PWMdata.m_header[0];
            packet[i++] = (byte)PWMdata.m_header[1];
            packet[i++] = (byte)PWMdata.m_header[2];

            packet[i++] = (byte)(PWMdata.m_aileron); // LSB first
            packet[i++] = (byte)(PWMdata.m_aileron >> 8);
            packet[i++] = (byte)(PWMdata.m_elevator);
            packet[i++] = (byte)(PWMdata.m_elevator >> 8);
            packet[i++] = (byte)(PWMdata.m_mode);
            packet[i++] = (byte)(PWMdata.m_mode >> 8);
            packet[i++] = (byte)(PWMdata.m_rudder);
            packet[i++] = (byte)(PWMdata.m_rudder >> 8);
            packet[i++] = (byte)(PWMdata.m_throttle);
            packet[i++] = (byte)(PWMdata.m_throttle >> 8);

            return packet;
        }

        static public byte CreateChecksum(byte[] packet)
        {
            byte checksum = 0;

            for (int i = 0; i < packet.Length; i++)
                checksum += packet[i];
            return checksum;
        }



    }
}