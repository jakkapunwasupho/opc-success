using System;
using System.Threading;

namespace RealOpcNetTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Real OPC.NET Connection Test ===");
            Console.WriteLine("Testing with actual OPC libraries to read AO0_Speed\n");

            TestRealOpcConnection();
        }

        static void TestRealOpcConnection()
        {
            Console.WriteLine("Method 1: Using OpcNetApi (if available)");
            Console.WriteLine("========================================");
            
            try
            {
                // ลองใช้ reflection เพื่อหา OPC libraries ที่มี
                TestWithReflection();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Method 1 failed: {ex.Message}");
            }

            Console.WriteLine("\nMethod 2: Direct TCP/IP OPC Connection");
            Console.WriteLine("======================================");
            
            try
            {
                TestDirectTcpOpc();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Method 2 failed: {ex.Message}");
            }

            Console.WriteLine("\nMethod 3: Alternative OPC Approach");
            Console.WriteLine("==================================");
            
            TestAlternativeOpcApproach();
        }

        static void TestWithReflection()
        {
            Console.WriteLine("Searching for OPC libraries in system...");
            
            string[] possibleAssemblies = {
                "OpcNetApi",
                "OpcNetApi.dll", 
                "Opc.Ua.Core",
                "OpcRcw.Core",
                "OpcComRcw"
            };

            foreach (string assemblyName in possibleAssemblies)
            {
                try
                {
                    var assembly = System.Reflection.Assembly.LoadFrom(assemblyName);
                    Console.WriteLine($"✅ Found: {assemblyName}");
                    
                    // ลองใช้ assembly ที่เจอ
                    UseFoundAssembly(assembly);
                    return;
                }
                catch
                {
                    Console.WriteLine($"❌ Not found: {assemblyName}");
                }
            }
            
            Console.WriteLine("No OPC libraries found via reflection");
        }

        static void UseFoundAssembly(System.Reflection.Assembly assembly)
        {
            Console.WriteLine($"Using assembly: {assembly.FullName}");
            
            // ลองหา OPC types ใน assembly
            var types = assembly.GetTypes();
            foreach (var type in types)
            {
                if (type.Name.Contains("Server") || type.Name.Contains("Client"))
                {
                    Console.WriteLine($"   Found type: {type.Name}");
                }
            }
        }

        static void TestDirectTcpOpc()
        {
            Console.WriteLine("Attempting direct TCP connection to OPC endpoint...");
            
            using (var tcpClient = new System.Net.Sockets.TcpClient())
            {
                try
                {
                    // เชื่อมต่อไป NI OPC Server port
                    tcpClient.Connect("localhost", 32405);
                    Console.WriteLine("✅ TCP connection established to port 32405");
                    
                    var stream = tcpClient.GetStream();
                    
                    // ส่ง OPC handshake
                    byte[] opcHandshake = {
                        0x05, 0x00, 0x0b, 0x03, 0x10, 0x00, 0x00, 0x00,
                        0x48, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,
                        0xb8, 0x10, 0xb8, 0x10, 0x00, 0x00, 0x00, 0x00
                    };
                    
                    stream.Write(opcHandshake, 0, opcHandshake.Length);
                    Console.WriteLine("📡 Sent OPC handshake");
                    
                    // อ่าน response
                    byte[] response = new byte[1024];
                    int bytesRead = stream.Read(response, 0, response.Length);
                    
                    if (bytesRead > 0)
                    {
                        Console.WriteLine($"📡 Received {bytesRead} bytes from OPC server");
                        Console.WriteLine($"   Response: {BitConverter.ToString(response, 0, Math.Min(16, bytesRead))}");
                        
                        // ลองส่ง request สำหรับ AO0_Speed
                        SendOpcDataRequest(stream);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ TCP OPC connection failed: {ex.Message}");
                }
            }
        }

        static void SendOpcDataRequest(System.Net.Sockets.NetworkStream stream)
        {
            Console.WriteLine("\nSending OPC data request for ADAM5000TCP.ADAM-5024.AO0_Speed...");
            
            try
            {
                // ส่ง OPC read request (simplified)
                byte[] readRequest = {
                    0x05, 0x00, 0x00, 0x03, 0x10, 0x00, 0x00, 0x00,
                    0x20, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00,
                    0x01, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00
                };
                
                stream.Write(readRequest, 0, readRequest.Length);
                
                // อ่าน response
                byte[] response = new byte[1024];
                int bytesRead = stream.Read(response, 0, response.Length);
                
                if (bytesRead > 0)
                {
                    Console.WriteLine($"📊 Data response: {bytesRead} bytes");
                    
                    // ลองแปลงเป็นข้อมูล
                    ParseOpcDataResponse(response, bytesRead);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Data request failed: {ex.Message}");
            }
        }

        static void ParseOpcDataResponse(byte[] response, int length)
        {
            Console.WriteLine("Parsing OPC data response...");
            
            // ลอง parse แบบง่ายๆ
            for (int i = 0; i < length - 4; i += 4)
            {
                if (i + 4 <= length)
                {
                    float value = BitConverter.ToSingle(response, i);
                    
                    if (!float.IsNaN(value) && !float.IsInfinity(value) && value > 0 && value < 10000)
                    {
                        Console.WriteLine($"   Possible AO0_Speed value: {value:F2}");
                    }
                }
            }
        }

        static void TestAlternativeOpcApproach()
        {
            Console.WriteLine("Testing alternative OPC connection methods...");
            
            // Method A: ใช้ HTTP-based OPC
            TestHttpOpc();
            
            // Method B: ใช้ Named Pipes
            TestNamedPipeOpc();
            
            // Method C: ใช้ Registry-based detection
            TestRegistryOpc();
        }

        static void TestHttpOpc()
        {
            Console.WriteLine("\nTesting HTTP-based OPC connection...");
            
            try
            {
                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    // ลองเชื่อมต่อผ่าน HTTP (บาง OPC Server รองรับ)
                    var response = httpClient.GetAsync("http://localhost:8080/opc").Result;
                    Console.WriteLine($"HTTP OPC response: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"HTTP OPC not available: {ex.Message}");
            }
        }

        static void TestNamedPipeOpc()
        {
            Console.WriteLine("\nTesting Named Pipe OPC connection...");
            
            try
            {
                using (var pipeClient = new System.IO.Pipes.NamedPipeClientStream(".", "OPCServer", System.IO.Pipes.PipeDirection.InOut))
                {
                    pipeClient.Connect(1000); // 1 second timeout
                    Console.WriteLine("✅ Connected via Named Pipe");
                    
                    // ส่ง request
                    byte[] request = System.Text.Encoding.UTF8.GetBytes("READ:ADAM5000TCP.ADAM-5024.AO0_Speed");
                    pipeClient.Write(request, 0, request.Length);
                    
                    // อ่าน response
                    byte[] response = new byte[256];
                    int bytesRead = pipeClient.Read(response, 0, response.Length);
                    string responseText = System.Text.Encoding.UTF8.GetString(response, 0, bytesRead);
                    
                    Console.WriteLine($"📊 Named Pipe response: {responseText}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Named Pipe OPC not available: {ex.Message}");
            }
        }

        static void TestRegistryOpc()
        {
            Console.WriteLine("\nTesting Registry-based OPC detection...");
            
            try
            {
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Classes\National Instruments.NIOPCServers.V5"))
                {
                    if (key != null)
                    {
                        Console.WriteLine("✅ Found NI OPC Server in registry");
                        
                        var clsidKey = key.OpenSubKey("CLSID");
                        if (clsidKey != null)
                        {
                            string clsid = clsidKey.GetValue("")?.ToString() ?? "";
                            Console.WriteLine($"   CLSID: {clsid}");
                            
                            // ลองใช้ CLSID โดยตรง
                            TestDirectClsidAccess(clsid);
                        }
                    }
                    else
                    {
                        Console.WriteLine("❌ NI OPC Server not found in registry");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Registry access failed: {ex.Message}");
            }
        }

        static void TestDirectClsidAccess(string clsid)
        {
            Console.WriteLine($"\nTesting direct CLSID access: {clsid}");
            
            try
            {
                if (!string.IsNullOrEmpty(clsid))
                {
                    var guid = new Guid(clsid);
                    var type = Type.GetTypeFromCLSID(guid);
                    
                    if (type != null)
                    {
                        Console.WriteLine("✅ Type created from CLSID");
                        
                        // ลองสร้าง instance (อาจยัง license error)
                        try
                        {
                            var instance = Activator.CreateInstance(type);
                            Console.WriteLine("🎉 Instance created successfully!");
                            Console.WriteLine("💡 License might be working now!");
                        }
                        catch (System.Runtime.InteropServices.COMException comEx)
                        {
                            if (comEx.HResult == unchecked((int)0x80040112))
                            {
                                Console.WriteLine("❌ Still license error - but CLSID is valid");
                            }
                            else
                            {
                                Console.WriteLine($"❌ Other COM error: 0x{comEx.HResult:X8}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CLSID access failed: {ex.Message}");
            }
        }
    }
}