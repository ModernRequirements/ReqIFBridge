using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using Edev.LM;
using inteGREAT.Web.Common;
using Newtonsoft.Json;
using ReqIFBridge.Utility;
using OperationResult = Edev.LM.OperationResult;
using OperationStatusTypes = Edev.LM.OperationStatusTypes;

namespace ReqIFBridge.Models
{
    public sealed class RedisRegistry:IRegistry
    {
        private const char CHAR_INSERT_ENCDEC = '$';
        private string mProductID = string.Empty;
        private string mProductVersion = string.Empty;
        private const string mRedisKeyPrefix = "lic-";
        private string mBackupFilePath = string.Empty;

        private const string CONST_OFFLINE_COMPUTERID = "||OFFLINE";
        private const string CONST_FLOATING_COMPUTERID = "||FLOATING";
        private const string CONST_FLOATING_FEATURESCOUNT = "||FEATURESCOUNT";

        /// <summary>
        /// Initializes the <see cref="RedisHelper"/> class.
        /// </summary>
        public RedisRegistry(int productID, int majorVersion, int minorVersion, int buildVersion)
        {
            DebugLogger.LogStart("RedisRegistry", "RedisRegistry()");


            mProductID = productID.ToString();
            mProductVersion = string.Format(@"{0}{1}{2}",
                new object[]
                {
                    majorVersion.ToString(),
                    minorVersion.ToString(),
                    buildVersion.ToString()
                });

            DebugLogger.LogEnd("RedisRegistry", "RedisRegistry()");
        }

        #region IRegistry
        public OperationResult Write(string key, string value)
        {
            DebugLogger.LogStart("RedisRegistry", "Write");

            OperationResult result = null;
            string keyWithPrefix = AddKeyPrefix(key);

            try
            {
                string encData = this.Encrypt(value);

                bool isWriteOnRedis = RedisHelper.SaveInCache(keyWithPrefix, JsonConvert.SerializeObject(encData), null);
                if (isWriteOnRedis)
                {
                    var msg  =
                    string.Format(
                        "key Value: {0} ; key Data: {1} is saved in key in aganist to actual key Data:", new object[] { keyWithPrefix, encData });
                    result = new OperationResult(OperationStatusTypes.Success, msg);
                    DebugLogger.LogInfo(result.Message);
                    
                }
                UpdateRegistryBackupFile(keyWithPrefix, encData);
            }
            catch (Exception ex)
            {
                string err = string.Format("Unable to write Key for User: {0}", key);
                result = new OperationResult(OperationStatusTypes.Failed, err, ex);
                DebugLogger.LogError(ex);
            }

            DebugLogger.LogEnd("RedisRegistry", "Write");
            return result;
        }
        
