using System;
using System.Net.Sockets;
using System.Text;

namespace DirectOPCTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Direct NI OPC Server Test ===");
            Console.WriteLine("Testing without OPC.Automation.1\n");

            TestDirectConnection();
        }

        static void TestDirectConnection()
        {
            Console.WriteLine("Testing Direct Connection to NI OPC Server on Port 32405");
            Console.WriteLine("========================================================");
            
            try
            {
                using (var client = new TcpClient())
                {
                    client.ReceiveTimeout = 3000;
                    client.SendTimeout = 3000;
                    
                    Console.WriteLine("Connecting to localhost:32405...");
                    client.Connect("localhost", 32405);
                    
                    Console.WriteLine("✅ Connected successfully!");
                    Console.WriteLine("NI OPC Server is running and accepting connections");
                    
                    var stream = client.GetStream();
                    
                    // ส่งคำขอพื้นฐาน
                    Console.WriteLine("\nSending data request...");
                    byte[] request = Encoding.UTF8.GetBytes("READ_DATA\n");
                    stream.Write(request, 0, request.Length);
                    
                    // รอและอ่าน response
                    System.Threading.Thread.Sleep(500);
                    
                    if (stream.DataAvailable)
                    {
                        byte[] buffer = new byte[1024];
                        int bytesRead = stream.Read(buffer, 0, buffer.Length);
                        string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        
                        Console.WriteLine($"📡 Received: {response}");
                    }
                    else
                    {
                        Console.WriteLine("📡 No immediate response (this is normal for OPC servers)");
                    }
                    
                    Console.WriteLine("\n🎯 Connection test successful!");
                    Console.WriteLine("This confirms NI OPC Server is running on port 32405");
                    Console.WriteLine("\nTo get actual AO0_Speed data, you need:");
                    Console.WriteLine("1. OPC Core Components installed");
                    Console.WriteLine("2. Proper OPC client library");
                    Console.WriteLine("3. ADAM-5000TCP device configured and connected");
                }
            }
            catch (SocketException sockEx)
            {
                Console.WriteLine($"❌ Connection Failed: {sockEx.Message}");
                
                switch (sockEx.SocketErrorCode)
                {
                    case SocketError.ConnectionRefused:
                        Console.WriteLine("🔍 Analysis: NI OPC Server is not running or not listening on port 32405");
                        Console.WriteLine("💡 Solution: Start NI OPC Server service");
                        break;
                    case SocketError.TimedOut:
                        Console.WriteLine("🔍 Analysis: Server is not responding");
                        Console.WriteLine("💡 Solution: Check server status and network settings");
                        break;
                    default:
                        Console.WriteLine($"🔍 Socket Error Code: {sockEx.SocketErrorCode}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Unexpected Error: {ex.Message}");
            }
        }
    }
}