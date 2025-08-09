using System;
using System.Net.Sockets;

namespace TestRegister40
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Test Register 40 (41 in 1-based) ===");
            Console.WriteLine("This register has value 819 - might be the OPC Quick Client source");
            Console.WriteLine("Writing value 5 to test if OPC Quick Client changes\n");

            TestRegister40();
        }

        static void TestRegister40()
        {
            string adamIP = "localhost";
            int adamPort = 502;
            ushort targetRegister = 40; // Register 40 (41 in 1-based) that has value 819
            ushort valueToWrite = 5;

            try
            {
                using (var client = new TcpClient())
                {
                    client.ReceiveTimeout = 2000;
                    client.SendTimeout = 2000;
                    client.Connect(adamIP, adamPort);
                    
                    var stream = client.GetStream();
                    
                    // Step 1: Read current value
                    Console.WriteLine($"📖 Step 1: Reading current value from register {targetRegister} (1-based: {targetRegister + 1})");
                    ushort currentValue = ReadRegister(stream, targetRegister);
                    Console.WriteLine($"   Current value: {currentValue}");
                    
                    // Step 2: Write new value
                    Console.WriteLine($"✍️  Step 2: Writing value {valueToWrite} to register {targetRegister}");
                    bool writeSuccess = WriteRegister(stream, targetRegister, valueToWrite);
                    Console.WriteLine($"   Write result: {(writeSuccess ? "SUCCESS" : "FAILED")}");
                    
                    if (writeSuccess)
                    {
                        // Step 3: Read back to verify
                        System.Threading.Thread.Sleep(100);
                        Console.WriteLine($"🔍 Step 3: Reading back to verify");
                        ushort verifyValue = ReadRegister(stream, targetRegister);
                        Console.WriteLine($"   Verified value: {verifyValue}");
                        
                        if (verifyValue == valueToWrite)
                        {
                            Console.WriteLine($"✅ SUCCESS! Value {valueToWrite} written and verified!");
                            Console.WriteLine($"🔍 Check OPC Quick Client NOW - it should show Speed = {valueToWrite}");
                            Console.WriteLine($"📍 If it changed, then register {targetRegister} (1-based: {targetRegister + 1}) is the correct one!");
                        }
                        else
                        {
                            Console.WriteLine($"❌ MISMATCH! Wrote {valueToWrite} but read {verifyValue}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
            
            Console.WriteLine("\nPress any key to continue...");
            try { Console.ReadKey(); } catch { }
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