        public OperationResult Read(string key, out string value)
        {
         
            DebugLogger.LogStart("RedisRegistry", "Read");
            OperationResult result = null;
            value = string.Empty;
            
            string keyWithPrefix = AddKeyPrefix(key);

            if (RedisHelper.KeyExist(keyWithPrefix))
            {
                try
                {
                    DebugLogger.LogInfo("RedisRegistry.Read:Key found in redis");
                    string keyValue = JsonConvert.DeserializeObject<string>(RedisHelper.LoadFromCache(keyWithPrefix));
                    value = this.Decrypt(keyValue);
                    if (string.IsNullOrEmpty(value))
                    {
                        result = new OperationResult(OperationStatusTypes.Failed);
                        result.Message = "Unable to decrypt Key's data.";
                    }
                    else
                    {
                        result = new OperationResult(OperationStatusTypes.Success);
                        result.Message = "Key's data is readed successfully from redis.";
                    }
                    
                    DebugLogger.LogInfo(result.Message);
                }
                catch (Exception ex)
                {
                    result = new OperationResult(OperationStatusTypes.Failed, ex.Message);
                    DebugLogger.LogError(ex);
                }
            }
            else
            {
                DebugLogger.LogInfo("RedisRegistry.Reading from Registry Backup File: Start");
                if (File.Exists(this.BackupFilePath))
                {
                    try
                    {
                    DebugLogger.LogInfo("Registry file has been found.");
                    string fileValue = string.Empty;
                    var lines = File.ReadAllLines(this.BackupFilePath);
                    foreach (var singleLine in lines)
                    {
                                if (string.IsNullOrEmpty(singleLine)) continue;
                        
                                string[] fileLineArray = singleLine.Split(';');
                                if (fileLineArray.Length == 2)
                                {
                                    var keyName = fileLineArray[0];
                                    var keyvalue = fileLineArray[1];
                                    
                                    string keyNameWithProductId = keyWithPrefix + this.ProductId.ToString();
                                    if (keyName == keyNameWithProductId)
                                    {
                                        fileValue = keyvalue;
                                    }
                                }
                                if (!string.IsNullOrEmpty(fileValue)) // in case key exist but no value
                                {
                                  
                                    value = this.Decrypt(fileValue);
                                  
                                    if (string.IsNullOrEmpty(value))
                                    {
                                        result = new OperationResult(OperationStatusTypes.Failed);
                                        result.Message = "Unable to decrypt Key's data.";
                                    }
                                    else
                                    {
                                        result = new OperationResult(OperationStatusTypes.Success);
                                        result.Message = "Key's data is readed successfully from redis.";
                                    }
                                    break;
                                }
                            }

                    if (string.IsNullOrEmpty(fileValue))
                    {
                        result = new OperationResult(OperationStatusTypes.Success);
                        result.Message = "Key's data is readed successfully. No Key found in Registry Backup File.";
                    }
                    }
                    catch (Exception ex)
                    {
                        result = new OperationResult(OperationStatusTypes.Failed, ex.Message);
                        DebugLogger.LogError(ex);

                    }
                }
                else
                {
                    string errmsg = string.Format("No Registry file has been found.");
                    result = new OperationResult(OperationStatusTypes.Failed, errmsg);
                    DebugLogger.LogWarn(errmsg);
                }
                DebugLogger.LogInfo("RedisRegistry.Reading from Registry Backup File: End");
             }

            DebugLogger.LogEnd("RedisRegistry", "Read");
            return result;
        }

        public OperationResult CreateKeyIfNotExists()
        {
            DebugLogger.LogStart("RedisRegistry", "CreateKeyIfNotExists");
            OperationResult result = null;
            if (this.isKeyExist)
            {
                result = new OperationResult(OperationStatusTypes.Success, "Registry Key already Exists.");
                DebugLogger.LogEnd("RedisRegistry", "CreateKeyIfNotExists");
                return result;
            }
            DebugLogger.LogEnd("RedisRegistry", "CreateKeyIfNotExists");
            return result;
        }

