using System;
using System.Text;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.IO;

namespace Go
{
#if CHECK_LICENSE
    abstract class license
    {
        static readonly object _lockCheck = new object();

        static public bool check()
        {
            lock (_lockCheck)
            {
                try
                {
                    string rsaPublicKey = @"<RSAKeyValue><Modulus>ljGyVPIqiyiwZj8U4CiySD6u85dauJSQ++u7GnEEM/WiS0j/Ww71Q46YCBst0dnYUF/Y3GEBnIwhODdhJcADe9WIIZNy+MvHLxXqQFOTBzqO+UcCKCVWZZhkku7wVdN9cHgLdrt38Rl6jfFl9j27SHI18IFNxByQbb+vBU1sStM=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";
                    MD5 md5Calc = new MD5CryptoServiceProvider();
                    RSACryptoServiceProvider rsaCheck = new RSACryptoServiceProvider();
                    string[] limitPerfor = File.ReadAllLines("csgo_license");
                    byte[] smartEnc = Convert.FromBase64String(limitPerfor[1]);
                    md5Calc.TransformBlock(smartEnc, 0, smartEnc.Length, null, 0);
                    byte[] devEnc = Convert.FromBase64String(limitPerfor[2]);
                    md5Calc.TransformBlock(devEnc, 0, devEnc.Length, null, 0);
                    byte[] serialEnc = Convert.FromBase64String(limitPerfor[3]);
                    md5Calc.TransformBlock(serialEnc, 0, serialEnc.Length, null, 0);
                    byte[] authHourEnc = Convert.FromBase64String(limitPerfor[4]);
                    md5Calc.TransformBlock(authHourEnc, 0, authHourEnc.Length, null, 0);
                    md5Calc.TransformFinalBlock(new byte[0], 0, 0);
                    rsaCheck.FromXmlString(rsaPublicKey);
                    if (!rsaCheck.VerifyData(md5Calc.Hash, SHA1.Create(), Convert.FromBase64String(limitPerfor[0])))
                    {
                        return false;
                    }
                    md5Calc = new MD5CryptoServiceProvider();
                    RijndaelManaged aes = new RijndaelManaged();
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    aes.IV = new byte[16];
                    aes.Key = Encoding.Default.GetBytes("Hello CsGo Hello CsGo Hello CsGo");
                    ICryptoTransform decryptor = aes.CreateDecryptor();
                    int authHour = int.Parse(Encoding.Default.GetString(decryptor.TransformFinalBlock(authHourEnc, 0, authHourEnc.Length)).Substring(8));
                    string checkSerialNumber = Encoding.Default.GetString(decryptor.TransformFinalBlock(serialEnc, 0, serialEnc.Length)).Substring(8);
                    string smartctl = "smartctl.exe";
                    using (FileStream smartStream = new FileStream(smartctl, FileMode.Open, FileAccess.Read))
                    {
                        byte[] md5 = md5Calc.ComputeHash(smartStream);
                        string md5Str = string.Format("{0:X2}{1:X2}{2:X2}{3:X2}{4:X2}{5:X2}{6:X2}{7:X2}{8:X2}{9:X2}{10:X2}{11:X2}{12:X2}{13:X2}{14:X2}{15:X2}",
                            md5[0], md5[1], md5[2], md5[3], md5[4], md5[5], md5[6], md5[7], md5[8], md5[9], md5[10], md5[11], md5[12], md5[13], md5[14], md5[15]);
                        if (Encoding.Default.GetString(decryptor.TransformFinalBlock(smartEnc, 0, smartEnc.Length)).Substring(8) == md5Str)
                        {
                            Process smartctlProcess = new Process();
                            smartctlProcess.StartInfo.FileName = smartctl;
                            smartctlProcess.StartInfo.Arguments = string.Format("-a /dev/{0}", Encoding.Default.GetString(decryptor.TransformFinalBlock(devEnc, 0, devEnc.Length)).Substring(8));
                            smartctlProcess.StartInfo.UseShellExecute = false;
                            smartctlProcess.StartInfo.RedirectStandardOutput = true;
                            smartctlProcess.StartInfo.CreateNoWindow = true;
                            smartctlProcess.Start();
                            string smartInfo = smartctlProcess.StandardOutput.ReadToEnd();
                            smartctlProcess.Close();
                            GroupCollection serialMat = Regex.Match(smartInfo, @"Serial Number: +(.+)\r").Groups;
                            GroupCollection hourMat = Regex.Match(smartInfo, @"Power_On_Hours.+?(\d+)\r").Groups;
                            if (serialMat[1].Value == checkSerialNumber && int.Parse(hourMat[1].Value) < authHour)
                            {
                                return true;
                            }
                        }
                    }
                }
                catch (System.Exception) { }
            }
            return false;
        }
    }
#endif
}
