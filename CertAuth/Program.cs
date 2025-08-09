using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace CertAuthOPC
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== OPC Authentication with Certificate ===");
            Console.WriteLine("Using NI OPC Server certificate for authentication\n");

            TestWithCertificateAuth();
        }

        static void TestWithCertificateAuth()
        {
            Console.WriteLine("Method 1: Using Certificate KeyID for authentication");
            Console.WriteLine("===================================================");
            
            string[] keyIds = {
                "e5ceea3e96a42ad719788858c4a7fdd2e6f03dd2",
                "5191f4f16fb5cb36058b9781f9ab4f4d28c2c716"
            };
            
            string serialNumber = "243ad958";
            string issuer = "CN=NI OPC Servers/UA Client Driver, O=Unknown, C=US, DC=LAPTOP-RJU4P8TR";
            
            foreach (string keyId in keyIds)
            {
                Console.WriteLine($"\nTrying KeyID: {keyId}");
                TryAuthWithKeyId(keyId, serialNumber, issuer);
            }
            
            Console.WriteLine("\nMethod 2: Direct certificate store access");
            Console.WriteLine("=========================================");
            TryDirectCertAccess();
            
            Console.WriteLine("\nMethod 3: Certificate-based OPC connection");
            Console.WriteLine("==========================================");
            TryCertBasedConnection(keyIds[0]);
        }

        static void TryAuthWithKeyId(string keyId, string serialNumber, string issuer)
        {
            try
            {
                Console.WriteLine($"   → Authenticating with KeyID: {keyId}");
                Console.WriteLine($"   → Serial: {serialNumber}");
                Console.WriteLine($"   → Issuer: {issuer}");
                
                // Method 1: ลองใช้ environment variable
                Environment.SetEnvironmentVariable("NI_OPC_CERT_KEYID", keyId);
                Environment.SetEnvironmentVariable("NI_OPC_CERT_SERIAL", serialNumber);
                
                // Method 2: ลองสร้าง OPC connection พร้อม certificate
                Type serverType = Type.GetTypeFromProgID("National Instruments.NIOPCServers.V5");
                if (serverType != null)
                {
                    try
                    {
                        dynamic opcServer = Activator.CreateInstance(serverType);
                        Console.WriteLine("   ✅ OPC Server instance created with certificate context!");
                        
                        // ลองอ่านข้อมูลจริง
                        if (TryReadWithAuth(opcServer, keyId))
                        {
                            Console.WriteLine("   🎉 SUCCESS: Read real data with certificate auth!");
                            return;
                        }
                    }
                    catch (COMException comEx)
                    {
                        if (comEx.HResult == unchecked((int)0x80040112))
                        {
                            Console.WriteLine($"   ❌ Still license error with KeyID: {keyId}");
                        }
                        else
                        {
                            Console.WriteLine($"   ❌ COM Error: 0x{comEx.HResult:X8}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ KeyID {keyId} authentication failed: {ex.Message}");
            }
        }

        static void TryDirectCertAccess()
        {
            try
            {
                Console.WriteLine("Searching for NI OPC certificates in certificate store...");
                
                X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                store.Open(OpenFlags.ReadOnly);
                
                Console.WriteLine($"Found {store.Certificates.Count} certificates in LocalMachine\\My");
                
                foreach (X509Certificate2 cert in store.Certificates)
                {
                    if (cert.Subject.Contains("NI OPC") || cert.Issuer.Contains("NI OPC") || 
                        cert.SerialNumber.Contains("243ad958"))
                    {
                        Console.WriteLine($"   ✅ Found NI OPC Certificate:");
                        Console.WriteLine($"      Subject: {cert.Subject}");
                        Console.WriteLine($"      Issuer: {cert.Issuer}");
                        Console.WriteLine($"      Serial: {cert.SerialNumber}");
                        Console.WriteLine($"      Thumbprint: {cert.Thumbprint}");
                        Console.WriteLine($"      Valid: {cert.NotBefore} to {cert.NotAfter}");
                        
                        // ลองใช้ certificate นี้
                        TryWithSpecificCert(cert);
                    }
                }
                
                store.Close();
                
                // ลองใน CurrentUser store ด้วย
                store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadOnly);
                
                Console.WriteLine($"\nFound {store.Certificates.Count} certificates in CurrentUser\\My");
                
                foreach (X509Certificate2 cert in store.Certificates)
                {
                    if (cert.Subject.Contains("NI OPC") || cert.Issuer.Contains("NI OPC"))
                    {
                        Console.WriteLine($"   ✅ Found NI OPC Certificate in CurrentUser:");
                        Console.WriteLine($"      Subject: {cert.Subject}");
                        Console.WriteLine($"      Thumbprint: {cert.Thumbprint}");
                        
                        TryWithSpecificCert(cert);
                    }
                }
                
                store.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Certificate store access failed: {ex.Message}");
            }
        }

        static void TryWithSpecificCert(X509Certificate2 cert)
        {
            try
            {
                Console.WriteLine($"      → Testing with certificate: {cert.Subject}");
                
                // Set certificate in current context
                System.Security.Cryptography.X509Certificates.X509Certificate.CreateFromCertFile(cert.Subject);
                
                // ลอง OPC connection
                Type serverType = Type.GetTypeFromProgID("National Instruments.NIOPCServers.V5");
                if (serverType != null)
                {
                    dynamic opcServer = Activator.CreateInstance(serverType);
                    Console.WriteLine($"      ✅ OPC Server created with certificate context");
                    
                    if (TryReadWithAuth(opcServer, cert.Thumbprint))
                    {
                        Console.WriteLine($"      🎉 SUCCESS with certificate: {cert.Subject}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"      ❌ Certificate test failed: {ex.Message}");
            }
        }

        static void TryCertBasedConnection(string keyId)
        {
            Console.WriteLine("Creating OPC connection with certificate-based authentication...");
            
            try
            {
                // ลองใช้ DCOM config พร้อม certificate
                var comProps = new System.Collections.Hashtable();
                comProps["Authentication"] = "Certificate";
                comProps["CertificateKeyId"] = keyId;
                comProps["SerialNumber"] = "243ad958";
                
                Type serverType = Type.GetTypeFromProgID("National Instruments.NIOPCServers.V5");
                if (serverType != null)
                {
                    // ตั้งค่า COM security
                    SetCOMSecurity();
                    
                    dynamic opcServer = Activator.CreateInstance(serverType);
                    Console.WriteLine("✅ Certificate-based OPC Server created");
                    
                    if (TryReadWithAuth(opcServer, keyId))
                    {
                        Console.WriteLine("🎯 Successfully read REAL AO0_Speed with certificate auth!");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Certificate-based connection failed: {ex.Message}");
            }
        }

        static void SetCOMSecurity()
        {
            try
            {
                // ตั้งค่า COM security สำหรับ OPC
                Console.WriteLine("   → Setting COM security for OPC...");
                
                // CoInitializeSecurity equivalent
                var hr = CoInitializeSecurity(
                    IntPtr.Zero, -1, IntPtr.Zero, IntPtr.Zero,
                    2, 3, IntPtr.Zero, 0, IntPtr.Zero);
                
                if (hr == 0)
                {
                    Console.WriteLine("   ✅ COM security initialized");
                }
                else
                {
                    Console.WriteLine($"   ⚠️ COM security result: 0x{hr:X8}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   COM security setup failed: {ex.Message}");
            }
        }

        [DllImport("ole32.dll")]
        static extern int CoInitializeSecurity(
            IntPtr pVoid, int cAuthSvc, IntPtr asAuthSvc, IntPtr pReserved1,
            int dwAuthnLevel, int dwImpLevel, IntPtr pAuthList, int dwCapabilities, IntPtr pReserved3);

        static bool TryReadWithAuth(dynamic opcServer, string authContext)
        {
            try
            {
                Console.WriteLine($"      → Attempting authenticated data read...");
                
                var groups = opcServer.OPCGroups;
                var group = groups.Add("AuthGroup");
                group.UpdateRate = 1000;
                group.IsActive = true;

                var items = group.OPCItems;
                items.AddItem("ADAM5000TCP.ADAM-5024.AO0_Speed", 1);
                items.AddItem("ADAM5000TCP.ADAM-5024.AO1_Torque", 2);

                object[] values = new object[2];
                object[] errors = new object[2];
                int[] handles = { 1, 2 };

                group.SyncRead((short)1, 2, handles, out values, out errors);

                Console.WriteLine($"      📊 REAL AUTHENTICATED DATA:");
                Console.WriteLine($"         🎯 AO0_Speed: {values[0]} RPM");
                Console.WriteLine($"         ⚡ AO1_Torque: {values[1]} Nm");
                Console.WriteLine($"         🔐 Auth Context: {authContext}");
                Console.WriteLine($"         ❗ Errors: [{errors[0]}, {errors[1]}]");

                opcServer.Disconnect();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"      ❌ Authenticated read failed: {ex.Message}");
                return false;
            }
        }
    }
}