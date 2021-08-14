﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using PriSecDBClientPanel.Helper;
using PriSecDBClientPanel.Model;
using ASodium;
using System.Web;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
using System.Net.Http;
using System.IO;
using System.Security.Cryptography;

namespace PriSecDBClientPanel
{
    public partial class Greetings : Form
    {
        private SecureIDGenerator MySecureIDGenerator = new SecureIDGenerator();

        public Greetings()
        {
            InitializeComponent();
        }

        private void Greetings_Load(object sender, EventArgs e)
        {
            if (Directory.GetFileSystemEntries(Application.StartupPath + "\\Temp_Session\\").Length == 0 || Directory.GetFileSystemEntries(Application.StartupPath + "\\Temp_Session\\").Length == 1)
            {
                CreateNewSession();
            }
            else
            {
                Boolean ServerOnlineChecker = true;
                StreamReader MyStreamReader = new StreamReader(Application.StartupPath + "\\Temp_Session\\" + "SessionID.txt");
                String Temp_Session_ID = MyStreamReader.ReadLine();
                MyStreamReader.Close();
                Byte[] ClientECDSASKByte = new Byte[] { };
                Byte[] RandomData = new Byte[240];
                RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();
                rngCsp.GetBytes(RandomData);
                Byte[] SignedRandomData = new Byte[] { };
                String Status = "";
                if (Temp_Session_ID != null && Temp_Session_ID.CompareTo("") != 0)
                {
                    using (var client = new HttpClient())
                    {
                        ClientECDSASKByte = File.ReadAllBytes(Application.StartupPath + "\\Temp_Session\\" + Temp_Session_ID + "\\ECDSASK.txt");
                        SignedRandomData = SodiumPublicKeyAuth.Sign(RandomData, ClientECDSASKByte);
                        client.BaseAddress = new Uri("https://mrchewitsoftware.com.my:5002/api/");
                        client.DefaultRequestHeaders.Accept.Clear();
                        client.DefaultRequestHeaders.Accept.Add(
                            new MediaTypeWithQualityHeaderValue("application/json"));
                        var response = client.GetAsync("ECDH_ECDSA_TempSession/DeleteByClientCryptographicID?ClientPathID=" + Temp_Session_ID + "&ValidationData=" + HttpUtility.UrlEncode(Convert.ToBase64String(SignedRandomData)));
                        GCHandle MyGCHandleForECDSASKByte = GCHandle.Alloc(ClientECDSASKByte, GCHandleType.Pinned);
                        SodiumSecureMemory.MemZero(MyGCHandleForECDSASKByte.AddrOfPinnedObject(), ClientECDSASKByte.Length);
                        MyGCHandleForECDSASKByte.Free();
                        GCHandle MyGeneralGCHandle = GCHandle.Alloc(RandomData, GCHandleType.Pinned);
                        SodiumSecureMemory.MemZero(MyGeneralGCHandle.AddrOfPinnedObject(), RandomData.Length);
                        MyGeneralGCHandle.Free();
                        MyGeneralGCHandle = GCHandle.Alloc(SignedRandomData, GCHandleType.Pinned);
                        SodiumSecureMemory.MemZero(MyGeneralGCHandle.AddrOfPinnedObject(), SignedRandomData.Length);
                        MyGeneralGCHandle.Free();
                        try
                        {
                            response.Wait();
                        }
                        catch
                        {
                            ServerOnlineChecker = false;
                            MessageBox.Show("The server is offline now please wait for awhile ..", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        if (ServerOnlineChecker == true)
                        {
                            var result = response.Result;
                            if (result.IsSuccessStatusCode)
                            {
                                var readTask = result.Content.ReadAsStringAsync();
                                readTask.Wait();

                                Status = readTask.Result;

                                if (Status.Contains("Error"))
                                {
                                    File.WriteAllText(Application.StartupPath + "\\Error_Data\\Greetings\\DeleteHandShakeSessionIDStatus.txt", Status);
                                }
                                else
                                {
                                    Directory.Delete(Application.StartupPath + "\\Temp_Session\\" + Temp_Session_ID, true);
                                    File.WriteAllText(Application.StartupPath + "\\Temp_Session\\" + "SessionID.txt", "");
                                    CreateNewSession();
                                }
                            }
                            else
                            {
                                File.WriteAllText(Application.StartupPath + "\\Error_Data\\Greetings\\GeneralStatus.txt", "Something went wrong on server side... refer to any status text to find out..");
                            }
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Something's wrong .. did you delete the values in the text file but not the folder?", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        public void CreateNewSession()
        {
            RevampedKeyPair SessionECDHKeyPair = SodiumPublicKeyBox.GenerateRevampedKeyPair();
            RevampedKeyPair SessionECDSAKeyPair = SodiumPublicKeyAuth.GenerateRevampedKeyPair();
            String MySession_ID = MySecureIDGenerator.GenerateUniqueString();
            ECDH_ECDSA_Models MyECDH_ECDSA_Models = new ECDH_ECDSA_Models();
            Boolean CheckServerOnline = true;
            Boolean CreateShareSecretStatus = true;
            Boolean CheckSharedSecretStatus = true;
            Byte[] ServerECDSAPKByte = new Byte[] { };
            Byte[] ServerECDHSPKByte = new Byte[] { };
            Byte[] ServerECDHPKByte = new Byte[] { };
            Byte[] SignedClientSessionECDHPKByte = new Byte[] { };
            Byte[] ClientSessionECDSAPKByte = new Byte[] { };
            Boolean VerifyBoolean = true;
            String SessionStatus = "";
            String ExceptionString = "";
            using (var InitializeHandShakeHttpclient = new HttpClient())
            {
                InitializeHandShakeHttpclient.BaseAddress = new Uri("https://mrchewitsoftware.com.my:5002/api/");
                InitializeHandShakeHttpclient.DefaultRequestHeaders.Accept.Clear();
                InitializeHandShakeHttpclient.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
                var response = InitializeHandShakeHttpclient.GetAsync("ECDH_ECDSA_TempSession/byID?ClientPathID=" + MySession_ID);
                try
                {
                    response.Wait();
                }
                catch
                {
                    CheckServerOnline = false;
                }
                if (CheckServerOnline == true)
                {
                    var result = response.Result;
                    if (result.IsSuccessStatusCode)
                    {
                        var readTask = result.Content.ReadAsStringAsync();
                        readTask.Wait();

                        var ECDH_ECDSA_Models_Result = readTask.Result;
                        MyECDH_ECDSA_Models = JsonConvert.DeserializeObject<ECDH_ECDSA_Models>(ECDH_ECDSA_Models_Result);
                        if (MyECDH_ECDSA_Models.ID_Checker_Message.CompareTo("You still can use the exact same client ID...") == 0 || MyECDH_ECDSA_Models.ID_Checker_Message.CompareTo("You have an exact client ID great~") == 0)
                        {
                            ServerECDSAPKByte = Convert.FromBase64String(MyECDH_ECDSA_Models.ECDSA_PK_Base64String);
                            ServerECDHSPKByte = Convert.FromBase64String(MyECDH_ECDSA_Models.ECDH_SPK_Base64String);
                            GCHandle ServerECDSAPKByteGCHandle = GCHandle.Alloc(ServerECDSAPKByte, GCHandleType.Pinned);
                            GCHandle ServerECDHSPKByteGCHandle = GCHandle.Alloc(ServerECDHSPKByte, GCHandleType.Pinned);
                            try
                            {
                                ServerECDHPKByte = SodiumPublicKeyAuth.Verify(ServerECDHSPKByte, ServerECDSAPKByte);
                            }
                            catch (Exception exception)
                            {
                                VerifyBoolean = false;
                                ExceptionString = exception.ToString();
                                SessionStatus += ExceptionString + Environment.NewLine;
                            }
                            if (VerifyBoolean == true)
                            {
                                ClientSessionECDSAPKByte = SessionECDSAKeyPair.PublicKey;
                                SignedClientSessionECDHPKByte = SodiumPublicKeyAuth.Sign(SessionECDHKeyPair.PublicKey, SessionECDSAKeyPair.PrivateKey);
                                CreateSharedSecret(ref CreateShareSecretStatus, MySession_ID, SignedClientSessionECDHPKByte, ClientSessionECDSAPKByte);
                                if (CreateShareSecretStatus == true)
                                {
                                    CheckSharedSecret(ref CheckSharedSecretStatus, MySession_ID, ServerECDHPKByte, SessionECDHKeyPair.PrivateKey,SessionECDHKeyPair.PublicKey , SessionECDSAKeyPair.PrivateKey);
                                    if (CheckSharedSecretStatus == true)
                                    {
                                        StatusLabel.Text = "Welcome to PriSecDBClient Panel, click any buttons below";
                                        MakeRenewPaymentBTN.Enabled = true;
                                        CreateSealedDBCredentialsBTN.Enabled = true;
                                        DBCRUDDemoBTN.Enabled = true;
                                        LockUnlockDBAccountBTN.Enabled = true;
                                    }
                                    else 
                                    {
                                        StatusLabel.Text = "Error in matching the established shared secret with the server, please restart the application";
                                    }
                                }
                                else 
                                {
                                    StatusLabel.Text = "Error in creating shared secret with the server, please restart the application";
                                }
                            }
                            else
                            {
                                File.WriteAllText(Application.StartupPath + "\\Error_Data\\Greetings\\FetchHandShakeSessionParameterStatus.txt", "Server's ECDH public key can't be verified with given ECDSA public key");
                                StatusLabel.Text = "Error in verifying the server ECDH public key, please restart the application";
                            }
                            SodiumSecureMemory.MemZero(ServerECDHSPKByteGCHandle.AddrOfPinnedObject(), ServerECDHSPKByte.Length);
                            SodiumSecureMemory.MemZero(ServerECDSAPKByteGCHandle.AddrOfPinnedObject(), ServerECDSAPKByte.Length);
                            ServerECDHSPKByteGCHandle.Free();
                            ServerECDSAPKByteGCHandle.Free();
                        }
                        else
                        {
                            MessageBox.Show(MyECDH_ECDSA_Models.ID_Checker_Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        File.WriteAllText(Application.StartupPath + "\\Error_Data\\Greetings\\FetchHandShakeSessionParameterStatus.txt", "Failed to get handshake parameters from server");
                    }
                }
                else
                {
                    MessageBox.Show("The server is offline now please wait for awhile ..", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            SessionECDHKeyPair.Clear();
            SessionECDSAKeyPair.Clear();
        }

        public void CreateSharedSecret(ref Boolean CheckBoolean, String MySession_ID, Byte[] SignedClientSessionECDHPKByte, Byte[] ClientSessionECDSAPKByte)
        {
            CheckBoolean = false;
            String SessionStatus = "";
            var CreateSharedSecretHttpClient = new HttpClient();
            CreateSharedSecretHttpClient.BaseAddress = new Uri("https://mrchewitsoftware.com.my:5002/api/");
            CreateSharedSecretHttpClient.DefaultRequestHeaders.Accept.Clear();
            CreateSharedSecretHttpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            var newresponse = CreateSharedSecretHttpClient.GetAsync("ECDH_ECDSA_TempSession/ByHandshake?ClientPathID=" + MySession_ID + "&SECDHPK=" + HttpUtility.UrlEncode(Convert.ToBase64String(SignedClientSessionECDHPKByte)) + "&ECDSAPK=" + HttpUtility.UrlEncode(Convert.ToBase64String(ClientSessionECDSAPKByte)));
            try
            {
                newresponse.Wait();
                var newresult = newresponse.Result;

                if (newresult.IsSuccessStatusCode)
                {
                    var newreadTask = newresult.Content.ReadAsStringAsync();
                    newreadTask.Wait();

                    SessionStatus += newreadTask.Result;

                    if (SessionStatus.Contains("Error"))
                    {
                        File.WriteAllText(Application.StartupPath + "\\Error_Data\\Greetings\\CreateSharedSecretStatus.txt", SessionStatus);
                    }
                    else
                    {
                        CheckBoolean = true;
                    }
                }
                else
                {
                    MessageBox.Show("Failed to fetch values from server", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch
            {
                MessageBox.Show("The server is offline now please wait for awhile ..", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void CheckSharedSecret(ref Boolean CheckBoolean, String MySession_ID, Byte[] ServerECDHPKByte, Byte[] SessionECDHPrivateKey, Byte[] SessionECDHPublicKey, Byte[] SessionECDSAPrivateKey)
        {
            if (Directory.Exists(Application.StartupPath + "\\Temp_Session\\" + MySession_ID)==false)
            {
                Directory.CreateDirectory(Application.StartupPath + "\\Temp_Session\\" + MySession_ID);
                File.WriteAllBytes(Application.StartupPath + "\\Temp_Session\\" + MySession_ID + "\\" + "ECDHSK.txt", SessionECDHPrivateKey);
            }
            else 
            {
                File.WriteAllBytes(Application.StartupPath + "\\Temp_Session\\" + MySession_ID + "\\" + "ECDHSK.txt", SessionECDHPrivateKey);
            }
            CheckBoolean = false;
            Boolean CheckServerOnline = true;
            String CheckSharedSecretStatus = "";
            Byte[] TestData = new Byte[] { 255, 255, 255 };
            Byte[] SharedSecretByte = SodiumScalarMult.Mult(SessionECDHPrivateKey, ServerECDHPKByte);
            Byte[] NonceByte = SodiumSecretBox.GenerateNonce();
            Byte[] TestEncryptedData = SodiumSecretBox.Create(TestData, NonceByte, SharedSecretByte);
            var CheckSharedSecretHttpClient = new HttpClient();
            CheckSharedSecretHttpClient.BaseAddress = new Uri("https://mrchewitsoftware.com.my:5002/api/");
            CheckSharedSecretHttpClient.DefaultRequestHeaders.Accept.Clear();
            CheckSharedSecretHttpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            var CheckSharedSecretHttpClientResponse = CheckSharedSecretHttpClient.GetAsync("ECDH_ECDSA_TempSession/BySharedSecret?ClientPathID=" + MySession_ID + "&CipheredData=" + HttpUtility.UrlEncode(Convert.ToBase64String(TestEncryptedData)) + "&Nonce=" + HttpUtility.UrlEncode(Convert.ToBase64String(NonceByte)));
            try
            {
                CheckSharedSecretHttpClientResponse.Wait();
            }
            catch
            {
                CheckServerOnline = false;
            }
            if (CheckServerOnline == true)
            {
                var CheckSharedSecretHttpClientResponseResult = CheckSharedSecretHttpClientResponse.Result;

                if (CheckSharedSecretHttpClientResponseResult.IsSuccessStatusCode)
                {
                    var CheckSharedSecretHttpClientResponseResultReadTask = CheckSharedSecretHttpClientResponseResult.Content.ReadAsStringAsync();
                    CheckSharedSecretHttpClientResponseResultReadTask.Wait();

                    CheckSharedSecretStatus = CheckSharedSecretHttpClientResponseResultReadTask.Result;
                    if (CheckSharedSecretStatus.Contains("Error"))
                    {
                        File.WriteAllText(Application.StartupPath + "\\Error_Data\\Greetings\\CheckEstablishedSharedSecretStatus.txt", CheckSharedSecretStatus);
                    }
                    else
                    {
                        File.WriteAllBytes(Application.StartupPath + "\\Temp_Session\\" + MySession_ID + "\\" + "SharedSecret.txt", SharedSecretByte);
                        File.WriteAllBytes(Application.StartupPath + "\\Temp_Session\\" + MySession_ID + "\\" + "ECDSASK.txt", SessionECDSAPrivateKey);
                        File.WriteAllText(Application.StartupPath + "\\Temp_Session\\" + "SessionID.txt", MySession_ID);
                        CheckSharedSecretStatus = CheckSharedSecretStatus.Substring(1, CheckSharedSecretStatus.Length - 2);
                        ETLSSessionIDStorage.ETLSID = MySession_ID;
                        CheckBoolean = true;
                    }
                }
                else
                {
                    MessageBox.Show("Failed to fetch values from server", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                GCHandle SharedSecretByteGCHandle = GCHandle.Alloc(SharedSecretByte, GCHandleType.Pinned);
                SodiumSecureMemory.MemZero(SharedSecretByteGCHandle.AddrOfPinnedObject(), SharedSecretByte.Length);
                SharedSecretByteGCHandle.Free();
                GCHandle ECDSASKGCHandle = GCHandle.Alloc(SessionECDSAPrivateKey, GCHandleType.Pinned);
                SodiumSecureMemory.MemZero(ECDSASKGCHandle.AddrOfPinnedObject(), SessionECDSAPrivateKey.Length);
                ECDSASKGCHandle.Free();
            }
            else
            {
                MessageBox.Show("The server is offline now please wait for awhile ..", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void MakeRenewPaymentBTN_Click(object sender, EventArgs e)
        {
            MainFormHolder.myForm = this;
            MainFormHolder.OpenMainForm = true;
            this.Hide();
            MakeRenewPaymentChooser NewForm = new MakeRenewPaymentChooser();
            NewForm.Show();
        }

        private void CreateSealedDBCredentialsBTN_Click(object sender, EventArgs e)
        {
            MainFormHolder.myForm = this;
            MainFormHolder.OpenMainForm = true;
            this.Hide();
            CreateSealedCredentialsChooser NewForm = new CreateSealedCredentialsChooser();
            NewForm.Show();
        }

        private void DBCRUDDemoBTN_Click(object sender, EventArgs e)
        {
            MainFormHolder.myForm = this;
            MainFormHolder.OpenMainForm = true;
            this.Hide();
            DBCRUDChooser NewForm = new DBCRUDChooser();
            NewForm.Show();
        }

        private void LockUnlockDBAccountBTN_Click(object sender, EventArgs e)
        {
            MainFormHolder.myForm = this;
            MainFormHolder.OpenMainForm = true;
            this.Hide();
            DBLockUnlockChooser NewForm = new DBLockUnlockChooser();
            NewForm.Show();
        }
    }
}
