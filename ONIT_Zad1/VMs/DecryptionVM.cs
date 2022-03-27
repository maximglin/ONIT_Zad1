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
    internal class DecryptionVM
    {
        public string PasswordPhrase { private get; set; }

        public string FilePath { get; set; }


        public ICommand DecryptCommand { get; }
        public ICommand CancelCommand { get; }

        private CancellationTokenSource cts = null;


        public DecryptionVM()
        {
            DecryptCommand = new AsyncRelayCommand(async () =>
            {
                using (cts = new CancellationTokenSource())
                {
                    var data = await Task.Run(() => File.ReadAllBytes(FilePath), cts.Token).ConfigureAwait(false);
                    var dec_data = await Decrypt(data, PasswordPhrase, cts.Token);

                    if(dec_data == null)
                        throw new Exception("Неверная парольная фраза для расшифровки!");

                    var dialog = new SaveFileDialog();
                    dialog.FileName = FilePath.Replace(".enc", "");
                    if (dialog.ShowDialog() == true)
                    {
                        await File.WriteAllBytesAsync(dialog.FileName, dec_data, cts.Token);
                        System.Windows.MessageBox.Show("Расшифровано и сохранено успешно!");
                    }
                }
                cts = null;
            },
            ex => System.Windows.MessageBox.Show(ex.Message, "Ошибка"));
            CancelCommand = new CancellationCommand(() => cts?.Cancel(), DecryptCommand);
        }




        private static async Task<byte[]> Decrypt(byte[] data, string password, CancellationToken cancellationToken = default)
        {
            byte[] decrypted;
            try
            {
                SymmetricAlgorithm enc_alg;
                if (data[0] == (byte)0)
                    enc_alg = Aes.Create();
                else
                    enc_alg = DES.Create();

                data = data.Where((b, i) => i > 0).ToArray();

                byte[] key;
                using (enc_alg)
                {
                    SHA256 sha = SHA256.Create();
                    enc_alg.Padding = PaddingMode.PKCS7;


                    using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(password)))
                        key = await sha.ComputeHashAsync(stream, cancellationToken);
                    try
                    {
                        enc_alg.KeySize = sha.HashSize;
                    }
                    catch (CryptographicException)
                    {
                        while (key.Length * 8 < enc_alg.KeySize)
                            key = key.Concat(key).ToArray();
                        key = key.Where((b, i) => i < enc_alg.KeySize / 8).ToArray();
                    }
                    enc_alg.Key = key;



                    using (MemoryStream msEncrypt = new MemoryStream())
                    {
                        var IVlength = BitConverter.ToInt32(data, 0);
                        var IV = data.Where((b, i) => i >= 4 && i < (4 + IVlength)).ToArray();

                        enc_alg.IV = IV;
                        ICryptoTransform encryptor = enc_alg.CreateDecryptor(enc_alg.Key, enc_alg.IV);


                        data = data.Where((b, i) => i >= (4 + IVlength)).ToArray();
                        using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        {
                            await csEncrypt.WriteAsync(data, 0, data.Length, cancellationToken);
                            await csEncrypt.FlushFinalBlockAsync(cancellationToken);

                            decrypted = msEncrypt.ToArray();
                        }
                    }
                }

                return decrypted;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
