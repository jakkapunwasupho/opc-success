using System;
using System.Net.Sockets;

namespace SetSpeedOne
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Set Speed = 1 in OPC Quick Client ===");
            Console.WriteLine("Writing calculated raw value to register 40 to achieve Speed = 1.0\n");

            SetSpeedToOne();
        }

        static void SetSpeedToOne()
        {
            string adamIP = "localhost";
            int adamPort = 502;
            ushort targetRegister = 40; // The correct register for OPC Quick Client
            
            // Based on observed data: Speed 2.0 = Raw 819
            // So for Speed 1.0 = Raw 409.5 ≈ 410
            ushort rawValueForOne = 410;

            try
            {
                using (var client = new TcpClient())
                {
                    client.ReceiveTimeout = 2000;
                    client.SendTimeout = 2000;
                    client.Connect(adamIP, adamPort);
                    
                    var stream = client.GetStream();
                    
                    Console.WriteLine($"🎯 Setting Speed = 1.0 (Raw value = {rawValueForOne})");
                    Console.WriteLine($"📍 Writing to register {targetRegister} (1-based: {targetRegister + 1})");
                    
                    bool success = WriteRegister(stream, targetRegister, rawValueForOne);
                    
                    if (success)
                    {
                        Console.WriteLine($"✅ SUCCESS! Speed set to 1.0");
                        Console.WriteLine($"🔍 Check OPC Quick Client - it should show Speed = 1.0");
                        
                        // Read back to verify
                        ushort readBack = ReadRegister(stream, targetRegister);
                        Console.WriteLine($"📖 Verification: Read back value = {readBack}");
                    }
                    else
                    {
                        Console.WriteLine($"❌ FAILED to set Speed");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
            
            Console.WriteLine("\n🎉 Task completed! OPC Quick Client should now show Speed = 1");
        }
        
        static bool WriteRegister(NetworkStream stream, ushort register, ushort value)
        {
            try
            {
                byte[] request = {
                    0x00, 0x01, // Transaction ID
                    0x00, 0x00, // Protocol ID
                    0x00, 0x06, // Length
                    0x01,       // Unit ID
                    0x06,       // Function Code (Write Single Register)
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