using System;
using System.Net.Sockets;
using System.Threading;

namespace WriteBroadcast
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Write Value 1 to Multiple Registers ===");
            Console.WriteLine("Writing to various registers to find the correct one for OPC Quick Client\n");

            WriteBroadcast();
        }

        static void WriteBroadcast()
        {
            string adamIP = "localhost";
            int adamPort = 502;
            ushort valueToWrite = 1;

            // ลองหลายๆ register ที่อาจเป็นได้
            ushort[] possibleRegisters = { 
                0, 1, 2, 3, 4, 5,           // Basic registers 0-5
                40000, 40001, 40002,        // 40001-40003 in 1-based
                40040, 40041, 40042,        // 400041-400043 in 1-based
                30000, 30001, 30002,        // Input registers
                10000, 10001, 10002         // Other possibilities
            };

            try
            {
                using (var client = new TcpClient())
                {
                    client.ReceiveTimeout = 2000;
                    client.SendTimeout = 2000;
                    client.Connect(adamIP, adamPort);
                    
                    var stream = client.GetStream();
                    
                    Console.WriteLine($"🔄 Writing value {valueToWrite} to multiple registers...\n");
                    
                    foreach (ushort register in possibleRegisters)
                    {
                        bool success = WriteRegister(stream, register, valueToWrite);
                        string status = success ? "✅ SUCCESS" : "❌ FAILED";
                        int basedRegister = register + 1; // Show 1-based address
                        Console.WriteLine($"Register {register} (1-based: {basedRegister}): {status}");
                        
                        Thread.Sleep(50); // พักเล็กน้อยระหว่าง write
                    }
                    
                    Console.WriteLine($"\n✅ Written value {valueToWrite} to all possible registers!");
                    Console.WriteLine("🔍 Check OPC Quick Client - it should show Speed = 1 now");
                    Console.WriteLine("📍 If it changed, note which register worked");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
        }
        
        static bool WriteRegister(NetworkStream stream, ushort register, ushort value)
        {
            try
            {
                // Function Code 06 (Write Single Register)
                byte[] request = {
                    0x00, 0x01, // Transaction ID
                    0x00, 0x00, // Protocol ID
                    0x00, 0x06, // Length
                    0x01,       // Unit ID
                    0x06,       // Function Code
                    (byte)(register >> 8), (byte)(register & 0xFF), // Register address
                    (byte)(value >> 8), (byte)(value & 0xFF) // Value
                };
                
                stream.Write(request, 0, request.Length);
                
                byte[] response = new byte[256];
                int bytesRead = stream.Read(response, 0, response.Length);
                
                if (bytesRead >= 12 && response[7] == 0x06)
                {
                    ushort echoRegister = (ushort)((response[8] << 8) | response[9]);
                    ushort echoValue = (ushort)((response[10] << 8) | response[11]);
                    return echoRegister == register && echoValue == value;
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}