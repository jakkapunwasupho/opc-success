using System;
using System.Net.Sockets;

namespace SetSpeed
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== ADAM-5024 Speed Setter ===");
            Console.WriteLine("Sets Speed value in OPC Quick Client via register 40 (41 in 1-based)");
            Console.WriteLine("Usage: Enter desired Speed value (e.g., 1, 2, 3...)");
            Console.WriteLine("Press 'q' to quit\n");

            SetSpeedInteractive();
        }

        static void SetSpeedInteractive()
        {
            string adamIP = "localhost";
            int adamPort = 502;
            ushort targetRegister = 40; // The correct register for OPC Quick Client

            while (true)
            {
                try
                {
                    Console.Write("Enter Speed value (or 'q' to quit): ");
                    string input = Console.ReadLine()?.Trim() ?? "";
                    
                    if (input.ToLower() == "q") break;
                    
                    if (float.TryParse(input, out float desiredSpeed))
                    {
                        // Calculate raw value based on observed scaling
                        // Speed 2.0 = Raw 819, so scaling factor ≈ 409.5 per unit
                        ushort rawValue = (ushort)(desiredSpeed * 409.5);
                        
                        Console.WriteLine($"🎯 Setting Speed = {desiredSpeed} (Raw value = {rawValue})");
                        
                        using (var client = new TcpClient())
                        {
                            client.ReceiveTimeout = 2000;
                            client.SendTimeout = 2000;
                            client.Connect(adamIP, adamPort);
                            
                            var stream = client.GetStream();
                            
                            bool success = WriteRegister(stream, targetRegister, rawValue);
                            
                            if (success)
                            {
                                Console.WriteLine($"✅ SUCCESS! Speed set to {desiredSpeed}");
                                Console.WriteLine($"🔍 Check OPC Quick Client - it should show Speed ≈ {desiredSpeed}");
                            }
                            else
                            {
                                Console.WriteLine($"❌ FAILED to set Speed");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("❌ Invalid input. Please enter a number.");
                    }
                    
                    Console.WriteLine();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error: {ex.Message}\n");
                }
            }
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
    }
}