using System;
using System.Net.Sockets;
using System.Threading;

namespace InputRegisterTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== ADAM-5024 Input Register Test ===");
            Console.WriteLine("Testing Function Code 04 (Read Input Registers)");
            Console.WriteLine("Testing register 400041 with different function codes");
            Console.WriteLine("Press Ctrl+C to stop\n");

            TestInputRegisters();
        }

        static void TestInputRegisters()
        {
            string adamIP = "localhost";
            int adamPort = 502;

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
                        
                        Console.WriteLine($"🔍 Testing at {DateTime.Now:HH:mm:ss}");
                        Console.WriteLine("═══════════════════════════════════════════");
                        
                        // Test 1: Function Code 04 (Read Input Registers) - register 40040 (400041)
                        Console.WriteLine("📡 Test 1: Function Code 04 - Input Registers");
                        TestModbusFunction(stream, 0x04, 40040, 2, "Input Reg 40040-40041");
                        
                        // Test 2: Function Code 03 (Read Holding Registers) - register 40040 (400041)
                        Console.WriteLine("📡 Test 2: Function Code 03 - Holding Registers");
                        TestModbusFunction(stream, 0x03, 40040, 2, "Holding Reg 40040-40041");
                        
                        // Test 3: Function Code 04 - register 0-1
                        Console.WriteLine("📡 Test 3: Function Code 04 - Input Registers 0-1");
                        TestModbusFunction(stream, 0x04, 0, 2, "Input Reg 0-1");
                        
                        // Test 4: Function Code 01 (Read Coils) - coil 0-15
                        Console.WriteLine("📡 Test 4: Function Code 01 - Coils 0-15");
                        TestModbusFunction(stream, 0x01, 0, 16, "Coils 0-15");
                        
                        // Test 5: Function Code 02 (Read Discrete Inputs) - input 0-15
                        Console.WriteLine("📡 Test 5: Function Code 02 - Discrete Inputs 0-15");
                        TestModbusFunction(stream, 0x02, 0, 16, "Discrete Input 0-15");
                        
                        Console.WriteLine();
                    }
                    
                    Thread.Sleep(3000); // รอ 3 วินาที
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Connection Error: {ex.Message}");
                    Thread.Sleep(3000);
                }
            }
        }

        static void TestModbusFunction(NetworkStream stream, byte functionCode, ushort startAddress, ushort quantity, string description)
        {
            try
            {
                byte[] modbusRequest = {
                    0x00, 0x01, // Transaction ID
                    0x00, 0x00, // Protocol ID
                    0x00, 0x06, // Length
                    0x01,       // Unit ID
                    functionCode, // Function Code
                    (byte)(startAddress >> 8), (byte)(startAddress & 0xFF), // Starting Address
                    (byte)(quantity >> 8), (byte)(quantity & 0xFF)  // Quantity
                };
                
                stream.Write(modbusRequest, 0, modbusRequest.Length);
                
                byte[] response = new byte[256];
                int bytesRead = stream.Read(response, 0, response.Length);
                
                string rawHex = BitConverter.ToString(response, 0, Math.Min(16, bytesRead));
                Console.WriteLine($"   📍 {description}: {rawHex}");
                
                if (bytesRead >= 9 && response[7] == functionCode)
                {
                    int dataLength = response[8];
                    Console.WriteLine($"   📊 Data Length: {dataLength} bytes");
                    
                    // Parse data based on function code
                    if (functionCode == 0x03 || functionCode == 0x04) // Register functions
                    {
                        for (int i = 0; i < dataLength; i += 2)
                        {
                            if (i + 10 < bytesRead)
                            {
                                ushort regValue = (ushort)((response[9 + i] << 8) | response[10 + i]);
                                Console.WriteLine($"   🔢 Register {startAddress + (i/2)}: {regValue} (0x{regValue:X4})");
                                
                                if (regValue > 0)
                                {
                                    Console.WriteLine($"   🎉 NON-ZERO VALUE FOUND! {regValue}");
                                    
                                    // แปลงเป็น engineering unit
                                    float speed = regValue * 0.1f; // ลองหลายวิธี
                                    float speed2 = (regValue / 100.0f) * 3000.0f;
                                    Console.WriteLine($"   🎯 Possible Speed: {speed} RPM or {speed2:F1} RPM");
                                }
                            }
                        }
                    }
                    else if (functionCode == 0x01 || functionCode == 0x02) // Bit functions
                    {
                        for (int i = 0; i < dataLength; i++)
                        {
                            byte bitValue = response[9 + i];
                            Console.WriteLine($"   🔘 Byte {i}: 0x{bitValue:X2} ({Convert.ToString(bitValue, 2).PadLeft(8, '0')})");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"   ❌ Invalid response or error code");
                }
                
                Thread.Sleep(200); // พักระหว่าง request
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Function {functionCode:X2} failed: {ex.Message}");
            }
        }
    }
}