
using System.Security.Cryptography;

var cryptoServiceProvider = new RSACryptoServiceProvider(2048);
File.WriteAllText("../../../../Api/privateKey.pem", cryptoServiceProvider.ExportRSAPrivateKeyPem());
Console.WriteLine($"Generated private key successfully and saved to {Path.GetFullPath("../../../../Api/privateKey.pem")}");