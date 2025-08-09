using System;
using System.Net.Sockets;

namespace WriteOnce
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Write Value 1 to ADAM-5024 ===");
            Console.WriteLine("Writing to register 400041 for OPC Quick Client to see\n");

            WriteValueOnce();
        }

        static void WriteValueOnce()
        {
            string adamIP = "localhost";
            int adamPort = 502;
            ushort targetRegister = 40040; // 400041 in 1-based
            ushort valueToWrite = 1;

            try
            {
                using (var client = new TcpClient())
                {
                    client.ReceiveTimeout = 2000;
                    client.SendTimeout = 2000;
                    client.Connect(adamIP, adamPort);
                    
                    var stream = client.GetStream();
                    
                    Console.WriteLine($"✍️ Writing value {valueToWrite} to register 400041...");
                    
                    // Function Code 06 (Write Single Register)
                    byte[] request = {
                        0x00, 0x01, // Transaction ID
                        0x00, 0x00, // Protocol ID
                        0x00, 0x06, // Length
                        0x01,       // Unit ID
                        0x06,       // Function Code
                        (byte)(targetRegister >> 8), (byte)(targetRegister & 0xFF), // Register address
                        (byte)(valueToWrite >> 8), (byte)(valueToWrite & 0xFF) // Value
                    };
                    
                    stream.Write(request, 0, request.Length);
                    
                    byte[] response = new byte[256];
                    int bytesRead = stream.Read(response, 0, response.Length);
                    
                    if (bytesRead >= 12 && response[7] == 0x06)
                    {
                        ushort echoRegister = (ushort)((response[8] << 8) | response[9]);
                        ushort echoValue = (ushort)((response[10] << 8) | response[11]);
                        
                        if (echoRegister == targetRegister && echoValue == valueToWrite)
                        {
                            Console.WriteLine($"✅ SUCCESS! Written value {valueToWrite} to register 400041");
                            Console.WriteLine($"🔍 OPC Quick Client should now show Speed = {valueToWrite}");
                        }
                        else
                        {
                            Console.WriteLine($"❌ Write failed - Echo mismatch");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"❌ Invalid response from ADAM-5024");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
            
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}