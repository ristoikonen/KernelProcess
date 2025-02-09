using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using static FxConsole.FxConsole;


namespace SKProcess
{
    public static class Utilities
    {
        const string jsonDirectory = @"C:\tmp\";
        public static List<string> Hashses { get; set; }

        //TODO: finish this
        public static async Task<bool> ExsistAsync(string fileNameB4hashing, string jsonDirectory)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                string hash = GetHash(sha256Hash, fileNameB4hashing);
                Console.WriteLine($"The SHA256 hash of {fileNameB4hashing} is: {hash}.");

                if (new DirectoryInfo(jsonDirectory).GetFiles().Select(f => f.Name).ToArray().Contains(hash))
                {
                    return true;
                }
            }
            return false;
        }

        //TODO: finish this
        public static async Task StoreJSONAsync(string jsonString, string emailAddress)
        {
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
            Hashses.Add(addMe);
        }


        public static bool Matches(string hashMadeOf2parts, string part1, string part2)
        {
            string source = part1 + part2;
            using (SHA256 sha256Hash = SHA256.Create())
            {
                string hash = GetHash(sha256Hash, source);
                Console.WriteLine($"The SHA256 hash of {source} is: {hash}.");
                Console.WriteLine("Verifying the hash...");
                if (VerifyHash(sha256Hash, source , hashMadeOf2parts))
                {
                    Console.WriteLine("The hashes are the same.");
                }
                else
                {
                    Console.WriteLine("The hashes are not same.");
                }
                return hashMadeOf2parts == hash;
            }
        }


        public static string GenerateHash(string part1, string part2)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                string hash = GetHash(sha256Hash, part1 + part2);
                Console.WriteLine($"The SHA256 hash of {part1 + part2} is: {hash}.");
                Console.WriteLine("Verifying the hash...");
                if (VerifyHash(sha256Hash, part1 + part2, hash))
                {
                    Console.WriteLine("The hashes are the same.");
                }
                else
                {
                    Console.WriteLine("The hashes are not same.");
                }
                return hash;
            }
        }

        // hash returned in hex format 
        private static string GetHash(HashAlgorithm hashAlgorithm, string input)
        {
            byte[] data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }
            return sBuilder.ToString();
        }

        // does input's hash match hash?
        private static bool VerifyHash(HashAlgorithm hashAlgorithm, string input, string hash)
        {
            var hashOfInput = GetHash(hashAlgorithm, input);
            StringComparer comparer = StringComparer.OrdinalIgnoreCase;
            return comparer.Compare(hashOfInput, hash) == 0;
        }



        // to exec batch files, remove this from Release -version!
        public static bool RunBat(string workingDirectory, string fileName)
        {
            using System.Diagnostics.Process p = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    WorkingDirectory = workingDirectory,
                    FileName = fileName,
                    UseShellExecute = true
                }
            };
            // true if Process starts
            return p.Start();
        }
    }
}
