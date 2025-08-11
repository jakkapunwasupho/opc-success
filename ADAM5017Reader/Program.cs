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
            
            // ADAM-5017P1 Input Registers (based on the image)
            // AI0_Barometer, AI1_Humidity, AI2_Temp, AI3_Pressure, AI4_Smoke, AI5, AI6, AI7
            ushort[] registers = { 0, 1, 2, 3, 4, 5, 6, 7 };
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
                            ushort rawValue = ReadInputRegister(stream, registers[i]);
                            double voltage = ConvertRawToVoltage(rawValue);
                            
                            Console.WriteLine($"  {channelNames[i],-15}: Raw={rawValue,5} | Voltage={voltage:F3}V");
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
        
        static double ConvertRawToVoltage(ushort rawValue)
        {
            // ADAM-5017 typically has 16-bit resolution for 0-10V range
            // Conversion: Voltage = (RawValue / 65535) * 10V
            return (rawValue / 65535.0) * 10.0;
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
    }
}