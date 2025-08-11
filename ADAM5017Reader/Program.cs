using System;
using System.Net.Sockets;
using System.Threading;

namespace ADAM5017Reader
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== ADAM-5017P1 Real-time Reader ===");
            Console.WriteLine("Reading all 8 analog input channels from ADAM5000TCP.ADAM-5017P1");
            Console.WriteLine("Press Ctrl+C to stop\n");

            ReadADAM5017Realtime();
        }

        static void ReadADAM5017Realtime()
        {
            string adamIP = "localhost";
            int adamPort = 502;
            
            // ADAM-5017P1 Holding Registers - from OPC Client config (400025-400032)
            // AI0_Barometer, AI1_Humidity, AI2_Temp, AI3_Pressure, AI4_Smoke, AI5, AI6, AI7
            ushort[] registers = { 24, 25, 26, 27, 28, 29, 30, 31 }; // 400025-400032 = Modbus address 24-31
            string[] channelNames = { 
                "AI0_Barometer", 
                "AI1_Humidity", 
                "AI2_Temp", 
                "AI3_Pressure", 
                "AI4_Smoke", 
                "AI5", 
                "AI6", 
                "AI7" 
            };

            while (true)
            {
                try
                {
                    using (var client = new TcpClient())
                    {
                        client.ReceiveTimeout = 2000;
                        client.SendTimeout = 2000;
                        client.Connect(adamIP, adamPort);
                        
                        var stream = client.GetStream();
                        
                        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]");
                        
                        for (int i = 0; i < registers.Length; i++)
                        {
                            // Use Holding Registers (0x03) as per OPC config
                            ushort rawValue = ReadHoldingRegister(stream, registers[i]);
                            double scaledValue = ConvertRawToScaled(rawValue, i);
                            string unit = GetChannelUnit(i);
                            
                            Console.WriteLine($"  {channelNames[i],-15}: Raw={rawValue,5} | Value={scaledValue:F3}{unit}");
                        }
                        
                        Console.WriteLine();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ❌ Error: {ex.Message}");
                }
                
                Thread.Sleep(2000); // Read every 2 seconds
            }
        }
        
        static double ConvertRawToScaled(ushort rawValue, int channelIndex)
        {
            // Based on OPC Client scaling configuration per channel
            switch (channelIndex)
            {
                case 0: // AI0_Barometer: 0-65535 → 0-10
                    return (rawValue / 65535.0) * 10.0;
                    
                case 1: // AI1_Humidity: 0-65535 → 4-20 
                case 2: // AI2_Temp: 0-65535 → 4-20
                case 3: // AI3_Pressure: 0-65535 → 4-20
                    return 4.0 + (rawValue / 65535.0) * 16.0; // 4 + (raw/65535) * (20-4)
                    
                case 4: // AI4_Smoke: 0-65535 → 0-10 (assuming same as barometer)
                    return (rawValue / 65535.0) * 10.0;
                    
                default: // AI5, AI6, AI7: Default to 0-10
                    return (rawValue / 65535.0) * 10.0;
            }
        }
        
        static string GetChannelUnit(int channelIndex)
        {
            switch (channelIndex)
            {
                case 0: return " bar";     // Barometer
                case 1: return " %RH";     // Humidity
                case 2: return " °C";      // Temperature
                case 3: return " Pa";      // Pressure  
                case 4: return " ppm";     // Smoke
                default: return " V";      // Voltage
            }
        }
        
        static ushort ReadInputRegister(NetworkStream stream, ushort register)
        {
            try
            {
                byte[] request = {
                    0x00, 0x01, // Transaction ID
                    0x00, 0x00, // Protocol ID
                    0x00, 0x06, // Length
                    0x01,       // Unit ID
                    0x04,       // Function Code (Read Input Registers)
                    (byte)(register >> 8), (byte)(register & 0xFF), // Register address
                    0x00, 0x01  // Quantity (1 register)
                };
                
                stream.Write(request, 0, request.Length);
                
                byte[] response = new byte[256];
                int bytesRead = stream.Read(response, 0, response.Length);
                
                if (bytesRead >= 11 && response[7] == 0x04 && response[8] == 0x02)
                {
                    return (ushort)((response[9] << 8) | response[10]);
                }
                
                return 0;
            }
            catch
            {
                return 0;
            }
        }
        
        static ushort ReadHoldingRegister(NetworkStream stream, ushort register)
        {
            try
            {
                byte[] request = {
                    0x00, 0x01, // Transaction ID
                    0x00, 0x00, // Protocol ID
                    0x00, 0x06, // Length
                    0x01,       // Unit ID
                    0x03,       // Function Code (Read Holding Registers)
                    (byte)(register >> 8), (byte)(register & 0xFF), // Register address
                    0x00, 0x01  // Quantity (1 register)
                };
                
                stream.Write(request, 0, request.Length);
                
                byte[] response = new byte[256];
                int bytesRead = stream.Read(response, 0, response.Length);
                
                if (bytesRead >= 11 && response[7] == 0x03 && response[8] == 0x02)
                {
                    return (ushort)((response[9] << 8) | response[10]);
                }
                
                return 0;
            }
            catch
            {
                return 0;
            }
        }
    }
}