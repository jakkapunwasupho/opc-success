using System;
using System.Net.Sockets;
using System.Threading;

namespace SpeedReader
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Real-time Speed Reader ===");
            Console.WriteLine("Reading speed values from register 40 in real-time");
            Console.WriteLine("Press Ctrl+C to stop\n");

            ReadSpeedRealtime();
        }

        static void ReadSpeedRealtime()
        {
            string adamIP = "localhost";
            int adamPort = 502;
            ushort targetRegister = 40;

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
                        
                        ushort rawValue = ReadRegister(stream, targetRegister);
                        double speed = ConvertRawToSpeed(rawValue);
                        
                        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Raw: {rawValue,4} | Speed: {speed:F2}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ❌ Error: {ex.Message}");
                }
                
                Thread.Sleep(1000);
            }
        }
        
        static double ConvertRawToSpeed(ushort rawValue)
        {
            // Based on observed data: Speed 2.0 = Raw 819
            // So conversion factor: Speed = Raw / 409.5
            return rawValue / 409.5;
        }
        
        static ushort ReadRegister(NetworkStream stream, ushort register)
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