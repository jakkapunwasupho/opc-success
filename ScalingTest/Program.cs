using System;
using System.Net.Sockets;

namespace ScalingTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== OPC Quick Client Scaling Factor Test ===");
            Console.WriteLine("Found that register 40 (41 in 1-based) affects OPC Quick Client");
            Console.WriteLine("Testing different values to determine scaling factor\n");

            TestScaling();
        }

        static void TestScaling()
        {
            string adamIP = "localhost";
            int adamPort = 502;
            ushort targetRegister = 40; // The correct register

            // Test different values to understand scaling
            ushort[] testValues = { 100, 200, 500, 1000, 2000, 5000, 10000 };

            try
            {
                using (var client = new TcpClient())
                {
                    client.ReceiveTimeout = 2000;
                    client.SendTimeout = 2000;
                    client.Connect(adamIP, adamPort);
                    
                    var stream = client.GetStream();
                    
                    Console.WriteLine("🧮 Testing scaling factor:");
                    Console.WriteLine("═══════════════════════════════════════");
                    
                    foreach (ushort testValue in testValues)
                    {
                        bool success = WriteRegister(stream, targetRegister, testValue);
                        
                        if (success)
                        {
                            // Calculate expected OPC value based on previous observation
                            // 5 → 0.01221, so scaling ≈ value × 0.002442
                            double expectedOPC = testValue * 0.002442;
                            
                            Console.WriteLine($"✍️  Raw Value: {testValue,5} → Expected OPC: {expectedOPC:F5}");
                            Console.WriteLine($"   🔍 Check OPC Quick Client for this value");
                            
                            System.Threading.Thread.Sleep(500); // Short pause
                        }
                    }
                    
                    Console.WriteLine("\n📊 Analysis:");
                    Console.WriteLine("If scaling factor is consistent, we can predict:");
                    Console.WriteLine("- To get Speed = 2.0 in OPC → Raw value ≈ 819");
                    Console.WriteLine("- To get Speed = 1.0 in OPC → Raw value ≈ 409"); 
                    
                    Console.WriteLine("\n🎯 Let's test writing 819 to get Speed ≈ 2.0:");
                    bool finalTest = WriteRegister(stream, targetRegister, 819);
                    
                    if (finalTest)
                    {
                        Console.WriteLine("✅ Written 819 to register 40");
                        Console.WriteLine("🔍 OPC Quick Client should now show Speed ≈ 2.0");
                    }
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