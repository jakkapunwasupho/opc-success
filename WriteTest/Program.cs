using System;
using System.Net.Sockets;
using System.Threading;

namespace WriteTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== ADAM-5024 Write/Read Test ===");
            Console.WriteLine("Testing writing value 1 to register 400041, then reading it back");
            Console.WriteLine("Press Ctrl+C to stop\n");

            WriteAndReadTest();
        }

        static void WriteAndReadTest()
        {
            string adamIP = "localhost";
            int adamPort = 502;
            ushort targetRegister = 40040; // 400041 in 1-based = 40040 in 0-based
            ushort valueToWrite = 1;

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
                        
                        Console.WriteLine($"🔄 Test Cycle at {DateTime.Now:HH:mm:ss}");
                        Console.WriteLine("═══════════════════════════════════════════");
                        
                        // Step 1: Read current value
                        Console.WriteLine($"📖 Step 1: Reading current value from register {targetRegister} (400041)");
                        ushort currentValue = ReadHoldingRegister(stream, targetRegister);
                        Console.WriteLine($"   Current value: {currentValue}");
                        
                        // Step 2: Write new value
                        Console.WriteLine($"✍️  Step 2: Writing value {valueToWrite} to register {targetRegister} (400041)");
                        bool writeSuccess = WriteHoldingRegister(stream, targetRegister, valueToWrite);
                        Console.WriteLine($"   Write result: {(writeSuccess ? "SUCCESS" : "FAILED")}");
                        
                        if (writeSuccess)
                        {
                            // Step 3: Read back to verify
                            Thread.Sleep(100); // พักเล็กน้อย
                            Console.WriteLine($"🔍 Step 3: Reading back to verify");
                            ushort verifyValue = ReadHoldingRegister(stream, targetRegister);
                            Console.WriteLine($"   Verified value: {verifyValue}");
                            
                            if (verifyValue == valueToWrite)
                            {
                                Console.WriteLine($"✅ SUCCESS! Value {valueToWrite} written and verified!");
                                
                                // แปลงเป็น RPM (ลองหลายวิธี)
                                float rpm1 = verifyValue; // Direct
                                float rpm2 = verifyValue * 0.1f; // * 0.1
                                float rpm3 = verifyValue * 100.0f; // * 100
                                float rpm4 = (verifyValue / 100.0f) * 3000.0f; // Scale to 3000 max
                                
                                Console.WriteLine($"🎯 Speed interpretations:");
                                Console.WriteLine($"   Direct: {rpm1} RPM");
                                Console.WriteLine($"   × 0.1:  {rpm2} RPM");
                                Console.WriteLine($"   × 100:  {rpm3} RPM");
                                Console.WriteLine($"   Scaled: {rpm4:F1} RPM");
                            }
                            else
                            {
                                Console.WriteLine($"❌ MISMATCH! Wrote {valueToWrite} but read {verifyValue}");
                            }
                        }
                        
                        // Test other possible registers
                        Console.WriteLine($"\\n🔍 Testing other register addresses:");
                        TestRegisterAddresses(stream);
                        
                        Console.WriteLine();
                    }
                    
                    Thread.Sleep(5000); // รอ 5 วินาที
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error: {ex.Message}");
                    Thread.Sleep(3000);
                }
            }
        }
        
        static ushort ReadHoldingRegister(NetworkStream stream, ushort register)
        {
            try
            {
                // Function Code 03 (Read Holding Registers)
                byte[] request = {
                    0x00, 0x01, // Transaction ID
                    0x00, 0x00, // Protocol ID
                    0x00, 0x06, // Length
                    0x01,       // Unit ID
                    0x03,       // Function Code
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
        
        static bool WriteHoldingRegister(NetworkStream stream, ushort register, ushort value)
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
                
                // Success if we get echo back
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
        
        static void TestRegisterAddresses(NetworkStream stream)
        {
            ushort[] testRegisters = { 0, 1, 2, 3, 4, 5, 40040, 40041, 40042 };
            
            foreach (ushort reg in testRegisters)
            {
                ushort value = ReadHoldingRegister(stream, reg);
                if (value > 0)
                {
                    Console.WriteLine($"   🎯 Register {reg}: {value} ⭐ NON-ZERO");
                }
                else
                {
                    Console.WriteLine($"   📍 Register {reg}: {value}");
                }
            }
        }
    }
}