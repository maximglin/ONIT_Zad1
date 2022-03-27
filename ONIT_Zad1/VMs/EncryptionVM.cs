using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ONIT_Zad1
{
    internal class EncryptionVM
    {
        public IEnumerable<string> Algorithms { get; } = new List<string>() { "AES", "DES" };
        public string SelectedAlgorythm { get; set; } 

        public string PasswordPhrase { private get; set; }

        public string FilePath { get; set; }


        public ICommand EncryptCommand { get; }
        public ICommand CancelCommand { get; }

        private CancellationTokenSource cts = null;
        public EncryptionVM()
        {
            SelectedAlgorythm = Algorithms.First();
            EncryptCommand = new AsyncRelayCommand(async () =>
            {
                using (cts = new CancellationTokenSource())
                {
                    var data = await Task.Run(() => File.ReadAllBytes(FilePath), cts.Token).ConfigureAwait(false);
                    var enc_data = await Encrypt(data, PasswordPhrase, SelectedAlgorythm, cts.Token);

                    var dialog = new SaveFileDialog();
                    dialog.FileName = FilePath + ".enc";
                    if (dialog.ShowDialog() == true)
                    {
                        await File.WriteAllBytesAsync(dialog.FileName, enc_data, cts.Token);
                        System.Windows.MessageBox.Show("Зашифровано и сохранено успешно!");
                    }
                }
                cts = null;
            },
            ex => System.Windows.MessageBox.Show(ex.Message, "Ошибка"));
            CancelCommand = new CancellationCommand(() => cts?.Cancel(), EncryptCommand);
        }



        static async Task<byte[]> Encrypt(byte[] data, string password, string alg, CancellationToken cancellationToken = default)
        {
            byte[] encrypted;


            SymmetricAlgorithm enc_alg;
            if (alg == "AES")
                enc_alg = Aes.Create();
            else
                enc_alg = DES.Create();

            using (enc_alg)
            {
                SHA256 sha = SHA256.Create();
                enc_alg.Padding = PaddingMode.PKCS7;
                byte[] key;

                enc_alg.GenerateIV();
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(password)))
                    key = await sha.ComputeHashAsync(stream, cancellationToken);

                try { 
                    enc_alg.KeySize = sha.HashSize;
                }
                catch (CryptographicException) {
                    while (key.Length*8 < enc_alg.KeySize)
                        key = key.Concat(key).ToArray();
                    key = key.Where((b, i) => i < enc_alg.KeySize/8).ToArray();
                }
                enc_alg.Key = key;


                ICryptoTransform encryptor = enc_alg.CreateEncryptor(enc_alg.Key, enc_alg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    var IVlength = BitConverter.GetBytes(enc_alg.IV.Length);
                    await msEncrypt.WriteAsync(new byte[] { (alg == "AES") ? (byte)0 : (byte)1 }, 0, 1, cancellationToken);
                    await msEncrypt.WriteAsync(IVlength, 0, IVlength.Length, cancellationToken);
                    await msEncrypt.WriteAsync(enc_alg.IV, 0, enc_alg.IV.Length, cancellationToken);
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        await csEncrypt.WriteAsync(data, 0, data.Length, cancellationToken);
                        await csEncrypt.FlushFinalBlockAsync(cancellationToken);

                        encrypted = msEncrypt.ToArray();
                    }
                }
            }

            return encrypted;
        }
    }
}
