// See https://aka.ms/new-console-template for more information

using System.Security.Cryptography;
using System.Text;

Console.WriteLine("Welcome to the generator CLI for non expiring tokens for YetAnotherCentralizedChat");
Console.WriteLine("Where is your private key .pem file located? (Please enter the path):");
string privateKeyPath = Console.ReadLine()!;
if (privateKeyPath == "") privateKeyPath = "../../../../Api/privateKey.pem";

var cryptoServiceProvider = new RSACryptoServiceProvider(2048);

cryptoServiceProvider.ImportFromPem(File.ReadAllText(privateKeyPath));

Console.WriteLine("Please enter the username of the user you would like a non expiring token for: ");
string username = Console.ReadLine()!;

Console.WriteLine("Please enter the user id of the user you would like a non expiring token for: ");
int id = int.Parse(Console.ReadLine()!);

DateTime expirationDate = DateTime.UnixEpoch;
string unencryptedToken = $"iwouldnottrustmyencryption;{username};{id};{expirationDate.ToBinary()};{Random.Shared.NextSingle()}";
byte[] tokenBytes = cryptoServiceProvider.Encrypt(Encoding.UTF8.GetBytes(unencryptedToken), RSAEncryptionPadding.Pkcs1);
string token = Convert.ToBase64String(tokenBytes);
Console.WriteLine($"Your non expiring token is: {token}");