        public OperationResult Remove(string key)
        {
            DebugLogger.LogStart("RedisRegistry", "Remove");

            OperationResult result = null;
            List<string> mKeysToDelForRedis = new List<string>();
            List<string> mKeysToDelForBackup = new List<string>();

            bool deleteSucessRedis = false;
            bool deleteSucessBackFile = false;
            string keyWithPrefix = AddKeyPrefix(key);
            mKeysToDelForRedis = CheckUsersInRedis(keyWithPrefix);

            try
            {
                if (mKeysToDelForRedis.Count > 0)
                {
                    foreach (var keyName in mKeysToDelForRedis)
                    {
                        try
                        {
                            deleteSucessRedis = RedisHelper.RemoveFromCache(keyName);
                        }
                        catch (Exception ex)
                        {
                            DebugLogger.LogError(ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string err = string.Format("Unable to remove key for user: {0}", key);
                result = new OperationResult(OperationStatusTypes.Failed, err, ex);
                DebugLogger.LogError(ex);
            }

            mKeysToDelForBackup = CheckUsersInBackupFile(keyWithPrefix);

            try
            {
                if (mKeysToDelForBackup.Count > 0)
                {
                    deleteSucessBackFile = RemoveUserFromBackupFile(mKeysToDelForBackup);
                }

                if (deleteSucessBackFile || deleteSucessRedis)
                {
                    var msg = string.Format("Unable to remove key for user: {0}", key);
                    result = new OperationResult(OperationStatusTypes.Success, msg);
                    DebugLogger.LogInfo(result.Message);

                }
                else
                {
                    var msg = string.Format("Unable to remove key for user: {0}", key);
                    result = new OperationResult(OperationStatusTypes.Failed, msg);
                    DebugLogger.LogInfo(result.Message);
                }
               
            }
            catch (Exception ex)
            {
                string err = string.Format("Unable to remove key for user: {0}", key);
                result = new OperationResult(OperationStatusTypes.Failed, err, ex);
                DebugLogger.LogError(ex);
            }

            DebugLogger.LogEnd("RedisRegistry", "Remove");
            return result;
        }



        #endregion

        #region Public Property

        public bool isKeyExist
        {
            get
            {
                bool isExist = true;
                return isExist;
            }
        }

        public int ProductId
        {
            get
            {
                int id = -1;
                int.TryParse(mProductID, out id);
                return id;
            }
            //set { mProductID = value; }
        }

        public int ProductVersion
        {
            get
            {
                int ver = -1;
                if (!string.IsNullOrEmpty(mProductVersion) && int.TryParse(mProductVersion, out ver))
                {
                }
                return ver;
            }
        }

        public string BackupFilePath
        {
            get
            {
               return this.mBackupFilePath;
            }
            set
            {
               this.mBackupFilePath = value;
            }
        }

        #endregion



        #region Private Methods

        private string AddKeyPrefix(string key)
        {
            DebugLogger.LogStart("RedisRegistry","AddKeyPrefix");
            string mkey = mRedisKeyPrefix + key;
            DebugLogger.LogEnd("RedisRegistry", "AddKeyPrefix");
            return mkey;
        }

        private void UpdateRegistryBackupFile(string regKeyName, string encData)
        {
            DebugLogger.LogStart("RedisRegistry", "UpdateRegistryBackupFile");
            string keyNameWithProductId = regKeyName + this.ProductId.ToString();
            Dictionary<string, string> registryBackFileDictionary = new Dictionary<string, string>();

            try
            {
                if (File.Exists(this.BackupFilePath))
                {
                    var lines = File.ReadAllLines(this.BackupFilePath);
                    foreach (var singleLine in lines)
                    {
                        string[] fileLineArray = singleLine.Split(';');
                        if (!string.IsNullOrEmpty(singleLine) && fileLineArray.Length == 2)
                        {
                            var keyName = fileLineArray[0];
                            var keyvalue = fileLineArray[1];
                            registryBackFileDictionary.Add(keyName, keyvalue);
                        }
                    }
                   
                }
                if (registryBackFileDictionary.Count > 0)
                {
                    if (registryBackFileDictionary.ContainsKey(keyNameWithProductId))
                    {
                        registryBackFileDictionary[keyNameWithProductId] = encData;
                        
                    }
                    else
                    {
                        registryBackFileDictionary.Add(keyNameWithProductId, encData);
                       
                    }
                }
                else
                {
                    registryBackFileDictionary.Add(keyNameWithProductId, encData);
                    
                }

                using (StreamWriter file = new StreamWriter(this.BackupFilePath))
                    foreach (var entry in registryBackFileDictionary)
                    {
                        file.WriteLine("{0}{1}{2}", entry.Key, ";", entry.Value);
                    }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
                //DebugLogger.LogError(string.Format("Error Details: {0} Stack: {1}", ex.Message, ex.StackTrace));
            }
            
            DebugLogger.LogEnd("RedisRegistry", "UpdateRegistryBackupFile");
         }


        private List<string> CheckUsersInRedis(string keyName)
        {
            //DebugLogger.LogDebug("RedisRegistry.CheckUsersInRedis: Start");
            DebugLogger.LogStart("RedisRegistry", "CheckUsersInRedis");
            List<string> mKeysToDelForRedis = new List<string>();
            try
            {
            
                if (RedisHelper.IsRedisRunning)
                {
                    if (RedisHelper.KeyExist(keyName))
                    {
                        mKeysToDelForRedis.Add(keyName);
                    }
                    else if (RedisHelper.KeyExist(keyName + CONST_OFFLINE_COMPUTERID))
                    {
                        mKeysToDelForRedis.Add(keyName + CONST_OFFLINE_COMPUTERID);
                    }
                    else if(RedisHelper.KeyExist(keyName + CONST_FLOATING_COMPUTERID))
                    {
                        mKeysToDelForRedis.Add(keyName + CONST_FLOATING_COMPUTERID);
                        mKeysToDelForRedis.Add(keyName + CONST_FLOATING_FEATURESCOUNT);
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
            }
            
            DebugLogger.LogEnd("RedisRegistry", "CheckUsersInRedis");
            return mKeysToDelForRedis;
        }



        private List<string> CheckUsersInBackupFile(string keyName)
        {
            DebugLogger.LogStart("RedisRegistry","CheckUsersInBackupFile");
            List<string> mKeysToDelForBackup = new List<string>();
            Dictionary<string, string> registryBackFileDictionary = new Dictionary<string, string>();
            try
            {
               
                if (File.Exists(this.BackupFilePath))

                {
                    var lines = File.ReadAllLines(this.BackupFilePath);
                    foreach (var singleLine in lines)
                    {
                        string[] fileLineArray = singleLine.Split(';');
                        if (!string.IsNullOrEmpty(singleLine) && fileLineArray.Length == 2)
                        {
                            var key = fileLineArray[0];
                            var keyvalue = fileLineArray[1];
                            registryBackFileDictionary.Add(key, keyvalue);
                        }
                    }
                  
                }
                if (registryBackFileDictionary.Count > 0)
                {

                    if (registryBackFileDictionary.ContainsKey(keyName + ProductId))
                    {
                        mKeysToDelForBackup.Add(keyName + ProductId);
                    }

                   else if (registryBackFileDictionary.ContainsKey(keyName + CONST_OFFLINE_COMPUTERID + ProductId))
                    {
                        mKeysToDelForBackup.Add(keyName + CONST_OFFLINE_COMPUTERID + ProductId);
                    }

                    else if (registryBackFileDictionary.ContainsKey(keyName + CONST_FLOATING_COMPUTERID + ProductId))
                    {
                        mKeysToDelForBackup.Add(keyName + CONST_FLOATING_COMPUTERID + ProductId);
                        mKeysToDelForBackup.Add(keyName + CONST_FLOATING_FEATURESCOUNT + ProductId);
                    }
                }
            }
            catch (Exception ex)
            {
                
                DebugLogger.LogError(ex);
            }

            DebugLogger.LogEnd("RedisRegistry", "CheckUsersInBackupFile");
            return mKeysToDelForBackup;
        }

        private bool RemoveUserFromBackupFile(List<string> lstKeys)
        {
            DebugLogger.LogStart("RedisRegistry","RemoveUserFromBackupFile");
            Dictionary<string, string> licenseBackFileDictionary = new Dictionary<string, string>();
            bool isUserRemove = false;
            try
            {
                if (File.Exists(this.BackupFilePath))
                {
                    var lines = File.ReadAllLines(this.BackupFilePath);
                    foreach (var singleLine in lines)
                    {
                        string[] fileLineArray = singleLine.Split(';');
                        if (!string.IsNullOrEmpty(singleLine) && fileLineArray.Length == 2)
                        {
                            var keyName = fileLineArray[0];
                            var keyvalue = fileLineArray[1];
                            licenseBackFileDictionary.Add(keyName, keyvalue);
                        }
                    }
                   
                }
                if (licenseBackFileDictionary.Count > 0)
                {
                    foreach (var keyName in lstKeys)
                    {
                        if (licenseBackFileDictionary.ContainsKey(keyName))
                        {
                            licenseBackFileDictionary.Remove(keyName);
                            isUserRemove = true;
                        }
                    }
                }

                if (isUserRemove)
                {
                    using (StreamWriter file = new StreamWriter(this.BackupFilePath))
                        foreach (var entry in licenseBackFileDictionary)
                        {
                            file.WriteLine("{0}{1}{2}", entry.Key, ";", entry.Value);
                        }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
            }
            DebugLogger.LogEnd("RedisRegistry", "RemoveUserFromBackupFile");
            return isUserRemove;
        }



        #endregion


        #region Encryption Decryption

        /// <summary>
        /// Encrypts the string value.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private string Encrypt(string value)
        {
            DebugLogger.LogStart("RedisRegistry","Encrypt");
            string strEncrypted = string.Empty;

            value = string.Format("{0}{1}{2}", new object[] { CHAR_INSERT_ENCDEC, value, CHAR_INSERT_ENCDEC });
            try
            {
                strEncrypted = Encryption.EncryptIt(value, ProductId, ProductVersion);
            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
            }

            DebugLogger.LogEnd("RedisRegistry","Encrypt");
            return strEncrypted;
        }

        /// <summary>
        /// Decrypts the string value
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private string Decrypt(string value)
        {
            DebugLogger.LogStart("RedisRegistry","Decrypt");

            //char insertChar = '$';
            string strDecrypted = string.Empty;

            try
            {

                strDecrypted = Encryption.DecryptIt(value, ProductId, ProductVersion);

                if (!string.IsNullOrEmpty(strDecrypted) && strDecrypted.StartsWith(CHAR_INSERT_ENCDEC.ToString()) && strDecrypted.EndsWith(CHAR_INSERT_ENCDEC.ToString()))
                {
                    strDecrypted = strDecrypted.Remove(0, 1);
                    strDecrypted = strDecrypted.Remove(strDecrypted.Length - 1, 1);
                }
                else
                {
                    strDecrypted = string.Empty;
                }

            }
            catch (Exception ex)
            {
                DebugLogger.LogError(ex);
            }

            DebugLogger.LogInfo(string.Format("Decrypted value is: {0} from original {1}", strDecrypted, value));
            DebugLogger.LogEnd("RedisRegistry", "Decrypt");
            return strDecrypted;
        }

        #endregion


    }

    internal sealed class Encryption
    {
        internal static string EncryptIt(string strVal, int productId, int majorVersionNo)
        {
            // Get the last 4 digits of current TimeStamp
            string strTicks = DateTime.Now.Ticks.ToString();

            Random objRandom = new Random(int.Parse(strTicks.Substring(strTicks.Length / 2, 4)));
            string strRandomDigits = objRandom.Next(1000, 9999).ToString();

            /*
             * Note: The 4th position of string in Registry will contain
             *       the 4 Random digits to be concatenated with SaltValue
             * */
            string strEncString = EncryptString(strVal, GetKey(), GetSaltValue(productId, majorVersionNo) + strRandomDigits, "MD5", 3, "@1B2c3D4e5F6g7H8", 256);

            strEncString = strEncString.Insert(3, strRandomDigits);

            return strEncString;
        }
        internal static string DecryptIt(string strVal, int productId, int majorVersionNo)
        {
            string strRandomDigits = string.Empty;
            string strDecryptedString = string.Empty;

            try
            {
                // Seperate the 4 concatenated digits from resgistery value
                for (int i = 0; i < 4; i++)
                {
                    strRandomDigits += strVal[3].ToString();
                    strVal = strVal.Remove(3, 1);
                }

                // Getting Decrypted string
                strDecryptedString = DecryptString(strVal, GetKey(), GetSaltValue(productId, majorVersionNo) + strRandomDigits, "MD5", 3,
                                                   "@1B2c3D4e5F6g7H8", 256);
            }
            catch (Exception err)
            {
                Console.Write(err.Message);
            }

            return strDecryptedString;
        }

        #region Key Generation

        private static string GetSaltValue(int productId, int majorVersionNo)
        {
            return string.Format("{0}_{1}_{2}", GetBoardSerialID(), productId, majorVersionNo);
        }

        private static string GetKey()
        {
            byte[] arrBytes = new byte[36];

            arrBytes[0] = 48;
            arrBytes[1] = 56;
            arrBytes[2] = 65;
            arrBytes[3] = 54;
            arrBytes[4] = 66;
            arrBytes[5] = 67;
            arrBytes[6] = 51;
            arrBytes[7] = 48;
            arrBytes[8] = 45;
            arrBytes[9] = 55;
            arrBytes[10] = 51;
            arrBytes[11] = 55;
            arrBytes[12] = 68;
            arrBytes[13] = 45;
            arrBytes[14] = 52;
            arrBytes[15] = 51;
            arrBytes[16] = 97;
            arrBytes[17] = 102;
            arrBytes[18] = 45;
            arrBytes[19] = 66;
            arrBytes[20] = 52;
            arrBytes[21] = 53;
            arrBytes[22] = 48;
            arrBytes[23] = 45;
            arrBytes[24] = 51;
            arrBytes[25] = 69;
            arrBytes[26] = 54;
            arrBytes[27] = 55;
            arrBytes[28] = 51;
            arrBytes[29] = 69;
            arrBytes[30] = 70;
            arrBytes[31] = 69;
            arrBytes[32] = 67;
            arrBytes[33] = 51;
            arrBytes[34] = 68;
            arrBytes[35] = 54;


            //Use the following call to get the string from byte array
            return new System.Text.ASCIIEncoding().GetString(arrBytes);
            //The output of above call is: 08A6BC30-737D-43af-B450-3E673EFEC3D6
        }

        #endregion

        #region Encription/Decryption Logic

        /// <summary>
        /// Encrypts the string.
        /// </summary>
        /// <param name="plainText">The plain text.</param>
        /// <param name="passPhrase">The pass phrase.</param>
        /// <param name="saltValue">The salt value.</param>
        /// <param name="hashAlgorithm">The hash algorithm.</param>
        /// <param name="passwordIterations">The password iterations.</param>
        /// <param name="initVector">The init vector.</param>
        /// <param name="keySize">Size of the key.</param>
        /// <returns></returns>
        private static string EncryptString(string plainText,
            string passPhrase,
            string saltValue,
            string hashAlgorithm,
            int passwordIterations,
            string initVector,
            int keySize)
        {
            // Convert strings into byte arrays.
            // Let us assume that strings only contain ASCII codes.
            // If strings include Unicode characters, use Unicode, UTF7, or UTF8 
            // encoding.
            byte[] initVectorBytes = Encoding.ASCII.GetBytes(initVector);
            byte[] saltValueBytes = Encoding.ASCII.GetBytes(saltValue);

            // Convert our plaintext into a byte array.
            // Let us assume that plaintext contains UTF8-encoded characters.
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);

            // First, we must create a password, from which the key will be derived.
            // This password will be generated from the specified passphrase and 
            // salt value. The password will be created using the specified hash 
            // algorithm. Password creation can be done in several iterations.
            PasswordDeriveBytes password = new PasswordDeriveBytes(
                passPhrase,
                saltValueBytes,
                hashAlgorithm,
                passwordIterations);

            // Use the password to generate pseudo-random bytes for the encryption
            // key. Specify the size of the key in bytes (instead of bits).
            byte[] keyBytes = password.GetBytes(keySize / 8);

            // Create uninitialized Rijndael encryption object.
            RijndaelManaged symmetricKey = new RijndaelManaged();

            // It is reasonable to set encryption mode to Cipher Block Chaining
            // (CBC). Use default options for other symmetric key parameters.
            symmetricKey.Mode = CipherMode.CBC;

            // Generate encryptor from the existing key bytes and initialization 
            // vector. Key size will be defined based on the number of the key 
            // bytes.
            ICryptoTransform encryptor = symmetricKey.CreateEncryptor(
                keyBytes,
                initVectorBytes);

            // Define memory stream which will be used to hold encrypted data.
            MemoryStream memoryStream = new MemoryStream();
            byte[] cipherTextBytes;

            // Define cryptographic stream (always use Write mode for encryption).
            using (CryptoStream cryptoStream = new CryptoStream(memoryStream,
                      encryptor,
                      CryptoStreamMode.Write))
            {
                // Start encrypting.
                cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);

                // Finish encrypting.
                cryptoStream.FlushFinalBlock();

                // Convert our encrypted data from a memory stream into a byte array.
                cipherTextBytes = memoryStream.ToArray();

                // Close both streams.
                memoryStream.Close();
                cryptoStream.Close();
            }

            // Convert encrypted data into a base64-encoded string.
            string cipherText = Convert.ToBase64String(cipherTextBytes);

            // Return encrypted string.
            return cipherText;
        }


        /// <summary>
        /// Decrypts the string.
        /// </summary>
        /// <param name="cipherText">The cipher text.</param>
        /// <param name="passPhrase">The pass phrase.</param>
        /// <param name="saltValue">The salt value.</param>
        /// <param name="hashAlgorithm">The hash algorithm.</param>
        /// <param name="passwordIterations">The password iterations.</param>
        /// <param name="initVector">The init vector.</param>
        /// <param name="keySize">Size of the key.</param>
        /// <returns></returns>
        private static string DecryptString(string cipherText,
            string passPhrase,
            string saltValue,
            string hashAlgorithm,
            int passwordIterations,
            string initVector,
            int keySize)
        {
            // Convert strings defining encryption key characteristics into byte
            // arrays. Let us assume that strings only contain ASCII codes.
            // If strings include Unicode characters, use Unicode, UTF7, or UTF8
            // encoding.
            byte[] initVectorBytes = Encoding.ASCII.GetBytes(initVector);
            byte[] saltValueBytes = Encoding.ASCII.GetBytes(saltValue);

            // Convert our ciphertext into a byte array.
            byte[] cipherTextBytes = Convert.FromBase64String(cipherText);

            // First, we must create a password, from which the key will be 
            // derived. This password will be generated from the specified 
            // passphrase and salt value. The password will be created using
            // the specified hash algorithm. Password creation can be done in
            // several iterations.
            PasswordDeriveBytes password = new PasswordDeriveBytes(
                passPhrase,
                saltValueBytes,
                hashAlgorithm,
                passwordIterations);

            // Use the password to generate pseudo-random bytes for the encryption
            // key. Specify the size of the key in bytes (instead of bits).
            byte[] keyBytes = password.GetBytes(keySize / 8);

            // Create uninitialized Rijndael encryption object.
            RijndaelManaged symmetricKey = new RijndaelManaged();

            // It is reasonable to set encryption mode to Cipher Block Chaining
            // (CBC). Use default options for other symmetric key parameters.
            symmetricKey.Mode = CipherMode.CBC;

            // Generate decryptor from the existing key bytes and initialization 
            // vector. Key size will be defined based on the number of the key 
            // bytes.
            ICryptoTransform decryptor = symmetricKey.CreateDecryptor(
                keyBytes,
                initVectorBytes);

            // Define memory stream which will be used to hold encrypted data.
            MemoryStream memoryStream = new MemoryStream(cipherTextBytes);
            string plainText = string.Empty;

            // Define cryptographic stream (always use Read mode for encryption).
            using (CryptoStream cryptoStream = new CryptoStream(memoryStream,
                      decryptor,
                      CryptoStreamMode.Read))
            {
                // Since at this point we don't know what the size of decrypted data
                // will be, allocate the buffer long enough to hold ciphertext;
                // plaintext is never longer than ciphertext.
                byte[] plainTextBytes = new byte[cipherTextBytes.Length];

                // Start decrypting.
                int decryptedByteCount = cryptoStream.Read(plainTextBytes,
                    0,
                    plainTextBytes.Length);

                // Close both streams.
                memoryStream.Close();
                cryptoStream.Close();


                // Convert decrypted data into a string. 
                // Let us assume that the original plaintext string was UTF8-encoded.
                plainText = Encoding.UTF8.GetString(plainTextBytes,
                    0,
                    decryptedByteCount);
            }

            // Return decrypted string.   
            return plainText;
        }

        #endregion

        #region Machine Hardware ID Reading

        // query string to get serial number of a motherboard.
        private static readonly string MOTHERBOARD_SERIAL_QUERY = "Select SerialNumber from Win32_BaseBoard";

        // error string.
        private static readonly string ERROR_STRING = "Error";

        /// <summary>
        /// This method returns the serial number of the mother board.
        /// </summary>
        /// <returns></returns>
        internal static string GetBoardSerialID()
        {
            string boardSerialID = "";

            // Cannot used motherboard id in encryption/decryption of license key for load balancing environment. Therefore, returning fixed Id.
            //if (Global.EnableLoadBalancer)
            //{
            //    boardSerialID = "294AB485-7FF6-4E05-959A-7DC058FE7FDF";
            //    return boardSerialID;
            //}

            ManagementObjectSearcher query;
            ManagementObjectCollection queryCollection;
            try
            {
                query = new ManagementObjectSearcher(MOTHERBOARD_SERIAL_QUERY);
                queryCollection = query.Get();
                foreach (ManagementObject mo in queryCollection)
                {
                    boardSerialID = mo["SerialNumber"].ToString();
                }
            }
            catch (Exception err)
            {
                //LicenseInvoker.Log("Info: -GetBoardSerialID()-");
                //LicenseInvoker.LogError(err);
                //boardSerialID = "Error in getting motherboard ID.";
                boardSerialID = ERROR_STRING;
            }
            return boardSerialID.Trim();
        }

        #endregion
    }

}