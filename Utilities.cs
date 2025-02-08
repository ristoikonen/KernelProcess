using System.Security.Cryptography;
using System.Text;
using System.Text.Json;


namespace SKProcess
{
    public static class Utilities
    {
        const string jsonDirectory = @"C:\tmp\";
        public static List<string> Hashses { get; set; }

        //TODO: finish this
        public static async Task<bool> ExsistAsync( string emailAddress)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                string hash = GetHash(sha256Hash, emailAddress);
                Console.WriteLine($"The SHA256 hash of {emailAddress} is: {hash}.");

                if (new DirectoryInfo(jsonDirectory).GetFiles().Select(f => f.Name).ToArray().Contains(hash))
                {
                    return true;
                }
                //Directory.EnumerateFiles("*.json").First();
            }

            //string fileName = "Account.json";
            //await using FileStream createStream = File.Create(fileName);
            //await JsonSerializer.SerializeAsync(createStream, jsonString);

            //Console.WriteLine(File.ReadAllText(fileName));
            
            return false;
        }

        //TODO: finish this
        public static async Task StoreJSONAsync(string jsonString, string emailAddress)
        {
            //string jsonString = JsonSerializer.Serialize(storeMe);
            using (SHA256 sha256Hash = SHA256.Create())
            {
                string hash = GetHash(sha256Hash, emailAddress);
                Console.WriteLine($"The SHA256 hash of {emailAddress} is: {hash}.");

                string fileName = hash ?? "Account.json";
                await using FileStream createStream = File.Create(fileName);
                await JsonSerializer.SerializeAsync(createStream, jsonString);

                Console.WriteLine(File.ReadAllText(fileName));
            }
        }

        public static void AddTohashCollection(string addMe)
        {
            // check if exsuist?
            Hashses.Add(addMe);
        }

        //TODO to bool
        public static void Matches(string comapereMe)
        {
            string source = "Hello World!";
            using (SHA256 sha256Hash = SHA256.Create())
            {
                string hash = GetHash(sha256Hash, source);
                Console.WriteLine($"The SHA256 hash of {source} is: {hash}.");
                Console.WriteLine("Verifying the hash...");
                if (VerifyHash(sha256Hash, source, hash))
                {
                    Console.WriteLine("The hashes are the same.");
                }
                else
                {
                    Console.WriteLine("The hashes are not same.");
                }
            }
        }

        private static string GetHash(HashAlgorithm hashAlgorithm, string input)
        {
            // Convert the input string to a byte array and compute the hash.
            byte[] data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));
            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            var sBuilder = new StringBuilder();
            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }
            // Return the hexadecimal string.
            return sBuilder.ToString();
        }

        // Verify a hash against a string.
        private static bool VerifyHash(HashAlgorithm hashAlgorithm, string input, string hash)
        {
            // Hash the input.
            var hashOfInput = GetHash(hashAlgorithm, input);
            // Create a StringComparer an compare the hashes.
            StringComparer comparer = StringComparer.OrdinalIgnoreCase;
            return comparer.Compare(hashOfInput, hash) == 0;
        }
    }
}
